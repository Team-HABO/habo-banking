import * as amqp from "amqplib";
import { v4 as uuidv4 } from "uuid";
import { describe, it, expect, beforeAll, afterAll } from "vitest";

// ── Exchange / routing-key constants (must match service topology) ──────────
// Entry point: the AI service publishes FraudChecked here; transaction service
// binds its "check-fraud" queue to this fanout exchange.
const FRAUD_CHECKED_EXCHANGE = "service_ai.Messages:FraudChecked";

// The currency-exchange service publishes ExchangeProcessed to this exchange
// with the routing key below (transaction service also reads from this same exchange).
const CURRENCY_EXCHANGE_EVENTS = "currency-exchange-events";
const EXCHANGE_PROCESSED_ROUTING_KEY = "currency-exchange-response-queue";

const RABBITMQ_URL = process.env.RABBITMQ_URL ?? "amqp://localhost";
const WAIT_TIMEOUT_MS = 20_000;

// ── Types ────────────────────────────────────────────────────────────────────
type ExchangeProcessed = {
	data: {
		ownerId: string;
		accountGuid: string;
		accountName: string;
		amount: string;
		currency: string;
		transactionType: string;
		exchangeRate: number;
	};
	metadata: {
		messageType: string;
		messageTimestamp: string;
		messageId: string;
	};
};

// ── Shared state ─────────────────────────────────────────────────────────────
let connection: amqp.ChannelModel;
let channel: amqp.Channel;
let resultQueue: string;

beforeAll(async () => {
	connection = await amqp.connect(RABBITMQ_URL);
	channel = await connection.createChannel();

	// Assert the fanout exchange so we can publish to it even before
	// service-transaction has started and declared it.
	await channel.assertExchange(FRAUD_CHECKED_EXCHANGE, "fanout", { durable: true });

	// Assert the direct exchange so we can bind our listener queue.
	await channel.assertExchange(CURRENCY_EXCHANGE_EVENTS, "direct", { durable: true });

	// Exclusive, auto-delete queue — unique per test run, cleaned up on disconnect.
	const { queue } = await channel.assertQueue("", { exclusive: true });
	resultQueue = queue;

	await channel.bindQueue(resultQueue, CURRENCY_EXCHANGE_EVENTS, EXCHANGE_PROCESSED_ROUTING_KEY);
});

afterAll(async () => {
	await channel?.close();
	await connection?.close();
});

// ── Test ─────────────────────────────────────────────────────────────────────
describe("Currency exchange — end-to-end systems test", () => {
	it(
		"receives ExchangeProcessed with exchangeRate > 0 after calling the real Frankfurter API",
		async () => {
			// Set up listener before publishing so no message is missed.
			const received = new Promise<ExchangeProcessed>((resolve, reject) => {
				const timeout = setTimeout(
					() => reject(new Error(`ExchangeProcessed not received within ${WAIT_TIMEOUT_MS}ms`)),
					WAIT_TIMEOUT_MS,
				);

				channel.consume(
					resultQueue,
					(msg) => {
						if (!msg) return;
						clearTimeout(timeout);
						channel.ack(msg);
						resolve(JSON.parse(msg.content.toString()) as ExchangeProcessed);
					},
					{ noAck: false },
				);
			});

			// Publish a FraudChecked message — acting as the AI service.
			// The transaction service will pick this up from its "check-fraud" queue,
			// see transactionType "EXCHANGE", and forward it to the currency-exchange service.
			const payload = {
				data: {
					ownerId: "systems-test-owner",
					account: {
						guid: "systems-test-account-guid",
						name: "Systems Test Account",
						type: "SAVINGS",
					},
					amount: "100",
					transactionType: "EXCHANGE",
					currency: "USD",
				},
				metadata: {
					messageType: "TRANSACTION_EXCHANGE",
					messageTimestamp: new Date().toISOString(),
					messageId: uuidv4(),
				},
			};

			channel.publish(
				FRAUD_CHECKED_EXCHANGE,
				"", // fanout — routing key is ignored
				Buffer.from(JSON.stringify(payload)),
				{ persistent: true },
			);

			// Assert the full chain ran: transaction → currency-exchange → Frankfurter (2xx)
			const message = await received;

            console.log("test result: ", message);
            

			expect(message.data.exchangeRate).toBeGreaterThan(0);
			expect(message.data.currency).toBe("USD");
			expect(message.data.accountGuid).toBe("systems-test-account-guid");
		},
	);
});
