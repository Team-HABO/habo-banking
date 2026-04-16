import * as amqp from "amqplib";

export class RabbitMQ<T> {
	private connection!: amqp.ChannelModel | null;
	private channel!: amqp.Channel | null;

	public async connect(): Promise<void> {
		if (!this.connection) {
			this.connection = await amqp.connect(`amqp://${process.env.RABBITMQ_HOST ?? "localhost"}`);
			this.channel = await this.connection.createChannel();

			this.connection.on("close", () => {
				console.error("RabbitMQ connection closed.");
			});

			this.connection.on("error", (err) => {
				console.error("RabbitMQ connection error:", err);
			});

			console.log("RabbitMQ connected and channel created successfully.");
		}
	}

	public async sendToExchange(exchange: string, exchangeType: string, message: T, routingKey = "") {
		try {
			if (!this.channel) {
				throw new Error("RabbitMQ channel is not initialized. Call connect() first.");
			}

			await this.channel.assertExchange(exchange, exchangeType, { durable: true });

			this.channel.publish(exchange, routingKey, Buffer.from(JSON.stringify(message)), {
				persistent: true
			});
			console.log("[X] Published to exchange %s with routing key %s", exchange, routingKey || "<empty>");
		} catch (error) {
			console.error(error);
			throw error;
		}
	}

	public async consumeFromExchange(
		queue: string,
		exchange: string,
		exchangeType: string,
		callback: (data: T, ack: () => void, nack: (requeue?: boolean) => void) => void,
		bindingKey = "",
		maxRetries = 3
	) {
		try {
			if (!this.channel) {
				throw new Error("RabbitMQ is not initialized. Call connect() first.");
			}

			const dlxExchange = `${exchange}.dlx`;
			const dlqName = `${queue}.dlq`;

			await this.channel.assertExchange(exchange, exchangeType, { durable: true });
			await this.channel.assertExchange(dlxExchange, "direct", { durable: true });
			await this.channel.assertQueue(dlqName, { durable: true });
			await this.channel.bindQueue(dlqName, dlxExchange, queue);
			await this.channel.assertQueue(queue, {
				durable: true,
				arguments: {
					"x-dead-letter-exchange": dlxExchange,
					"x-dead-letter-routing-key": queue
				}
			});
			await this.channel.bindQueue(queue, exchange, bindingKey);

			console.log(`[*] Waiting for messages in ${queue} via exchange ${exchange}. To exit press CTRL+C`);
			this.channel.consume(
				queue,
				(msg) => {
					if (!msg || !msg.content) {
						return console.error("Error: No message content received");
					}

					try {
						const data = JSON.parse(msg.content.toString()) as T;
						console.log(" [x] Received: ", data);

						const retryCount = (msg.properties.headers?.["x-retry-count"] as number) ?? 0;

						const ack = () => this.channel!.ack(msg);
						const nack = (requeue = true) => {
							if (!requeue) {
								this.channel!.nack(msg, false, false);
								return;
							}
							if (retryCount >= maxRetries) {
								console.error(`[!] Max retries (${maxRetries}) reached for queue "${queue}". Dead-lettering message.`);
								this.channel!.nack(msg, false, false);
							} else {
								console.warn(`[~] Retrying message (attempt ${retryCount + 1}/${maxRetries}) for queue "${queue}".`);
								this.channel!.publish(exchange, bindingKey, msg.content, {
									persistent: true,
									headers: { ...msg.properties.headers, "x-retry-count": retryCount + 1 }
								});
								this.channel!.ack(msg);
							}
						};

						callback(data, ack, nack);
					} catch (error) {
						console.error("Error parsing message content: " + msg.content + ", Error: " + error);
						this.channel!.nack(msg, false, false);
					}
				},
				{ noAck: false }
			);
		} catch (error) {
			console.error(error);
			throw error;
		}
	}

	public async closeConnection(): Promise<void> {
		if (this.channel) {
			await this.channel.close();
			this.channel = null;
		}
		if (this.connection) {
			await this.connection.close();
			this.connection = null;
		}
	}

	public getChannel(): amqp.Channel {
		if (!this.channel) {
			throw new Error("RabbitMQ is not initialized. Call connect() first.");
		}
		return this.channel;
	}

	public getConnection(): amqp.ChannelModel {
		if (!this.connection) {
			throw new Error("RabbitMQ is not initialized. Call connect() first.");
		}
		return this.connection;
	}
}
