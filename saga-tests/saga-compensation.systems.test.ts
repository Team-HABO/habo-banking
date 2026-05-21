import * as amqp from "amqplib";
import jwt from "jsonwebtoken";
import pg from "pg";
import { v4 as uuidv4 } from "uuid";
import { describe, it, expect, beforeAll, afterAll } from "vitest";

// ── Connection constants ─────────────────────────────────────────────────────
const RABBITMQ_URL = process.env.RABBITMQ_URL ?? "amqp://localhost";
const ACCOUNT_API_URL = process.env.ACCOUNT_API_URL ?? "http://localhost:8000/v1";

const PG_CONFIG: pg.PoolConfig = {
    host: process.env.POSTGRES_HOST ?? "localhost",
    port: Number(process.env.POSTGRES_PORT ?? 5432),
    database: process.env.POSTGRES_DB ?? "account_db",
    user: process.env.POSTGRES_USER ?? "postgres",
    password: process.env.POSTGRES_PASSWORD ?? "postgres",
};

const JWT_SECRET = process.env.JWT_SECRET_KEY ?? "test-secret-key-for-saga-tests-xxxxxxxxx";

/** Generate a signed JWT for the given owner ID. */
function generateToken(ownerId: string): string {
    return jwt.sign({ nameid: ownerId }, JWT_SECRET, { algorithm: "HS256" });
}

// ── RabbitMQ topology (must match service-transaction producer) ──────────────
const COMPENSATION_EXCHANGE = "account-create-response";
const COMPENSATION_ROUTING_KEY = "account-create-failed-queue";

const WAIT_TIMEOUT_MS = 20_000;
const POLL_INTERVAL_MS = 500;

// ── Shared state ─────────────────────────────────────────────────────────────
let rabbitConnection: amqp.ChannelModel;
let rabbitChannel: amqp.Channel;
let pgPool: pg.Pool;

// ── Helpers ──────────────────────────────────────────────────────────────────

/** Create an account via the HTTP API and return its GUID. */
async function createAccount(accountGuid: string, ownerId: string): Promise<void> {
    const token = generateToken(ownerId);
    const res = await fetch(`${ACCOUNT_API_URL}/accounts/`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({
            name: `saga-test-${accountGuid.slice(0, 8)}`,
            type: "savings",
            accountGuid,
        }),
    });

    if (!res.ok && res.status !== 200) {
        const body = await res.text();
        throw new Error(`Failed to create account (${res.status}): ${body}`);
    }
}

/** Delete an account via the HTTP API. */
async function deleteAccount(accountGuid: string): Promise<void> {
    const res = await fetch(`${ACCOUNT_API_URL}/accounts/${accountGuid}/`, {
        method: "DELETE",
    });

    if (!res.ok) {
        const body = await res.text();
        throw new Error(`Failed to delete account (${res.status}): ${body}`);
    }
}

/** Publish a BALANCE_CREATE_FAILED compensation message to RabbitMQ. */
function publishBalanceCreateFailed(accountGuid: string, ownerId: string): void {
    const payload = {
        data: {
            accountGuid,
            ownerId,
            reason: "Balance creation failed",
        },
        metadata: {
            messageType: "BALANCE_CREATE_FAILED",
            messageTimestamp: new Date().toISOString(),
            messageId: uuidv4(),
        },
    };

    rabbitChannel.publish(COMPENSATION_EXCHANGE, COMPENSATION_ROUTING_KEY, Buffer.from(JSON.stringify(payload)), {
        persistent: true,
        contentType: "application/json",
    });
}

/** Get the internal account PK by its GUID. Returns null if not found. */
async function getAccountId(accountGuid: string): Promise<number | null> {
    const { rows } = await pgPool.query<{ id: number }>("SELECT id FROM accounts WHERE account_guid = $1", [accountGuid]);
    return rows[0]?.id ?? null;
}

/** Count soft-delete records for an account PK. */
async function countDeletedRecords(accountId: number): Promise<number> {
    const { rows } = await pgPool.query<{ count: string }>("SELECT COUNT(*)::text AS count FROM deleted_accounts WHERE account_id = $1", [accountId]);
    return Number(rows[0].count);
}

/**
 * Poll until the account has at least `expectedCount` deleted_accounts rows,
 * or throw on timeout.
 */
async function waitForDeletion(accountId: number, expectedCount = 1): Promise<void> {
    const deadline = Date.now() + WAIT_TIMEOUT_MS;

    while (Date.now() < deadline) {
        const count = await countDeletedRecords(accountId);
        if (count >= expectedCount) return;
        await new Promise((r) => setTimeout(r, POLL_INTERVAL_MS));
    }

    throw new Error(`Timed out waiting for deleted_accounts row for account_id=${accountId}`);
}

// ── Setup / Teardown ─────────────────────────────────────────────────────────

beforeAll(async () => {
    // RabbitMQ
    rabbitConnection = await amqp.connect(RABBITMQ_URL);
    rabbitChannel = await rabbitConnection.createChannel();

    await rabbitChannel.assertExchange(COMPENSATION_EXCHANGE, "direct", {
        durable: true,
    });

    // PostgreSQL
    pgPool = new pg.Pool(PG_CONFIG);
    // Verify connectivity
    await pgPool.query("SELECT 1");
});

afterAll(async () => {
    await rabbitChannel?.close();
    await rabbitConnection?.close();
    await pgPool?.end();
});

// ── Tests ────────────────────────────────────────────────────────────────────

describe("SAGA compensation — BALANCE_CREATE_FAILED", () => {
    it("soft-deletes the account when compensation message is received", async () => {
        const accountGuid = uuidv4();
        const ownerId = `saga-test-owner-${uuidv4().slice(0, 8)}`;

        // 1. Create a live account via HTTP API
        await createAccount(accountGuid, ownerId);

        // 2. Confirm it exists and is NOT deleted
        const accountId = await getAccountId(accountGuid);
        expect(accountId).not.toBeNull();
        expect(await countDeletedRecords(accountId!)).toBe(0);

        // 3. Publish BALANCE_CREATE_FAILED — simulating what service-transaction
        //    would do if balance creation failed
        publishBalanceCreateFailed(accountGuid, ownerId);

        // 4. Poll the DB until the compensation consumer soft-deletes the account
        await waitForDeletion(accountId!);

        // 5. Confirm exactly one deleted_accounts row
        expect(await countDeletedRecords(accountId!)).toBe(1);
    });

    it("is idempotent — skips an already-deleted account", async () => {
        const accountGuid = uuidv4();
        const ownerId = `saga-test-owner-${uuidv4().slice(0, 8)}`;

        // 1. Create and immediately delete the account via HTTP
        await createAccount(accountGuid, ownerId);
        await deleteAccount(accountGuid);

        // 2. Confirm it has exactly one deleted_accounts row
        const accountId = await getAccountId(accountGuid);
        expect(accountId).not.toBeNull();
        expect(await countDeletedRecords(accountId!)).toBe(1);

        // 3. Publish compensation message for the already-deleted account
        publishBalanceCreateFailed(accountGuid, ownerId);

        // 4. Wait a bit for the consumer to process (it should skip)
        await new Promise((r) => setTimeout(r, 3000));

        // 5. Still exactly one deleted_accounts row — no duplicate
        expect(await countDeletedRecords(accountId!)).toBe(1);
    });

    it("handles a non-existent account gracefully", async () => {
        const fakeGuid = uuidv4();
        const fakeOwnerId = "non-existent-owner";

        // 1. Confirm the account does not exist
        const accountId = await getAccountId(fakeGuid);
        expect(accountId).toBeNull();

        // 2. Publish compensation message for a GUID that was never created
        publishBalanceCreateFailed(fakeGuid, fakeOwnerId);

        // 3. Wait a bit for the consumer to process (it should log and skip)
        await new Promise((r) => setTimeout(r, 3000));

        // 4. Still no account and no deleted_accounts row
        const accountIdAfter = await getAccountId(fakeGuid);
        expect(accountIdAfter).toBeNull();
    });
});
