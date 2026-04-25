import "dotenv/config";
import { describe, it, expect, beforeEach, afterEach } from "vitest";
import { prisma } from "../prisma/prisma";
import handleExchange from "../src/handlers/handleExchange";
import type { TExchangeProcessedPayload } from "../src/events/transaction";
import { ACCOUNT_GUID, cleanupBalance, OWNER_ID } from "./helpers";
import { v4 as uuidv4 } from "uuid";

describe("handleExchange database integration", () => {
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

	function makeExchangePayload(amount: string, offsetMs = 0, accountGuid = ACCOUNT_GUID): TExchangeProcessedPayload {
		return {
			data: {
				ownerId: OWNER_ID,
				accountGuid,
				accountName: "Test Account",
				amount,
				currency: "EUR",
				transactionType: "EXCHANGE",
				exchangeRate: 0.5
			},
			metadata: {
				messageType: "TRANSACTION_EXCHANGE",
				messageTimestamp: new Date(Date.now() + offsetMs).toISOString(),
				messageId: uuidv4()
			}
		};
	}

	it("should create a new balance detail with exchanged subtraction", async () => {
		await handleExchange(makeExchangePayload("50"));

		const latest = await prisma.balanceDetail.findFirst({
			where: { balanceId },
			orderBy: { createdAt: "desc" }
		});

		expect(latest).not.toBeNull();
		expect(latest!.amount).toBe("75"); // 100 - (50 * 0.5)
	});

	it("should accumulate balance across multiple exchanges", async () => {
		await handleExchange(makeExchangePayload("20", 0));
		await handleExchange(makeExchangePayload("10", 1000));

		const details = await prisma.balanceDetail.findMany({
			where: { balanceId },
			orderBy: { createdAt: "asc" }
		});

		expect(details).toHaveLength(3);
		expect(details[0]!.amount).toBe("100"); // initial
		expect(details[1]!.amount).toBe("90"); // 100 - (20 * 0.5)
		expect(details[2]!.amount).toBe("85"); // 90 - (10 * 0.5)
	});

	it("should not throw when the account does not exist", async () => {
		const payload = makeExchangePayload("50", 0, "non-existent-guid-xyz");

		await expect(handleExchange(payload)).resolves.toBeUndefined();
	});

	it("should not create a new balance detail when the account does not exist", async () => {
		const payload = makeExchangePayload("50", 0, "non-existent-guid-xyz");

		await handleExchange(payload);

		const count = await prisma.balanceDetail.count({ where: { balanceId } });
		expect(count).toBe(1);
	});

	it("should not process the same exchange twice", async () => {
		const payload = makeExchangePayload("50");

		await handleExchange(payload);
		await handleExchange(payload);

		const details = await prisma.balanceDetail.findMany({
			where: { balanceId },
			orderBy: { createdAt: "asc" }
		});

		expect(details).toHaveLength(2); // initial + one exchange
		expect(details[1]!.amount).toBe("75"); // 100 - (50 * 0.5), applied once
	});

	it("should not apply an old exchange event", async () => {
		await handleExchange(makeExchangePayload("10", 1000));
		await handleExchange(makeExchangePayload("20", -1000));

		const details = await prisma.balanceDetail.findMany({
			where: { balanceId },
			orderBy: { createdAt: "asc" }
		});

		expect(details).toHaveLength(2); // initial + first exchange only
		expect(details[1]!.amount).toBe("95"); // 100 - (10 * 0.5)
	});

	it("should not exchange from a deleted balance", async () => {
		await prisma.deletedBalance.create({ data: { balanceId } });

		await handleExchange(makeExchangePayload("50"));

		const count = await prisma.balanceDetail.count({ where: { balanceId } });
		expect(count).toBe(1);
	});

	it("should create a transaction audit record after exchange", async () => {
		const payload = makeExchangePayload("50");
		await handleExchange(payload);

		const audits = await prisma.transactionAudit.findMany({
			where: { senderBalanceId: balanceId }
		});

		expect(audits).toHaveLength(1);
		expect(audits[0]!.amount).toBe("50");
		expect(audits[0]!.transactionId).toBe(payload.metadata.messageId);
		expect(audits[0]!.senderBalanceId).toBe(balanceId);
		expect(audits[0]!.receiverBalanceId).toBe(balanceId);
	});

	it("should only create one audit record when the same exchange event is processed twice", async () => {
		const payload = makeExchangePayload("50");

		await handleExchange(payload);
		await handleExchange(payload);

		const audits = await prisma.transactionAudit.findMany({
			where: { senderBalanceId: balanceId }
		});

		expect(audits).toHaveLength(1);
		expect(audits[0]!.transactionId).toBe(payload.metadata.messageId);
	});

	it("should throw when currency is missing", async () => {
		const payload = makeExchangePayload("50");
		(payload.data as { currency: string | null }).currency = null as unknown as string;

		await expect(handleExchange(payload)).rejects.toThrow();
	});

	it("should throw when exchangeRate is missing", async () => {
		const payload = makeExchangePayload("50");
		(payload.data as { exchangeRate: number | null }).exchangeRate = null as unknown as number;

		await expect(handleExchange(payload)).rejects.toThrow();
	});
});
