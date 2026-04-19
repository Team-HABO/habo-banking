import "dotenv/config";
import { describe, it, expect, beforeEach, afterEach } from "vitest";
import { prisma } from "../prisma/prisma";
import { RabbitMQ } from "../src/RabbitMQ";
import handleDelete from "../src/handlers/handleDelete";
import type { TAccountPayload } from "../src/events/account";
import { ACCOUNT_GUID, cleanupBalance, EXCHANGE, OWNER_ID, QUEUE } from "./helpers";
import { v4 as uuidv4 } from "uuid";

function makeDeletePayload(accountGuid = ACCOUNT_GUID): TAccountPayload {
	return {
		message: {
			data: {
				accountGuid,
				ownerId: OWNER_ID,
				timestamp: new Date().toISOString()
			},
			metadata: {
				messageType: "ACCOUNT_DELETE",
				messageTimestamp: new Date().toISOString(),
				messageId: uuidv4()
			}
		}
	};
}

describe("handleDelete via RabbitMQ exchange", () => {
	let balanceId: number;
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

		const balance = await prisma.balance.create({
			data: {
				accountGuid: ACCOUNT_GUID,
				ownerId: OWNER_ID,
				balanceDetails: {
					create: { amount: "100" }
				}
			}
		});
		balanceId = balance.id;
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

	it("should process an ACCOUNT_DELETE message published to the exchange", async () => {
		const processed = new Promise<void>((resolve, reject) => {
			const timeout = setTimeout(() => reject(new Error("Timed out waiting for exchange message")), 10_000);

			consumer
				.consumeFromExchange(QUEUE, EXCHANGE, "fanout", async (data) => {
					clearTimeout(timeout);
					try {
						await handleDelete(data);
						resolve();
					} catch (err) {
						reject(err);
					}
				})
				.catch(reject);
		});

		const payload = makeDeletePayload();
		producer.getChannel().publish(EXCHANGE, "", Buffer.from(JSON.stringify(payload)), { persistent: true });
		await processed;

		const deleted = await prisma.deletedBalance.findFirst({
			where: { balanceId }
		});

		expect(deleted).not.toBeNull();
		expect(deleted!.balanceId).toBe(balanceId);
	});

	it("should process multiple ACCOUNT_DELETE messages idempotently", async () => {
		let received = 0;
		const allProcessed = new Promise<void>((resolve, reject) => {
			const timeout = setTimeout(() => reject(new Error("Timed out waiting for exchange messages")), 10_000);

			consumer
				.consumeFromExchange(QUEUE, EXCHANGE, "fanout", async (data) => {
					try {
						await handleDelete(data);
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
		const payload1 = makeDeletePayload();
		const payload2 = makeDeletePayload();
		ch.publish(EXCHANGE, "", Buffer.from(JSON.stringify(payload1)), { persistent: true });
		ch.publish(EXCHANGE, "", Buffer.from(JSON.stringify(payload2)), { persistent: true });
		await allProcessed;

		const deletedRecords = await prisma.deletedBalance.findMany({
			where: { balanceId }
		});

		expect(deletedRecords).toHaveLength(1);
	});
});
