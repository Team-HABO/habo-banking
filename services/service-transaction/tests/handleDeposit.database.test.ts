import "dotenv/config";
import { describe, it, expect, beforeEach, afterEach } from "vitest";
import { prisma } from "../prisma/prisma";
import handleDeposit from "../src/handlers/handleDeposit";
import { ACCOUNT_GUID, cleanupBalance, makePayload, OWNER_ID } from "./helpers";

describe("handleDeposit database integration", () => {
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

	it("should create a new balance detail with the sum after deposit", async () => {
		await handleDeposit(makePayload("50", ACCOUNT_GUID, 0, "DEPOSIT"));

		const latest = await prisma.balanceDetail.findFirst({
			where: { balanceId },
			orderBy: { createdAt: "desc" }
		});

		expect(latest).not.toBeNull();
		expect(latest!.amount).toBe("150");
	});

	it("should accumulate balance across multiple deposits", async () => {
		await handleDeposit(makePayload("50", ACCOUNT_GUID, 0, "DEPOSIT"));
		await handleDeposit(makePayload("25", ACCOUNT_GUID, 1000, "DEPOSIT"));

		const details = await prisma.balanceDetail.findMany({
			where: { balanceId },
			orderBy: { id: "asc" }
		});

		expect(details).toHaveLength(3);
		expect(details[0]!.amount).toBe("100"); // initial seed
		expect(details[1]!.amount).toBe("150"); // 100 + 50
		expect(details[2]!.amount).toBe("175"); // 150 + 25
	});

	it("should not throw when the account does not exist", async () => {
		const payload = makePayload("50", ACCOUNT_GUID, 0, "DEPOSIT");
		payload.data.account.guid = "non-existent-guid-xyz";

		await expect(handleDeposit(payload)).resolves.toBeUndefined();
	});

	it("should not create a new balance detail when the account does not exist", async () => {
		const payload = makePayload("50", ACCOUNT_GUID, 0, "DEPOSIT");
		payload.data.account.guid = "non-existent-guid-xyz";

		await handleDeposit(payload);

		const count = await prisma.balanceDetail.count({ where: { balanceId } });
		expect(count).toBe(1);
	});

	it("should not process the same deposit twice", async () => {
		const payload = makePayload("50", ACCOUNT_GUID, 0, "DEPOSIT");

		await handleDeposit(payload);
		await handleDeposit(payload);

		const details = await prisma.balanceDetail.findMany({
			where: { balanceId },
			orderBy: { id: "asc" }
		});

		expect(details).toHaveLength(2); // initial + one deposit
		expect(details[1]!.amount).toBe("150"); // 100 + 50, applied only once
	});

	it("should not deposit to a deleted balance", async () => {
		await prisma.deletedBalance.create({ data: { balanceId } });

		await handleDeposit(makePayload("50", ACCOUNT_GUID, 0, "DEPOSIT"));

		const count = await prisma.balanceDetail.count({ where: { balanceId } });
		expect(count).toBe(1);
	});

	it("should create a transaction audit record after deposit", async () => {
		const payload = makePayload("50", ACCOUNT_GUID, 0, "DEPOSIT");
		await handleDeposit(payload);

		const audits = await prisma.transactionAudit.findMany({
			where: { senderBalanceId: balanceId }
		});

		expect(audits).toHaveLength(1);
		expect(audits[0]!.amount).toBe("50");
		expect(audits[0]!.transactionId).toBe(payload.metadata.messageId);
		expect(audits[0]!.senderBalanceId).toBe(balanceId);
		expect(audits[0]!.receiverBalanceId).toBe(balanceId);
	});

	it("should only create one audit record when the same deposit event is processed twice", async () => {
		const payload = makePayload("50", ACCOUNT_GUID, 0, "DEPOSIT");

		await handleDeposit(payload);
		await handleDeposit(payload);

		const audits = await prisma.transactionAudit.findMany({
			where: { senderBalanceId: balanceId }
		});

		expect(audits).toHaveLength(1);
		expect(audits[0]!.transactionId).toBe(payload.metadata.messageId);
	});
});
