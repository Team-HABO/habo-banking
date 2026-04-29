#!/usr/bin/env node
import { RabbitMQ } from "./RabbitMQ.js";
import type { TAccountPayload } from "./events/account.js";
import type { TExchangeProcessedPayload, TTransactionPayload } from "./events/transaction.js";
import handleCreate from "./handlers/handleCreate.js";
import handleDelete from "./handlers/handleDelete.js";
import handleDeposit from "./handlers/handleDeposit.js";
import handleExchange from "./handlers/handleExchange.js";
import handleExchangeRequest from "./handlers/handleExchangeRequest.js";
import handleTransfer from "./handlers/handleTransfer.js";
import handleWithdraw from "./handlers/handleWithdraw.js";

const transactionHandlers: Record<string, (data: TTransactionPayload) => Promise<void>> = {
	TRANSFER: handleTransfer,
	DEPOSIT: handleDeposit,
	WITHDRAW: handleWithdraw,
	EXCHANGE: handleExchangeRequest
};

const accountHandlers: Record<string, (data: TAccountPayload) => Promise<void>> = {
	ACCOUNT_CREATE: handleCreate,
	ACCOUNT_DELETE: handleDelete
};

const transactionRabbit = new RabbitMQ<TTransactionPayload>();
await transactionRabbit.connect();

const accountRabbit = new RabbitMQ<TAccountPayload>();
await accountRabbit.connect();

await transactionRabbit.consumeFromExchange("check-fraud", "service_ai.Messages:FraudChecked", "fanout", async (data, ack, nack) => {
	const transactionType = data.data.transactionType.toUpperCase();
	const handler = transactionHandlers[transactionType];

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

const exchangeRabbit = new RabbitMQ<TExchangeProcessedPayload>();
await exchangeRabbit.connect();

await exchangeRabbit.consumeFromExchange(
	"currency-exchange-response-queue",
	"currency-exchange-events",
	"direct",
	async (data, ack, nack) => {
		try {
			await handleExchange(data);
			ack();
		} catch (error) {
			console.error(`Handler failed for event ${JSON.stringify(data)}. Error: `, error);
			nack(true);
		}
	},
	"currency-exchange-response-queue"
);

await accountRabbit.consumeFromExchange("account-queue", "account-exchange-events", "fanout", async (data, ack, nack) => {
	const messageType = data.message.metadata.messageType.toUpperCase();
	const handler = accountHandlers[messageType];

	if (!handler) {
		console.error(`No handler registered for messageType: ${messageType}`);
		nack(false);
		return;
	}

	try {
		await handler(data);
		ack();
	} catch (error) {
		console.error(`Handler failed for event ${JSON.stringify(data)}. Error: `, error);
		nack(true);
	}
});
