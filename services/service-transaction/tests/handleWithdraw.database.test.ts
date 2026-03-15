import "dotenv/config";
import { describe, it, expect, beforeEach, afterEach } from "vitest";
import { prisma } from "../prisma/prisma";
import handleWithdraw from "../src/handlers/handleWithdraw";
import { ACCOUNT_GUID, cleanupBalance, makePayload, OWNER_ID } from "./helpers";

describe("handleWithdraw database integration", () => {
	let balanceId: number;

	beforeEach(async () => {
		const balance = await prisma.balance.create({
			data: {
				accountGuid: ACCOUNT_GUID,
				ownerId: OWNER_ID,
				balanceDetails: {
					create: { amount: "100" }
				}
			}
		});
		balanceId = balance.id;
	});

	afterEach(async () => {
		await cleanupBalance(ACCOUNT_GUID);
	});

	it("should create a new balance detail with the difference after withdraw", async () => {
		await handleWithdraw(makePayload("50", ACCOUNT_GUID, 0, "WITHDRAW"));

		const latest = await prisma.balanceDetail.findFirst({
			where: { balanceId },
			orderBy: { createdAt: "desc" }
		});

		expect(latest).not.toBeNull();
		expect(latest!.amount).toBe("50");
	});

	it("should accumulate balance across multiple withdrawals", async () => {
		await handleWithdraw(makePayload("50", ACCOUNT_GUID, 0, "WITHDRAW"));
		await handleWithdraw(makePayload("25", ACCOUNT_GUID, 1000, "WITHDRAW"));

		const details = await prisma.balanceDetail.findMany({
			where: { balanceId },
			orderBy: { createdAt: "asc" }
		});

		expect(details).toHaveLength(3);
		expect(details[0]!.amount).toBe("100"); // initial seed
		expect(details[1]!.amount).toBe("50"); // 100 - 50
		expect(details[2]!.amount).toBe("25"); // 50 - 25
	});

	it("should not throw when the account does not exist", async () => {
		const payload = makePayload("50", ACCOUNT_GUID, 0, "WITHDRAW");
		payload.message.data.account.guid = "non-existent-guid-xyz";

		await expect(handleWithdraw(payload)).resolves.toBeUndefined();
	});

	it("should not create a new balance detail when the account does not exist", async () => {
		const payload = makePayload("50", ACCOUNT_GUID, 0, "WITHDRAW");
		payload.message.data.account.guid = "non-existent-guid-xyz";

		await handleWithdraw(payload);

		const count = await prisma.balanceDetail.count({ where: { balanceId } });
		expect(count).toBe(1);
	});

	it("should not process the same withdrawal twice", async () => {
		const payload = makePayload("50", ACCOUNT_GUID, 0, "WITHDRAW");

		await handleWithdraw(payload);
		await handleWithdraw(payload);

		const details = await prisma.balanceDetail.findMany({
			where: { balanceId },
			orderBy: { createdAt: "asc" }
		});

		expect(details).toHaveLength(2); // initial + one withdrawal
		expect(details[1]!.amount).toBe("50"); // 100 - 50, applied only once
	});

	it("should not withdraw from a deleted balance", async () => {
		await prisma.deletedBalance.create({ data: { balanceId } });

		await handleWithdraw(makePayload("50", ACCOUNT_GUID, 0, "WITHDRAW"));

		const count = await prisma.balanceDetail.count({ where: { balanceId } });
		expect(count).toBe(1);
	});

	it("should create a transaction audit record after withdrawal", async () => {
		const payload = makePayload("30", ACCOUNT_GUID, 0, "WITHDRAW");
		await handleWithdraw(payload);

		const audits = await prisma.transactionAudit.findMany({
			where: { senderBalanceId: balanceId }
		});

		expect(audits).toHaveLength(1);
		expect(audits[0]!.amount).toBe("30");
		expect(audits[0]!.transactionId).toBe(payload.message.metadata.messageId);
		expect(audits[0]!.senderBalanceId).toBe(balanceId);
		expect(audits[0]!.receiverBalanceId).toBe(balanceId);
	});

	it("should only create one audit record when the same withdrawal event is processed twice", async () => {
		const payload = makePayload("30", ACCOUNT_GUID, 0, "WITHDRAW");

		await handleWithdraw(payload);
		await handleWithdraw(payload);

		const audits = await prisma.transactionAudit.findMany({
			where: { senderBalanceId: balanceId }
		});

		expect(audits).toHaveLength(1);
		expect(audits[0]!.transactionId).toBe(payload.message.metadata.messageId);
	});
});
