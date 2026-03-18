import { prisma } from "../prisma/prisma";
import type { TTransactionPayload } from "../src/events/transaction";
import { v4 as uuidv4 } from "uuid";

export const ACCOUNT_GUID = "test-deposit-account-guid";
export const OWNER_ID = "test-deposit-owner-id";

export const RECEIVER_ACCOUNT_GUID = "test-receiver-account-guid";
export const RECEIVER_OWNER_ID = "test-receiver-owner-id";

export const QUEUE = "transaction-service";
export const EXCHANGE = "transaction-exchange";

export function makePayload(amount: string, accountGuid = ACCOUNT_GUID, offsetMs = 0, transactionType: string): TTransactionPayload {
	return {
		message: {
			data: {
				account: { guid: accountGuid, name: "Test Account", type: "SAVINGS" },
				amount,
				transactionType
			},
			metadata: {
				messageType: "TRANSACTION",
				messageTimestamp: new Date(Date.now() + offsetMs).toISOString(),
				messageId: uuidv4() // Generate random GUID
			}
		}
	};
}

export function makeTransferPayload(
	amount: string,
	senderGuid: string,
	receiverGuid: string,
	offsetMs = 0
): TTransactionPayload {
	return {
		message: {
			data: {
				account: { guid: senderGuid, name: "Sender Account", type: "SAVINGS" },
				receiver: { guid: receiverGuid, name: "Receiver Account", type: "SAVINGS" },
				amount,
				transactionType: "TRANSFER"
			},
			metadata: {
				messageType: "TRANSACTION_TRANSFER",
				messageTimestamp: new Date(Date.now() + offsetMs).toISOString(),
				messageId: uuidv4()
			}
		}
	};
}

export async function cleanupBalance(accountGuid: string) {
	const balance = await prisma.balance.findUnique({ where: { accountGuid } });
	if (!balance) return;

	await prisma.transactionAudit.deleteMany({ where: { OR: [{ senderBalanceId: balance.id }, { receiverBalanceId: balance.id }] } });
	await prisma.deletedBalance.deleteMany({ where: { balanceId: balance.id } });
	await prisma.balanceDetail.deleteMany({ where: { balanceId: balance.id } });
	await prisma.balance.delete({ where: { id: balance.id } });
}
