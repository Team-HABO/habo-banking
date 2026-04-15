import { prisma } from "../../prisma/prisma";
import type { TSynchronizeTransactionPayload, TTransactionPayload } from "../events/transaction";
import { produceNotification, produceSynchronization } from "../producer";
import { createAudit, getLatestBalance, updateBalance } from "../repository";

export default async function handleWithdraw(payload: TTransactionPayload) {
	console.log("Handling withdraw:", payload);
	const { data, metadata } = payload.message;

	const message = await prisma.$transaction(async (tx) => {
		const latestBalance = await getLatestBalance(tx, data.account.guid);
		if (!latestBalance) return;

		// If old event, discard.
		if (latestBalance.createdAt >= new Date(metadata.messageTimestamp)) return;

		// Idempotency, if already processed, discard.
		const alreadyProcessed = await tx.transactionAudit.findUnique({
			where: { transactionId: metadata.messageId }
		});
		if (alreadyProcessed) return;

		const newAmount = Number(latestBalance.amount) - Number(data.amount);
		if (newAmount < 0) {
			return `Insufficient funds. Current balance: ${latestBalance.amount}, requested withdrawal: ${data.amount}.`;
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
					guid: data.account.guid,
					balance: {
						amount: updatedBalance.amount,
						timestamp: updatedBalance.createdAt
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
