import "dotenv/config";
import { describe, it, expect, beforeEach, afterEach } from "vitest";
import { prisma } from "../prisma/prisma";
import handleTransfer from "../src/handlers/handleTransfer";
import { ACCOUNT_GUID, OWNER_ID, RECEIVER_ACCOUNT_GUID, RECEIVER_OWNER_ID, cleanupBalance, makeTransferPayload } from "./helpers";

describe("handleTransfer database integration", () => {
	let senderBalanceId: number;
	let receiverBalanceId: number;

	beforeEach(async () => {
		const sender = await prisma.balance.create({
			data: {
				accountGuid: ACCOUNT_GUID,
				ownerId: OWNER_ID,
				balanceDetails: {
					create: { amount: "100" }
				}
			}
		});
		senderBalanceId = sender.id;

		const receiver = await prisma.balance.create({
			data: {
				accountGuid: RECEIVER_ACCOUNT_GUID,
				ownerId: RECEIVER_OWNER_ID,
				balanceDetails: {
					create: { amount: "200" }
				}
			}
		});
		receiverBalanceId = receiver.id;
	});

	afterEach(async () => {
		await cleanupBalance(ACCOUNT_GUID);
		await cleanupBalance(RECEIVER_ACCOUNT_GUID);
	});

	it("should debit sender and credit receiver after transfer", async () => {
		await handleTransfer(makeTransferPayload("50", ACCOUNT_GUID, RECEIVER_ACCOUNT_GUID));

		const senderLatest = await prisma.balanceDetail.findFirst({
			where: { balanceId: senderBalanceId },
			orderBy: { createdAt: "desc" }
		});
		const receiverLatest = await prisma.balanceDetail.findFirst({
			where: { balanceId: receiverBalanceId },
			orderBy: { createdAt: "desc" }
		});

		expect(senderLatest).not.toBeNull();
		expect(senderLatest!.amount).toBe("50"); // 100 - 50
		expect(receiverLatest).not.toBeNull();
		expect(receiverLatest!.amount).toBe("250"); // 200 + 50
	});

	it("should accumulate balances across multiple transfers", async () => {
		await handleTransfer(makeTransferPayload("30", ACCOUNT_GUID, RECEIVER_ACCOUNT_GUID));
		await handleTransfer(makeTransferPayload("20", ACCOUNT_GUID, RECEIVER_ACCOUNT_GUID, 1000));

		const senderDetails = await prisma.balanceDetail.findMany({
			where: { balanceId: senderBalanceId },
			orderBy: { createdAt: "asc" }
		});
		const receiverDetails = await prisma.balanceDetail.findMany({
			where: { balanceId: receiverBalanceId },
			orderBy: { createdAt: "asc" }
		});

		expect(senderDetails).toHaveLength(3);
		expect(senderDetails[0]!.amount).toBe("100"); // initial
		expect(senderDetails[1]!.amount).toBe("70"); // 100 - 30
		expect(senderDetails[2]!.amount).toBe("50"); // 70 - 20

		expect(receiverDetails).toHaveLength(3);
		expect(receiverDetails[0]!.amount).toBe("200"); // initial
		expect(receiverDetails[1]!.amount).toBe("230"); // 200 + 30
		expect(receiverDetails[2]!.amount).toBe("250"); // 230 + 20
	});

	it("should not throw when the sender account does not exist", async () => {
		const payload = makeTransferPayload("50", "non-existent-sender", RECEIVER_ACCOUNT_GUID);

		await expect(handleTransfer(payload)).resolves.toBeUndefined();
	});

	it("should not create balance details when the sender account does not exist", async () => {
		await handleTransfer(makeTransferPayload("50", "non-existent-sender", RECEIVER_ACCOUNT_GUID));

		const senderCount = await prisma.balanceDetail.count({ where: { balanceId: senderBalanceId } });
		const receiverCount = await prisma.balanceDetail.count({ where: { balanceId: receiverBalanceId } });
		expect(senderCount).toBe(1);
		expect(receiverCount).toBe(1);
	});

	it("should not process the same transfer twice", async () => {
		const payload = makeTransferPayload("50", ACCOUNT_GUID, RECEIVER_ACCOUNT_GUID);

		await handleTransfer(payload);
		await handleTransfer(payload);

		const senderDetails = await prisma.balanceDetail.findMany({
			where: { balanceId: senderBalanceId },
			orderBy: { createdAt: "asc" }
		});
		const receiverDetails = await prisma.balanceDetail.findMany({
			where: { balanceId: receiverBalanceId },
			orderBy: { createdAt: "asc" }
		});

		expect(senderDetails).toHaveLength(2); // initial + one transfer
		expect(senderDetails[1]!.amount).toBe("50"); // 100 - 50, applied only once

		expect(receiverDetails).toHaveLength(2); // initial + one transfer
		expect(receiverDetails[1]!.amount).toBe("250"); // 200 + 50, applied only once
	});

	it("should not transfer from a deleted balance", async () => {
		await prisma.deletedBalance.create({ data: { balanceId: senderBalanceId } });

		await handleTransfer(makeTransferPayload("50", ACCOUNT_GUID, RECEIVER_ACCOUNT_GUID));

		const senderCount = await prisma.balanceDetail.count({ where: { balanceId: senderBalanceId } });
		const receiverCount = await prisma.balanceDetail.count({ where: { balanceId: receiverBalanceId } });
		expect(senderCount).toBe(1);
		expect(receiverCount).toBe(1);
	});

	it("should create a transaction audit record after transfer", async () => {
		const payload = makeTransferPayload("50", ACCOUNT_GUID, RECEIVER_ACCOUNT_GUID);
		await handleTransfer(payload);

		const audits = await prisma.transactionAudit.findMany({
			where: { senderBalanceId: senderBalanceId }
		});

		expect(audits).toHaveLength(1);
		expect(audits[0]!.amount).toBe("50");
		expect(audits[0]!.transactionId).toBe(payload.metadata.messageId);
		expect(audits[0]!.senderBalanceId).toBe(senderBalanceId);
		expect(audits[0]!.receiverBalanceId).toBe(receiverBalanceId);
	});

	it("should only create one audit record when the same transfer event is processed twice", async () => {
		const payload = makeTransferPayload("50", ACCOUNT_GUID, RECEIVER_ACCOUNT_GUID);

		await handleTransfer(payload);
		await handleTransfer(payload);

		const audits = await prisma.transactionAudit.findMany({
			where: { senderBalanceId: senderBalanceId }
		});

		expect(audits).toHaveLength(1);
		expect(audits[0]!.transactionId).toBe(payload.metadata.messageId);
	});
});
