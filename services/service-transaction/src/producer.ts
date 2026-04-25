import type { TSynchronizeTransactionPayload, TExchangeRequestedPayload } from "./events/transaction";
import { RabbitMQ } from "./RabbitMQ";

export async function produceNotification(payload: object) {
	const rabbit = new RabbitMQ<object>();

	try {
		await rabbit.connect();
		await rabbit.sendToExchange("notification-events", "direct", payload, "notification-queue");
	} finally {
		await rabbit.closeConnection();
	}
}

export async function produceSynchronization(payload: TSynchronizeTransactionPayload) {
	const rabbit = new RabbitMQ<TSynchronizeTransactionPayload>();
	try {
		await rabbit.connect();
		await rabbit.sendToExchange("synchronize-events", "direct", payload, "synchronize-transaction-queue");
	} finally {
		await rabbit.closeConnection();
	}
}

export async function produceCurrencyExchanger(payload: TExchangeRequestedPayload) {
	const rabbit = new RabbitMQ<TExchangeRequestedPayload>();
	try {
		await rabbit.connect();
		await rabbit.sendToExchange("currency-exchange-events", "direct", payload, "currency-exchange-requests-queue");
	} finally {
		await rabbit.closeConnection();
	}
}
