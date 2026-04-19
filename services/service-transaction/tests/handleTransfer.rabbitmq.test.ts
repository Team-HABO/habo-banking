import "dotenv/config";
import { describe, it, expect, beforeEach, afterEach } from "vitest";
import { prisma } from "../prisma/prisma";
import { RabbitMQ } from "../src/RabbitMQ";
import handleTransfer from "../src/handlers/handleTransfer";
import type { TTransactionPayload } from "../src/events/transaction";
import {
	ACCOUNT_GUID,
	OWNER_ID,
	RECEIVER_ACCOUNT_GUID,
	RECEIVER_OWNER_ID,
	cleanupBalance,
	EXCHANGE,
	makeTransferPayload,
	QUEUE
} from "./helpers";

describe("handleTransfer via RabbitMQ exchange", () => {
	let senderBalanceId: number;
	let receiverBalanceId: number;
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

		const sender = await prisma.balance.create({
			data: {
				accountGuid: ACCOUNT_GUID,
				ownerId: OWNER_ID,
				balanceDetails: {
					create: { amount: "100" }
				}
			}
		});
		senderBalanceId = sender.id;

		const receiver = await prisma.balance.create({
			data: {
				accountGuid: RECEIVER_ACCOUNT_GUID,
				ownerId: RECEIVER_OWNER_ID,
				balanceDetails: {
					create: { amount: "200" }
				}
			}
		});
		receiverBalanceId = receiver.id;
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
		await cleanupBalance(RECEIVER_ACCOUNT_GUID);
	});

	it("should process a TRANSFER message published to the exchange", async () => {
		const processed = new Promise<void>((resolve, reject) => {
			const timeout = setTimeout(() => reject(new Error("Timed out waiting for exchange message")), 10_000);

			consumer
				.consumeFromExchange(QUEUE, EXCHANGE, "fanout", async (data) => {
					clearTimeout(timeout);
					try {
						await handleTransfer(data);
						resolve();
					} catch (err) {
						reject(err);
					}
				})
				.catch(reject);
		});

		const payload = makeTransferPayload("50", ACCOUNT_GUID, RECEIVER_ACCOUNT_GUID);
		producer.getChannel().publish(EXCHANGE, "", Buffer.from(JSON.stringify(payload)), { persistent: true });
		await processed;

		const senderLatest = await prisma.balanceDetail.findFirst({
			where: { balanceId: senderBalanceId },
			orderBy: { createdAt: "desc" }
		});
		const receiverLatest = await prisma.balanceDetail.findFirst({
			where: { balanceId: receiverBalanceId },
			orderBy: { createdAt: "desc" }
		});

		expect(senderLatest).not.toBeNull();
		expect(senderLatest!.amount).toBe("50"); // 100 - 50
		expect(receiverLatest).not.toBeNull();
		expect(receiverLatest!.amount).toBe("250"); // 200 + 50
	});

	it("should process multiple TRANSFER messages published to the exchange in order", async () => {
		let received = 0;
		const allProcessed = new Promise<void>((resolve, reject) => {
			const timeout = setTimeout(() => reject(new Error("Timed out waiting for exchange messages")), 10_000);

			consumer
				.consumeFromExchange(QUEUE, EXCHANGE, "fanout", async (data) => {
					try {
						await handleTransfer(data);
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
		const payload1 = makeTransferPayload("30", ACCOUNT_GUID, RECEIVER_ACCOUNT_GUID, 1000);
		const payload2 = makeTransferPayload("20", ACCOUNT_GUID, RECEIVER_ACCOUNT_GUID, 10000);
		ch.publish(EXCHANGE, "", Buffer.from(JSON.stringify(payload1)), { persistent: true });
		ch.publish(EXCHANGE, "", Buffer.from(JSON.stringify(payload2)), { persistent: true });
		await allProcessed;

		const senderDetails = await prisma.balanceDetail.findMany({
			where: { balanceId: senderBalanceId },
			orderBy: { createdAt: "asc" }
		});
		const receiverDetails = await prisma.balanceDetail.findMany({
			where: { balanceId: receiverBalanceId },
			orderBy: { createdAt: "asc" }
		});

		expect(senderDetails).toHaveLength(3);
		expect(senderDetails[0]!.amount).toBe("100");
		expect(senderDetails[1]!.amount).toBe("70"); // 100 - 30
		expect(senderDetails[2]!.amount).toBe("50"); // 70 - 20

		expect(receiverDetails).toHaveLength(3);
		expect(receiverDetails[0]!.amount).toBe("200");
		expect(receiverDetails[1]!.amount).toBe("230"); // 200 + 30
		expect(receiverDetails[2]!.amount).toBe("250"); // 230 + 20
	});
});
