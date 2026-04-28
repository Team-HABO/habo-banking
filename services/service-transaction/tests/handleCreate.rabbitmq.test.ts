import "dotenv/config";
import { describe, it, expect, beforeEach, afterEach } from "vitest";
import { prisma } from "../prisma/prisma";
import { RabbitMQ } from "../src/RabbitMQ";
import handleCreate from "../src/handlers/handleCreate";
import type { TAccountPayload } from "../src/events/account";
import { ACCOUNT_GUID, cleanupBalance, EXCHANGE, OWNER_ID, QUEUE } from "./helpers";
import { v4 as uuidv4 } from "uuid";

function makeCreatePayload(accountGuid = ACCOUNT_GUID, ownerId = OWNER_ID): TAccountPayload {
	return {
		message: {
			data: {
				accountGuid,
				ownerId,
				type: "SAVINGS",
				name: "Test Account",
				isFrozen: false,
				timestamp: new Date().toISOString()
			},
			metadata: {
				messageType: "ACCOUNT_CREATE",
				messageTimestamp: new Date().toISOString(),
				messageId: uuidv4()
			}
		}
	};
}

describe("handleCreate via RabbitMQ exchange", () => {
	let producer: RabbitMQ<TAccountPayload>;
	let consumer: RabbitMQ<TAccountPayload>;

	beforeEach(async () => {
		producer = new RabbitMQ<TAccountPayload>();
		await producer.connect();

		consumer = new RabbitMQ<TAccountPayload>();
		await consumer.connect();

		// Set up exchange, DLX, and queue binding, then purge leftover messages
		const ch = consumer.getChannel();
		const dlxExchange = `${EXCHANGE}.dlx`;
		const dlqName = `${QUEUE}.dlq`;
		await ch.assertExchange(EXCHANGE, "fanout", { durable: true });
		await ch.assertExchange(dlxExchange, "direct", { durable: true });
		await ch.assertQueue(dlqName, { durable: true });
		await ch.bindQueue(dlqName, dlxExchange, QUEUE);
		await ch.assertQueue(QUEUE, {
			durable: true,
			arguments: {
				"x-dead-letter-exchange": dlxExchange,
				"x-dead-letter-routing-key": QUEUE
			}
		});
		await ch.bindQueue(QUEUE, EXCHANGE, "");
		await ch.purgeQueue(QUEUE);
	});

	afterEach(async () => {
		const ch = consumer.getChannel();
		await ch.deleteQueue(QUEUE);
		await ch.deleteQueue(`${QUEUE}.dlq`);
		await ch.deleteExchange(EXCHANGE);
		await ch.deleteExchange(`${EXCHANGE}.dlx`);
		await producer.closeConnection();
		await consumer.closeConnection();
		await cleanupBalance(ACCOUNT_GUID);
	});

	it("should process an ACCOUNT_CREATE message published to the exchange", async () => {
		const processed = new Promise<void>((resolve, reject) => {
			const timeout = setTimeout(() => reject(new Error("Timed out waiting for exchange message")), 10_000);

			consumer
				.consumeFromExchange(QUEUE, EXCHANGE, "fanout", async (data) => {
					clearTimeout(timeout);
					try {
						await handleCreate(data);
						resolve();
					} catch (err) {
						reject(err);
					}
				})
				.catch(reject);
		});

		const payload = makeCreatePayload();
		producer.getChannel().publish(EXCHANGE, "", Buffer.from(JSON.stringify(payload)), { persistent: true });
		await processed;

		const balance = await prisma.balance.findUnique({
			where: { accountGuid: ACCOUNT_GUID }
		});

		expect(balance).not.toBeNull();
		expect(balance!.ownerId).toBe(OWNER_ID);

		const details = await prisma.balanceDetail.findMany({
			where: { balanceId: balance!.id }
		});

		expect(details).toHaveLength(1);
		expect(details[0]!.amount).toBe("0");
	});

	it("should process multiple ACCOUNT_CREATE messages idempotently", async () => {
		let received = 0;
		const allProcessed = new Promise<void>((resolve, reject) => {
			const timeout = setTimeout(() => reject(new Error("Timed out waiting for exchange messages")), 10_000);

			consumer
				.consumeFromExchange(QUEUE, EXCHANGE, "fanout", async (data) => {
					try {
						await handleCreate(data);
						received++;
						if (received === 2) {
							clearTimeout(timeout);
							resolve();
						}
					} catch (err) {
						clearTimeout(timeout);
						reject(err);
					}
				})
				.catch(reject);
		});

		const ch = producer.getChannel();
		const payload1 = makeCreatePayload();
		const payload2 = makeCreatePayload();
		ch.publish(EXCHANGE, "", Buffer.from(JSON.stringify(payload1)), { persistent: true });
		ch.publish(EXCHANGE, "", Buffer.from(JSON.stringify(payload2)), { persistent: true });
		await allProcessed;

		const balances = await prisma.balance.findMany({
			where: { accountGuid: ACCOUNT_GUID }
		});

		expect(balances).toHaveLength(1);
	});
});
