#!/usr/bin/env node
import { RabbitMQ } from "./RabbitMQ.js";
import type { TTransactionPayload } from "./events/transaction.js";
import handleDeposit from "./handlers/handleDeposit.js";
import handleTransfer from "./handlers/handleTransfer.js";
import handleWithdraw from "./handlers/handleWithdraw.js";

const handlers: Record<string, (data: TTransactionPayload) => Promise<void>> = {
	TRANSFER: handleTransfer,
	DEPOSIT: handleDeposit,
	WITHDRAW: handleWithdraw
};

const rabbit = new RabbitMQ<TTransactionPayload>();
await rabbit.connect();

const queue = "check-fraud";
const exchange = "service_ai.Messages:FraudChecked";

await rabbit.consumeFromExchange(queue, exchange, "fanout", async (data, ack, nack) => {
	const transactionType = data.message.data.transactionType.toUpperCase();
	const handler = handlers[transactionType];

	if (!handler) {
		console.error(`No handler registered for transactionType: ${transactionType}`);
		nack(false);
		return;
	}

	try {
		await handler(data);
		ack();
	} catch (error) {
		console.error(`Handler failed for transactionType: ${transactionType}`, error);
		nack(true);
	}
});
