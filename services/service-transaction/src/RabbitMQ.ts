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

	public async sendToQueue(queue: string, message: T) {
		try {
			if (!this.channel) {
				throw new Error("RabbitMQ channel is not initialized. Call connect() first.");
			}

			await this.channel.assertQueue(queue, {
				durable: true
			});

			this.channel.sendToQueue(queue, Buffer.from(JSON.stringify(message)), { persistent: true });
			console.log("[X] Sent %s", message);
		} catch (error) {
			console.error(error);
			throw error;
		}
	}

	public async consumeFromQueue(queue: string, callback: (data: T) => void) {
		if (!this.channel) {
			throw new Error("RabbitMQ is not initialized. Call connect() first.");
		}

		await this.channel.assertQueue(queue, { durable: true });

		console.log(`[*] Waiting for messages in ${queue}. To exit press CTRL+C`);
		this.channel.consume(
			queue,
			(msg) => {
				if (!msg || !msg.content) {
					return console.error("Error: No message content received");
				}

				try {
					const data = JSON.parse(msg.content.toString()) as T;
					console.log(" [x] Received: ", data);
					callback(data);
				} catch (error) {
					console.error("Error parsing message content: ", msg.content);
					throw error;
				}
			},
			{ noAck: true }
		);
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
