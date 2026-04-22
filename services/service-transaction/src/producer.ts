import type { TTransactionPayload } from "./events/transaction";
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

export async function produceSynchronization<T>(payload: T, routingKey: string) {
	const rabbit = new RabbitMQ<T>();
	try {
		await rabbit.connect();
		await rabbit.sendToExchange("synchronize-events", "direct", payload, routingKey);
	} finally {
		await rabbit.closeConnection();
	}
}

export async function produceCurrencyExchanger(payload: TTransactionPayload) {
	const rabbit = new RabbitMQ<TTransactionPayload>();
	try {
		await rabbit.connect();
		await rabbit.sendToExchange("currency-exchange-events", "direct", payload, "currency-exchange-requests-queue");
	} finally {
		await rabbit.closeConnection();
	}
}
