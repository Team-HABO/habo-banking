#!/usr/bin/env node
import type { TMessagePayload } from "./events/message.js";
import { RabbitMQ } from "./RabbitMQ.js";

const rabbit = new RabbitMQ<TMessagePayload>();
await rabbit.connect();

const queue = "hello-queue";

await rabbit.consumeFromQueue(queue, (data: TMessagePayload) => {
	console.log(" [x] Processing %o", data);
});
