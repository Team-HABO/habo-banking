import { prisma } from "../../prisma/prisma";
import type { TSynchronizeTransactionPayload, TExchangeProcessedPayload } from "../events/transaction";
import { produceNotification, produceSynchronization } from "../producer";
import { createAudit, getLatestBalance, updateBalance } from "../repository";
import { isAlreadyProcessed, isOlderEvent } from "../utils/helper";

export default async function handleExchange(payload: TExchangeProcessedPayload) {
	console.log("Handling exchange request from currency service:", payload);
	const { data, metadata } = payload;

	if (!data.currency) {
		throw new Error(`'currency' is undefined`);
	}

	if (!data.exchangeRate) {
		throw new Error(`'exchangeRate' is undefined`);
	}

	const message = await prisma.$transaction(async (tx) => {
		const latestBalance = await getLatestBalance(tx, data.accountGuid);
		if (!latestBalance) return;

		if (isOlderEvent(latestBalance.createdAt, new Date(metadata.messageTimestamp))) {
			return;
		}

		// Idempotency
		if (await isAlreadyProcessed(metadata.messageId)) {
			return;
		}

		const exchangeAmount = Number(data.amount) * data.exchangeRate!;
		const newAmount = Number(latestBalance.amount) - exchangeAmount;
		if (newAmount < 0) {
			return `Insufficient funds. Current balance: ${latestBalance.amount}, requested exchange: ${data.amount}.`;
		}

		const updatedBalance = await updateBalance(tx, newAmount, latestBalance.balanceId);

		// Audit
		const audit = await createAudit(tx, {
			senderBalanceId: latestBalance.balanceId,
			receiverBalanceId: latestBalance.balanceId,
			amount: data.amount,
			transactionType: data.transactionType,
			transactionId: metadata.messageId
		});

		// Payload to synchronizer
		return {
			data: {
				ownerId: data.ownerId,
				account: {
					guid: data.accountGuid,
					balance: {
						amount: updatedBalance.amount,
						timestamp: updatedBalance.createdAt
					},
					audits: {
						receiver: data.accountName,
						amount: data.amount,
						type: data.transactionType.toUpperCase(),
						timestamp: audit.createdAt
					}
				}
			},
			metadata
		} as TSynchronizeTransactionPayload;
	});

	// Publish notification
	if (typeof message === "string") {
		console.warn(message);
		const payload = {
			data: {
				message
			},
			metadata
		};
		await produceNotification(payload);
		return;
	}

	// Publish to synchronizer after transaction succeeds
	if (message) {
		await produceSynchronization(message);
	}
}
