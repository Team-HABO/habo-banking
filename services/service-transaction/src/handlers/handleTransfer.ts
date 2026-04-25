import { prisma } from "../../prisma/prisma";
import type { TSynchronizeTransactionPayload, TTransactionPayload } from "../events/transaction";
import { produceNotification, produceSynchronization } from "../producer";
import { createAudit, getLatestBalance, updateBalance } from "../repository";
import { isAlreadyProcessed, isOlderEvent } from "../utils/helper";

export default async function handleTransfer(payload: TTransactionPayload) {
	console.log("Handling transfer:", payload);
	const { data, metadata } = payload;

	if (!data.receiver) {
		throw new Error("data.receiver is undefined");
	}

	const message = await prisma.$transaction(async (tx) => {
		const senderBalance = await getLatestBalance(tx, data.account.guid);
		if (!senderBalance) return;

		const receiverBalance = await getLatestBalance(tx, data.receiver!.guid);
		if (!receiverBalance) return;

		// If old event, discard.
		if (isOlderEvent(senderBalance.createdAt, new Date(metadata.messageTimestamp))) {
			return;
		}
		if (isOlderEvent(receiverBalance.createdAt, new Date(metadata.messageTimestamp))) {
			return;
		}

		// Idempotency
		if (await isAlreadyProcessed(metadata.messageId)) {
			return;
		}

		const newSenderAmount = Number(senderBalance.amount) - Number(data.amount);
		if (newSenderAmount < 0) {
			return `Insufficient funds. Current balance: ${senderBalance.amount}, requested withdrawal: ${data.amount}.`;
		}

		const newReceiverAmount = Number(receiverBalance.amount) + Number(data.amount);

		const updatedSenderBalance = await updateBalance(tx, newSenderAmount, senderBalance.balanceId);
		const updatedReceiverBalance = await updateBalance(tx, newReceiverAmount, receiverBalance.balanceId);

		// Audit
		const audit = await createAudit(tx, {
			senderBalanceId: senderBalance.balanceId,
			receiverBalanceId: receiverBalance.balanceId,
			amount: data.amount,
			transactionType: data.transactionType,
			transactionId: metadata.messageId
		});

		return {
			data: {
				ownerId: data.ownerId,
				account: {
					guid: data.account.guid,
					balance: {
						amount: updatedSenderBalance.amount,
						timestamp: updatedSenderBalance.createdAt
					},
					audits: {
						receiver: data.account.name,
						amount: data.amount,
						type: data.transactionType.toUpperCase(),
						timestamp: audit.createdAt
					}
				},
				receiver: {
					guid: data.receiver!.guid,
					balance: {
						amount: updatedReceiverBalance.amount,
						timestamp: updatedReceiverBalance.createdAt
					},
					audits: {
						receiver: data.account.name,
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
