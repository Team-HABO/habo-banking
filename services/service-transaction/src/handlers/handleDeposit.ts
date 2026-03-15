import { prisma } from "../../prisma/prisma";
import type { TData, TTransactionPayload } from "../events/transaction";
import type { BalanceDetail, PrismaClient } from "../../generated/prisma/client";
import type { ITXClientDenyList } from "@prisma/client/runtime/client";

type TxClient = Omit<PrismaClient, ITXClientDenyList>;

export default async function handleDeposit(payload: TTransactionPayload) {
	console.log("Handling deposit:", payload);
	const { data, metadata } = payload.message;

	await prisma.$transaction(async (tx) => {
		const latestBalance = await getLatestBalance(tx, data);
		if (!latestBalance) return;

		// If old event, discard.
		if (latestBalance.createdAt >= new Date(metadata.messageTimestamp)) return;

		// Idempotency, if already processed, discard.
		const existingAudit = await tx.transactionAudit.findUnique({
			where: { transactionId: metadata.messageId }
		});
		if (existingAudit) return;

		const newAmount = Number(latestBalance.amount) + Number(data.amount);
		await depositAmount(tx, newAmount, latestBalance.balanceId);

		// Audit
		await createAudit(tx, {
			senderBalanceId: latestBalance.balanceId,
			receiverBalanceId: latestBalance.balanceId,
			amount: data.amount,
			transactionType: data.transactionType,
			transactionId: metadata.messageId
		});
	});

	//TODO: Produce to synchronizer
}

async function depositAmount(tx: TxClient, amount: number, balanceId: number) {
	await tx.balanceDetail.create({
		data: {
			amount: String(amount),
			balanceId: balanceId
		}
	});
}

async function getLatestBalance(tx: TxClient, data: TData) {
	return (
		((await tx.balanceDetail.findFirst({
			where: {
				balance: {
					accountGuid: data.account.guid,
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

async function createAudit(
	tx: TxClient,
	params: {
		senderBalanceId: number;
		receiverBalanceId: number;
		amount: string;
		transactionType: string;
		transactionId: string;
	}
) {
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


