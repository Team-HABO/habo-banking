#!/usr/bin/env node
import type { TMessagePayload } from "./events/message.js";
import { RabbitMQ } from "./RabbitMQ.js";

const rabbit = new RabbitMQ<TMessagePayload>();

try {
	await rabbit.connect();

	const queue = "hello-queue";
	const msg = { message: "Hello World!" };

	await rabbit.sendToQueue(queue, msg);
} finally {
	await rabbit.closeConnection();
}
