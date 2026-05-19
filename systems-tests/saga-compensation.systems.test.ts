import * as amqp from 'amqplib';
import pg from 'pg';
import { v4 as uuidv4 } from 'uuid';
import { describe, it, expect, beforeAll, afterAll } from 'vitest';

// ── Constants (must match service topology) ─────────────────────────────────
const COMPENSATION_EXCHANGE = 'account-create-response';
const COMPENSATION_ROUTING_KEY = 'account-create-failed-queue';

const ACCOUNT_SERVICE_URL = process.env.ACCOUNT_SERVICE_URL ?? 'http://localhost:8000';
const RABBITMQ_URL = process.env.RABBITMQ_URL ?? 'amqp://localhost';

const PG_CONFIG = {
	host: process.env.POSTGRES_HOST ?? 'localhost',
	port: Number(process.env.POSTGRES_PORT ?? '5432'),
	database: process.env.POSTGRES_DB ?? 'account_db',
	user: process.env.POSTGRES_USER ?? 'postgres',
	password: process.env.POSTGRES_PASSWORD ?? 'postgres',
};

const POLL_INTERVAL_MS = 500;
const POLL_TIMEOUT_MS = 15_000;

// ── Shared state ────────────────────────────────────────────────────────────
let rabbitConnection: amqp.ChannelModel;
let channel: amqp.Channel;
let pgClient: pg.Client;

// ── Helpers ─────────────────────────────────────────────────────────────────

/** Publish a BALANCE_CREATE_FAILED compensation message to RabbitMQ. */
function publishCompensation(accountGuid: string, ownerId: string, reason = 'Balance creation failed') {
	const payload = {
		data: {
			accountGuid,
			ownerId,
			reason,
		},
		metadata: {
			messageType: 'BALANCE_CREATE_FAILED',
			messageTimestamp: new Date().toISOString(),
			messageId: uuidv4(),
		},
	};

	channel.publish(
		COMPENSATION_EXCHANGE,
		COMPENSATION_ROUTING_KEY,
		Buffer.from(JSON.stringify(payload)),
		{ persistent: true },
	);
}

/** Create an account via the HTTP API. Returns the response body. */
async function createAccount(accountGuid: string, ownerId: string) {
	const res = await fetch(`${ACCOUNT_SERVICE_URL}/accounts/`, {
		method: 'POST',
		headers: { 'Content-Type': 'application/json' },
		body: JSON.stringify({
			owner_id: ownerId,
			name: `SAGA Test ${accountGuid.slice(0, 8)}`,
			type: 'savings',
			accountGuid,
		}),
	});
	return { status: res.status, body: await res.json() };
}

/** Delete an account via the HTTP API. */
async function deleteAccount(accountGuid: string) {
	const res = await fetch(`${ACCOUNT_SERVICE_URL}/accounts/${accountGuid}/`, {
		method: 'DELETE',
	});
	return { status: res.status, body: await res.json() };
}

/** Get the internal account `id` (PK) for a given GUID. */
async function getAccountId(accountGuid: string): Promise<number | null> {
	const result = await pgClient.query<{ id: number }>(
		'SELECT id FROM accounts WHERE account_guid = $1',
		[accountGuid],
	);
	return result.rows[0]?.id ?? null;
}

/** Count how many deleted_accounts rows exist for a given account PK. */
async function countDeletedRecords(accountId: number): Promise<number> {
	const result = await pgClient.query<{ count: string }>(
		'SELECT COUNT(*)::int AS count FROM deleted_accounts WHERE account_id = $1',
		[accountId],
	);
	return Number(result.rows[0].count);
}

/**
 * Poll the deleted_accounts table until a row appears for the given account PK,
 * or until `POLL_TIMEOUT_MS` elapses.
 */
async function waitForDeletion(accountId: number): Promise<boolean> {
	const deadline = Date.now() + POLL_TIMEOUT_MS;
	while (Date.now() < deadline) {
		const count = await countDeletedRecords(accountId);
		if (count > 0) return true;
		await new Promise((r) => setTimeout(r, POLL_INTERVAL_MS));
	}
	return false;
}

// ── Setup / Teardown ────────────────────────────────────────────────────────

beforeAll(async () => {
	// RabbitMQ
	rabbitConnection = await amqp.connect(RABBITMQ_URL);
	channel = await rabbitConnection.createChannel();
	await channel.assertExchange(COMPENSATION_EXCHANGE, 'direct', { durable: true });

	// PostgreSQL
	pgClient = new pg.Client(PG_CONFIG);
	await pgClient.connect();
});

afterAll(async () => {
	await channel?.close();
	await rabbitConnection?.close();
	await pgClient?.end();
});

// ── Tests ───────────────────────────────────────────────────────────────────

describe('SAGA compensation — BALANCE_CREATE_FAILED', () => {
	it('soft-deletes the account when balance creation fails', async () => {
		const accountGuid = uuidv4();
		const ownerId = `saga-test-${uuidv4()}`;

		// 1. Create an account via HTTP
		const { status } = await createAccount(accountGuid, ownerId);
		expect(status).toBe(201);

		// 2. Confirm it exists and is NOT deleted
		const accountId = await getAccountId(accountGuid);
		expect(accountId).not.toBeNull();
		expect(await countDeletedRecords(accountId!)).toBe(0);

		// 3. Publish BALANCE_CREATE_FAILED compensation message
		publishCompensation(accountGuid, ownerId);

		// 4. Poll until the account appears in deleted_accounts
		const deleted = await waitForDeletion(accountId!);
		expect(deleted).toBe(true);

		// 5. Verify exactly one deleted_accounts row
		expect(await countDeletedRecords(accountId!)).toBe(1);
	});

	it('is idempotent — does not duplicate deletion for an already-deleted account', async () => {
		const accountGuid = uuidv4();
		const ownerId = `saga-test-${uuidv4()}`;

		// 1. Create and immediately delete the account via HTTP
		const { status: createStatus } = await createAccount(accountGuid, ownerId);
		expect(createStatus).toBe(201);

		const { status: deleteStatus } = await deleteAccount(accountGuid);
		expect(deleteStatus).toBe(200);

		const accountId = await getAccountId(accountGuid);
		expect(accountId).not.toBeNull();
		expect(await countDeletedRecords(accountId!)).toBe(1);

		// 2. Publish BALANCE_CREATE_FAILED for the already-deleted account
		publishCompensation(accountGuid, ownerId);

		// 3. Wait long enough for the consumer to process
		await new Promise((r) => setTimeout(r, 3_000));

		// 4. Count should still be exactly 1 (no duplicate)
		expect(await countDeletedRecords(accountId!)).toBe(1);
	});

	it('handles a non-existent account gracefully (no crash)', async () => {
		const fakeGuid = uuidv4();

		// 1. Confirm the account doesn't exist
		const accountId = await getAccountId(fakeGuid);
		expect(accountId).toBeNull();

		// 2. Publish BALANCE_CREATE_FAILED for a non-existent account
		publishCompensation(fakeGuid, 'non-existent-owner');

		// 3. Wait for the consumer to process (it should skip gracefully)
		await new Promise((r) => setTimeout(r, 3_000));

		// 4. Still no account, no deleted record
		const accountIdAfter = await getAccountId(fakeGuid);
		expect(accountIdAfter).toBeNull();
	});
});
