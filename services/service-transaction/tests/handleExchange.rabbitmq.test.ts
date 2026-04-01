import "dotenv/config";
import { describe, it, expect, beforeEach, afterEach } from "vitest";
import { prisma } from "../prisma/prisma";
import { RabbitMQ } from "../src/RabbitMQ";
import handleExchange from "../src/handlers/handleExchange";
import type { TTransactionPayload } from "../src/events/transaction";
import { ACCOUNT_GUID, cleanupBalance, EXCHANGE, makePayload, OWNER_ID, QUEUE } from "./helpers";

describe("handleExchange via RabbitMQ exchange", () => {
	let balanceId: number;
	let producer: RabbitMQ<TTransactionPayload>;
	let consumer: RabbitMQ<TTransactionPayload>;

	beforeEach(async () => {
		producer = new RabbitMQ<TTransactionPayload>();
		await producer.connect();

		consumer = new RabbitMQ<TTransactionPayload>();
		await consumer.connect();

		// Set up exchange and queue binding, then purge leftover messages
		const ch = consumer.getChannel();
		await ch.assertExchange(EXCHANGE, "fanout", { durable: true });
		await ch.assertQueue(QUEUE, { durable: true });
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
		await ch.deleteExchange(EXCHANGE);
		await producer.closeConnection();
		await consumer.closeConnection();
		await cleanupBalance(ACCOUNT_GUID);
	});

	function makeExchangePayload(amount: string, offsetMs = 0): TTransactionPayload {
		const payload = makePayload(amount, ACCOUNT_GUID, offsetMs, "EXCHANGE");
		payload.message.data.currency = "EUR";
		payload.message.data.exchangeRate = 0.5;
		return payload;
	}

	it("should process an EXCHANGE message published to the exchange", async () => {
		const processed = new Promise<void>((resolve, reject) => {
			const timeout = setTimeout(() => reject(new Error("Timed out waiting for exchange message")), 10_000);

			consumer
				.consumeFromExchange(QUEUE, EXCHANGE, "fanout", async (data) => {
					clearTimeout(timeout);
					try {
						await handleExchange(data);
						resolve();
					} catch (err) {
						reject(err);
					}
				})
				.catch(reject);
		});

		const payload = makeExchangePayload("50");
		producer.getChannel().publish(EXCHANGE, "", Buffer.from(JSON.stringify(payload)), { persistent: true });
		await processed;

		const latest = await prisma.balanceDetail.findFirst({
			where: { balanceId },
			orderBy: { createdAt: "desc" }
		});

		expect(latest).not.toBeNull();
		expect(latest!.amount).toBe("75"); // 100 - (50 * 0.5)
	});

	it("should process multiple EXCHANGE messages published to the exchange in order", async () => {
		let received = 0;
		const allProcessed = new Promise<void>((resolve, reject) => {
			const timeout = setTimeout(() => reject(new Error("Timed out waiting for exchange messages")), 10_000);

			consumer
				.consumeFromExchange(QUEUE, EXCHANGE, "fanout", async (data) => {
					try {
						await handleExchange(data);
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
		const payload1 = makeExchangePayload("20", 1000);
		const payload2 = makeExchangePayload("10", 2000);
		ch.publish(EXCHANGE, "", Buffer.from(JSON.stringify(payload1)), { persistent: true });
		ch.publish(EXCHANGE, "", Buffer.from(JSON.stringify(payload2)), { persistent: true });
		await allProcessed;

		const details = await prisma.balanceDetail.findMany({
			where: { balanceId },
			orderBy: { createdAt: "asc" }
		});

		expect(details).toHaveLength(3);
		expect(details[0]!.amount).toBe("100");
		expect(details[1]!.amount).toBe("90"); // 100 - (20 * 0.5)
		expect(details[2]!.amount).toBe("85"); // 90 - (10 * 0.5)
	});
});
