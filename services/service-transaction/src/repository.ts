import type { PrismaClient } from "@prisma/client/extension";
import type { ITXClientDenyList } from "@prisma/client/runtime/client";
import type { BalanceDetail } from "../generated/prisma/client";

type TxClient = Omit<PrismaClient, ITXClientDenyList>;

export async function getLatestBalance(tx: TxClient, guid: string) {
	return (
		((await tx.balanceDetail.findFirst({
			where: {
				balance: {
					accountGuid: guid,
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
