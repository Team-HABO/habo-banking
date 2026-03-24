import type { PrismaClient } from "@prisma/client/extension";
import type { ITXClientDenyList } from "@prisma/client/runtime/client";
import { prisma } from "../../prisma/prisma";
import type { TData, TTransactionPayload } from "../events/transaction";
import type { BalanceDetail } from "../../generated/prisma/client";

type TxClient = Omit<PrismaClient, ITXClientDenyList>;

export default async function handleExchange(payload: TTransactionPayload) {
	console.log("Handling exchange request from currency service:", payload);
	const { data, metadata } = payload.message;

	if (!data.currency) {
		throw new Error(`'currency' is undefined`);
	}

	if (!data.exchangeRate) {
		throw new Error(`'exchangeRate' is undefined`);
	}

	await prisma.$transaction(async (tx) => {
		const latestBalance = await getLatestBalance(tx, data);
		if (!latestBalance) return;

		// If old event, discard.
		if (latestBalance.createdAt >= new Date(metadata.messageTimestamp)) return;

		// Idempotency, if already processed, discard.
		const alreadyProcessed = await tx.transactionAudit.findUnique({
			where: { transactionId: metadata.messageId }
		});
		if (alreadyProcessed) return;

		const exchangeAmount = Number(data.amount) * data.exchangeRate!;
		const newAmount = Number(latestBalance.amount) - exchangeAmount;
		if (newAmount < 0) {
			//TODO: produce to Notifier service instead. Add early return
		}

		await withdrawAmount(tx, newAmount, latestBalance.balanceId);

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

async function withdrawAmount(tx: TxClient, amount: number, balanceId: number) {
	await tx.balanceDetail.create({
		data: {
			amount: String(amount),
			balanceId: balanceId
		}
	});
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
