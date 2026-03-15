import { prisma } from "../../prisma/prisma";
import type { TTransactionPayload } from "../events/transaction";
import type { BalanceDetail, PrismaClient } from "../../generated/prisma/client";
import type { ITXClientDenyList } from "@prisma/client/runtime/client";

type TxClient = Omit<PrismaClient, ITXClientDenyList>;

export default async function handleTransfer(payload: TTransactionPayload) {
	console.log("Handling transfer:", payload);
	const { data, metadata } = payload.message;

	if (!data.receiver) {
		throw new Error("data.receiver is undefined");
	}

	await prisma.$transaction(async (tx) => {
		const senderBalance = await getLatestBalance(tx, data.account.guid);
		if (!senderBalance) return;

		const receiverBalance = await getLatestBalance(tx, data.receiver!.guid);
		if (!receiverBalance) return;

		// If old event, discard.
		if (senderBalance.createdAt >= new Date(metadata.messageTimestamp)) return;
		if (receiverBalance.createdAt >= new Date(metadata.messageTimestamp)) return;

		// Idempotency, if already processed, discard.
		const alreadyProcessed = await tx.transactionAudit.findUnique({
			where: { transactionId: metadata.messageId }
		});
		if (alreadyProcessed) return;

		const newSenderAmount = Number(senderBalance.amount) - Number(data.amount);
		if (newSenderAmount < 0) {
			//TODO: produce to Notifier service instead. Add throw.
		}

		const newReceiverAmount = Number(receiverBalance.amount) + Number(data.amount);

		await updateBalance(tx, newSenderAmount, senderBalance.balanceId);
		await updateBalance(tx, newReceiverAmount, receiverBalance.balanceId);

		// Audit
		await createAudit(tx, {
			senderBalanceId: senderBalance.balanceId,
			receiverBalanceId: receiverBalance.balanceId,
			amount: data.amount,
			transactionType: data.transactionType,
			transactionId: metadata.messageId
		});
	});

	//TODO: Produce to synchronizer
}

async function getLatestBalance(tx: TxClient, accountGuid: string) {
	return (
		((await tx.balanceDetail.findFirst({
			where: {
				balance: {
					accountGuid: accountGuid,
					deletedBalances: {
						// Balance has not been deleted
						none: {}
					}
				}
			},
			orderBy: { createdAt: "desc" }
		})) as BalanceDetail) || null
	);
}

async function updateBalance(tx: TxClient, amount: number, balanceId: number) {
	await tx.balanceDetail.create({
		data: {
			amount: String(amount),
			balanceId: balanceId
		}
	});
}

async function createAudit(tx: TxClient, params: {
	senderBalanceId: number;
	receiverBalanceId: number;
	amount: string;
	transactionType: string;
	transactionId: string;
}) {
	const type = await tx.transactionType.findFirst({
		where: { name: params.transactionType }
	});
	if (!type) throw new Error(`Unknown transaction type: ${params.transactionType}`);

	await tx.transactionAudit.create({
		data: {
			senderBalanceId: params.senderBalanceId,
			receiverBalanceId: params.receiverBalanceId,
			amount: params.amount,
			typeId: type.id,
			transactionId: params.transactionId
		}
	});
}
