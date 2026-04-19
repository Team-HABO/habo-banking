import type { PrismaClient } from "@prisma/client/extension";
import type { ITXClientDenyList } from "@prisma/client/runtime/client";
import type { Balance, BalanceDetail } from "../generated/prisma/client";
import { prisma } from "../prisma/prisma";

type TxClient = Omit<PrismaClient, ITXClientDenyList>;

export async function getLatestBalance(tx: TxClient, accountGuid: string) {
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

export async function updateBalance(tx: TxClient, amount: number, balanceId: number) {
	return await tx.balanceDetail.create({
		data: {
			amount: String(amount),
			balanceId: balanceId
		}
	});
}

export async function deleteBalance(tx: TxClient, accountGuid: string) {
	const balance = await tx.balance.findFirst({
		where: { accountGuid }
	});
	if (!balance) throw new Error(`Could not find balance by accountGuid: ${accountGuid}`);

	return await prisma.deletedBalance.upsert({
		where: { balanceId: balance.id },
		create: { balanceId: balance.id },
		update: {}
	});
}

export async function createBalance(tx: TxClient, accountGuid: string, ownerId: string) {
	const balance: Balance = await tx.balance.create({
		data: {
			ownerId,
			accountGuid
		}
	});

	const detail: BalanceDetail = await tx.balanceDetail.create({
		data: {
			balanceId: balance.id,
			amount: "0"
		}
	});

	return detail;
}

export async function createAudit(
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

	return await tx.transactionAudit.create({
		data: {
			senderBalanceId: params.senderBalanceId,
			receiverBalanceId: params.receiverBalanceId,
			amount: params.amount,
			typeId: type.id,
			transactionId: params.transactionId
		}
	});
}
