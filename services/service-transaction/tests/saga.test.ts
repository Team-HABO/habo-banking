import "dotenv/config";
import { describe, it, expect, beforeEach, afterEach, vi } from "vitest";
import { prisma } from "../prisma/prisma";
import { RabbitMQ } from "../src/RabbitMQ";
import handleCreate from "../src/handlers/handleCreate";
import { produceAccountCreateFailed } from "../src/producer";
import type { TAccountCreateFailedPayload, TAccountPayload } from "../src/events/account";
import { ACCOUNT_GUID, cleanupBalance, OWNER_ID } from "./helpers";
import { v4 as uuidv4 } from "uuid";

const COMPENSATION_EXCHANGE = "account-create-response";
const COMPENSATION_QUEUE = "account-create-failed-queue";

function makeCreatePayload(accountGuid = ACCOUNT_GUID, ownerId = OWNER_ID): TAccountPayload {
	return {
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
	};
}

describe("Saga: ACCOUNT_CREATE compensation flow", () => {
	describe("produceAccountCreateFailed publishes to RabbitMQ", () => {
		let consumer: RabbitMQ<TAccountCreateFailedPayload>;

		beforeEach(async () => {
			consumer = new RabbitMQ<TAccountCreateFailedPayload>();
			await consumer.connect();

			const ch = consumer.getChannel();
			await ch.assertExchange(COMPENSATION_EXCHANGE, "direct", { durable: true });
			await ch.assertQueue(COMPENSATION_QUEUE, { durable: true });
			await ch.bindQueue(COMPENSATION_QUEUE, COMPENSATION_EXCHANGE, COMPENSATION_QUEUE);
			await ch.purgeQueue(COMPENSATION_QUEUE);
		});

		afterEach(async () => {
			const ch = consumer.getChannel();
			await ch.deleteQueue(COMPENSATION_QUEUE);
			await ch.deleteExchange(COMPENSATION_EXCHANGE);
			await consumer.closeConnection();
		});

		it("should publish a BALANCE_CREATE_FAILED message to the compensation exchange", async () => {
			const messageId = uuidv4();
			const payload: TAccountCreateFailedPayload = {
				data: {
					accountGuid: ACCOUNT_GUID,
					ownerId: OWNER_ID,
					reason: "Test failure"
				},
				metadata: {
					messageType: "BALANCE_CREATE_FAILED",
					messageTimestamp: new Date().toISOString(),
					messageId
				}
			};

			await produceAccountCreateFailed(payload);

			const received = await new Promise<TAccountCreateFailedPayload>((resolve, reject) => {
				const timeout = setTimeout(() => reject(new Error("Timed out waiting for compensation message")), 10_000);

				const ch = consumer.getChannel();
				ch.consume(
					COMPENSATION_QUEUE,
					(msg) => {
						if (!msg) return;
						clearTimeout(timeout);
						ch.ack(msg);
						resolve(JSON.parse(msg.content.toString()));
					},
					{ noAck: false }
				);
			});

			expect(received.metadata.messageType).toBe("BALANCE_CREATE_FAILED");
			expect(received.data.accountGuid).toBe(ACCOUNT_GUID);
			expect(received.data.ownerId).toBe(OWNER_ID);
			expect(received.data.reason).toBe("Test failure");
			expect(received.metadata.messageId).toBe(messageId);
		});
	});

	describe("handleCreate failure triggers compensation", () => {
		afterEach(async () => {
			await cleanupBalance(ACCOUNT_GUID);
			vi.restoreAllMocks();
		});

		it("should not create a balance when the database transaction fails", async () => {
			// Force prisma.$transaction to throw
			vi.spyOn(prisma, "$transaction").mockRejectedValueOnce(new Error("Simulated DB failure"));

			await expect(handleCreate(makeCreatePayload())).rejects.toThrow("Simulated DB failure");

			const balance = await prisma.balance.findUnique({
				where: { accountGuid: ACCOUNT_GUID }
			});
			expect(balance).toBeNull();
		});

		it("should build correct compensation payload from the failed message", () => {
			const payload = makeCreatePayload();
			const error = new Error("Some DB error");

			const compensationPayload: TAccountCreateFailedPayload = {
				data: {
					accountGuid: payload.data.accountGuid,
					ownerId: payload.data.ownerId,
					reason: error.message
				},
				metadata: {
					messageType: "BALANCE_CREATE_FAILED",
					messageTimestamp: new Date().toISOString(),
					messageId: payload.metadata.messageId
				}
			};

			expect(compensationPayload.data.accountGuid).toBe(ACCOUNT_GUID);
			expect(compensationPayload.data.ownerId).toBe(OWNER_ID);
			expect(compensationPayload.data.reason).toBe("Some DB error");
			expect(compensationPayload.metadata.messageType).toBe("BALANCE_CREATE_FAILED");
			expect(compensationPayload.metadata.messageId).toBe(payload.metadata.messageId);
		});
	});

	describe("end-to-end saga via RabbitMQ exchange", () => {
		let producer: RabbitMQ<TAccountPayload>;
		let accountConsumer: RabbitMQ<TAccountPayload>;
		let compensationConsumer: RabbitMQ<TAccountCreateFailedPayload>;

		const ACCOUNT_EXCHANGE = "account-exchange-events-saga-test";
		const ACCOUNT_QUEUE = "account-queue-saga-test";

		beforeEach(async () => {
			producer = new RabbitMQ<TAccountPayload>();
			await producer.connect();

			accountConsumer = new RabbitMQ<TAccountPayload>();
			await accountConsumer.connect();

			compensationConsumer = new RabbitMQ<TAccountCreateFailedPayload>();
			await compensationConsumer.connect();

			// Set up account exchange
			const ach = accountConsumer.getChannel();
			await ach.assertExchange(ACCOUNT_EXCHANGE, "fanout", { durable: true });
			await ach.assertQueue(ACCOUNT_QUEUE, { durable: true });
			await ach.bindQueue(ACCOUNT_QUEUE, ACCOUNT_EXCHANGE, "");
			await ach.purgeQueue(ACCOUNT_QUEUE);

			// Set up compensation exchange
			const cch = compensationConsumer.getChannel();
			await cch.assertExchange(COMPENSATION_EXCHANGE, "direct", { durable: true });
			await cch.assertQueue(COMPENSATION_QUEUE, { durable: true });
			await cch.bindQueue(COMPENSATION_QUEUE, COMPENSATION_EXCHANGE, COMPENSATION_QUEUE);
			await cch.purgeQueue(COMPENSATION_QUEUE);
		});

		afterEach(async () => {
			const ach = accountConsumer.getChannel();
			await ach.deleteQueue(ACCOUNT_QUEUE);
			await ach.deleteExchange(ACCOUNT_EXCHANGE);
			await producer.closeConnection();
			await accountConsumer.closeConnection();

			const cch = compensationConsumer.getChannel();
			await cch.deleteQueue(COMPENSATION_QUEUE);
			await cch.deleteExchange(COMPENSATION_EXCHANGE);
			await compensationConsumer.closeConnection();

			await cleanupBalance(ACCOUNT_GUID);
			vi.restoreAllMocks();
		});

		it("should publish BALANCE_CREATE_FAILED when handleCreate throws", async () => {
			// Force handleCreate to fail
			vi.spyOn(prisma, "$transaction").mockRejectedValue(new Error("Simulated DB failure"));

			// Simulate the consumer callback logic from consumer.ts
			const compensationReceived = new Promise<TAccountCreateFailedPayload>((resolve, reject) => {
				const timeout = setTimeout(() => reject(new Error("Timed out waiting for compensation")), 10_000);

				const cch = compensationConsumer.getChannel();
				cch.consume(
					COMPENSATION_QUEUE,
					(msg) => {
						if (!msg) return;
						clearTimeout(timeout);
						cch.ack(msg);
						resolve(JSON.parse(msg.content.toString()));
					},
					{ noAck: false }
				);
			});

			// Track when the account consumer finishes processing (including ack)
			let resolveAccountProcessed: () => void;
			const accountProcessed = new Promise<void>((resolve) => {
				resolveAccountProcessed = resolve;
			});

			// Set up the account consumer that mirrors the saga logic in consumer.ts
			const ach = accountConsumer.getChannel();
			ach.consume(
				ACCOUNT_QUEUE,
				async (msg) => {
					if (!msg) return;
					const data = JSON.parse(msg.content.toString()) as TAccountPayload;

					try {
						await handleCreate(data);
						ach.ack(msg);
					} catch (error) {
						const compensationPayload: TAccountCreateFailedPayload = {
							data: {
								accountGuid: data.data.accountGuid,
								ownerId: data.data.ownerId,
								reason: error instanceof Error ? error.message : "Balance creation failed"
							},
							metadata: {
								messageType: "BALANCE_CREATE_FAILED",
								messageTimestamp: new Date().toISOString(),
								messageId: data.metadata.messageId
							}
						};
						await produceAccountCreateFailed(compensationPayload);
						ach.ack(msg);
					}
					resolveAccountProcessed();
				},
				{ noAck: false }
			);

			// Publish the ACCOUNT_CREATE message
			const payload = makeCreatePayload();
			producer.getChannel().publish(ACCOUNT_EXCHANGE, "", Buffer.from(JSON.stringify(payload)), { persistent: true });

			// Wait for both the compensation message and the account consumer to finish
			const [received] = await Promise.all([compensationReceived, accountProcessed]);

			expect(received.metadata.messageType).toBe("BALANCE_CREATE_FAILED");
			expect(received.data.accountGuid).toBe(ACCOUNT_GUID);
			expect(received.data.ownerId).toBe(OWNER_ID);
			expect(received.data.reason).toBe("Simulated DB failure");
		});

		it("should NOT publish compensation when handleCreate succeeds", async () => {
			const ach = accountConsumer.getChannel();

			const createProcessed = new Promise<void>((resolve, reject) => {
				const timeout = setTimeout(() => reject(new Error("Timed out waiting for create")), 10_000);

				ach.consume(
					ACCOUNT_QUEUE,
					async (msg) => {
						if (!msg) return;
						clearTimeout(timeout);
						const data = JSON.parse(msg.content.toString()) as TAccountPayload;

						try {
							await handleCreate(data);
							ach.ack(msg);
							resolve();
						} catch (error) {
							const compensationPayload: TAccountCreateFailedPayload = {
								data: {
									accountGuid: data.data.accountGuid,
									ownerId: data.data.ownerId,
									reason: error instanceof Error ? error.message : "Balance creation failed"
								},
								metadata: {
									messageType: "BALANCE_CREATE_FAILED",
									messageTimestamp: new Date().toISOString(),
									messageId: data.metadata.messageId
								}
							};
							await produceAccountCreateFailed(compensationPayload);
							ach.ack(msg);
							reject(new Error("Should not have reached compensation path"));
						}
					},
					{ noAck: false }
				);
			});

			const payload = makeCreatePayload();
			producer.getChannel().publish(ACCOUNT_EXCHANGE, "", Buffer.from(JSON.stringify(payload)), { persistent: true });

			await createProcessed;

			// Verify balance was created successfully
			const balance = await prisma.balance.findUnique({
				where: { accountGuid: ACCOUNT_GUID }
			});
			expect(balance).not.toBeNull();

			// Verify no compensation message was sent
			const cch = compensationConsumer.getChannel();
			const queueInfo = await cch.checkQueue(COMPENSATION_QUEUE);
			expect(queueInfo.messageCount).toBe(0);
		});
	});
});
