import type { PrismaClient } from "@prisma/client/extension";
import type { ITXClientDenyList } from "@prisma/client/runtime/client";
import type { BalanceDetail } from "../generated/prisma/client";
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

export async function deleteBalance(accountGuid: string) {
	const balance = await prisma.balance.findFirst({
		where: { accountGuid }
	});
	if (!balance) throw new Error(`Could not find balance by accountGuid: ${accountGuid}`);

	const existing = await prisma.deletedBalance.findFirst({ where: { balanceId: balance.id } });
	if (existing) return existing;

	return await prisma.deletedBalance.create({
		data: { balanceId: balance.id }
	});
}

// export async function createBalance(tx: TxClient) {

// }

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
