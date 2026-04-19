import "dotenv/config";
import { describe, it, expect, beforeEach, afterEach } from "vitest";
import { prisma } from "../prisma/prisma";
import { RabbitMQ } from "../src/RabbitMQ";
import handleWithdraw from "../src/handlers/handleWithdraw";
import type { TTransactionPayload } from "../src/events/transaction";
import { ACCOUNT_GUID, cleanupBalance, EXCHANGE, makePayload, OWNER_ID, QUEUE } from "./helpers";

describe("handleWithdraw via RabbitMQ exchange", () => {
	let balanceId: number;
	let producer: RabbitMQ<TTransactionPayload>;
	let consumer: RabbitMQ<TTransactionPayload>;

	beforeEach(async () => {
		producer = new RabbitMQ<TTransactionPayload>();
		await producer.connect();

		consumer = new RabbitMQ<TTransactionPayload>();
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

	it("should process a WITHDRAW message published to the exchange", async () => {
		const processed = new Promise<void>((resolve, reject) => {
			const timeout = setTimeout(() => reject(new Error("Timed out waiting for exchange message")), 10_000);

			consumer
				.consumeFromExchange(QUEUE, EXCHANGE, "fanout", async (data) => {
					clearTimeout(timeout);
					try {
						await handleWithdraw(data);
						resolve();
					} catch (err) {
						reject(err);
					}
				})
				.catch(reject);
		});

		const payload = makePayload("50", ACCOUNT_GUID, 0, "WITHDRAW");
		producer.getChannel().publish(EXCHANGE, "", Buffer.from(JSON.stringify(payload)), { persistent: true });
		await processed;

		const latest = await prisma.balanceDetail.findFirst({
			where: { balanceId },
			orderBy: { createdAt: "desc" }
		});

		expect(latest).not.toBeNull();
		expect(latest!.amount).toBe("50");
	});

	it("should process multiple WITHDRAW messages published to the exchange in order", async () => {
		let received = 0;
		const allProcessed = new Promise<void>((resolve, reject) => {
			const timeout = setTimeout(() => reject(new Error("Timed out waiting for exchange messages")), 10_000);

			consumer
				.consumeFromExchange(QUEUE, EXCHANGE, "fanout", async (data) => {
					try {
						await handleWithdraw(data);
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
		const payload1 = makePayload("50", ACCOUNT_GUID, 1000, "WITHDRAW");
		const payload2 = makePayload("25", ACCOUNT_GUID, 2000, "WITHDRAW");
		ch.publish(EXCHANGE, "", Buffer.from(JSON.stringify(payload1)), { persistent: true });
		ch.publish(EXCHANGE, "", Buffer.from(JSON.stringify(payload2)), { persistent: true });
		await allProcessed;

		const details = await prisma.balanceDetail.findMany({
			where: { balanceId },
			orderBy: { createdAt: "asc" }
		});

		expect(details).toHaveLength(3);
		expect(details[0]!.amount).toBe("100");
		expect(details[1]!.amount).toBe("50"); // 100 - 50
		expect(details[2]!.amount).toBe("25"); // 50 - 25
	});
});
