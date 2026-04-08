import type { TSynchronizeTransactionPayload, TTransactionPayload } from "./events/transaction";
import { RabbitMQ } from "./RabbitMQ";

export async function produceNotification(payload: object) {
	const rabbit = new RabbitMQ<object>();

	try {
		await rabbit.connect();
		await rabbit.sendToExchange("notification-events", "direct", payload);
	} finally {
		await rabbit.closeConnection();
	}
}

export async function produceSynchronization(payload: TSynchronizeTransactionPayload) {
	const rabbit = new RabbitMQ<TSynchronizeTransactionPayload>();
	try {
		await rabbit.connect();
		await rabbit.sendToExchange("synchronize-events", "direct", payload, "synchronize-transaction");
	} finally {
		await rabbit.closeConnection();
	}
}

export async function produceCurrencyExchanger(payload: TTransactionPayload) {
	const rabbit = new RabbitMQ<TTransactionPayload>();
	try {
		await rabbit.connect();
		await rabbit.sendToExchange("currency-exchange-events", "direct", payload);
	} finally {
		await rabbit.closeConnection();
	}
}
