#!/usr/bin/env node
import { RabbitMQ } from "./RabbitMQ.js";
import type { TTransactionPayload } from "./events/transaction.js";
import handleDeposit from "./handlers/handleDeposit.js";
import handleExchange from "./handlers/handleExchange.js";
import handleExchangeRequest from "./handlers/handleExchangeRequest.js";
import handleTransfer from "./handlers/handleTransfer.js";
import handleWithdraw from "./handlers/handleWithdraw.js";

const handlers: Record<string, (data: TTransactionPayload) => Promise<void>> = {
	TRANSFER: handleTransfer,
	DEPOSIT: handleDeposit,
	WITHDRAW: handleWithdraw,
	EXCHANGE: handleExchangeRequest
};

const rabbit = new RabbitMQ<TTransactionPayload>();
await rabbit.connect();

await rabbit.consumeFromExchange("check-fraud", "service_ai.Messages:FraudChecked", "fanout", async (data, ack, nack) => {
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

await rabbit.consumeFromExchange("exchange-rate", "service_currency_exchange.Messages:change_me", "fanout", async (data, ack, nack) => {
	try {
		await handleExchange(data);
		ack();
	} catch (error) {
		console.error(`Handler failed for event ${data}. Error: `, error);
		nack(true);
	}
});
