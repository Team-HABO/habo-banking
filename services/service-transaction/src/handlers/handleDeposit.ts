import { prisma } from "../../prisma/prisma";
import type { TSynchronizeTransactionPayload, TTransactionPayload } from "../events/transaction";
import { produceSynchronization } from "../producer";
import { createAudit, getLatestBalance, updateBalance } from "../repository";
import { isTransactionAlreadyProcessed, isOlderEvent } from "../utils/helper";

export default async function handleDeposit(payload: TTransactionPayload) {
	console.log("Handling deposit:", payload);
	const { data, metadata } = payload.message;

	const message = await prisma.$transaction(async (tx) => {
		const latestBalance = await getLatestBalance(tx, data.account.guid);
		if (!latestBalance) return;

		if (isOlderEvent(latestBalance.createdAt, new Date(metadata.messageTimestamp))) {
			return;
		}

		// Idempotency
		if (await isTransactionAlreadyProcessed(metadata.messageId)) {
			return;
		}

		const newAmount = Number(latestBalance.amount) + Number(data.amount);
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

	// Publish to synchronizer after transaction succeeds
	if (message) {
		await produceSynchronization<TSynchronizeTransactionPayload>(message, "synchronize-transaction-queue");
	}
}
