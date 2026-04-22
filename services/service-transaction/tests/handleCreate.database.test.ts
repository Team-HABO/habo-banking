import "dotenv/config";
import { describe, it, expect, afterEach } from "vitest";
import { prisma } from "../prisma/prisma";
import handleCreate from "../src/handlers/handleCreate";
import type { TAccountPayload } from "../src/events/account";
import { ACCOUNT_GUID, cleanupBalance, OWNER_ID } from "./helpers";
import { v4 as uuidv4 } from "uuid";

function makeCreatePayload(accountGuid = ACCOUNT_GUID, ownerId = OWNER_ID): TAccountPayload {
	return {
		message: {
			data: {
				accountGuid,
				ownerId,
				type: "SAVINGS",
				name: "Test Account",
				isFrozen: false,
				timestamp: new Date().toISOString()
			},
			metadata: {
				messageType: "ACCOUNT_CREATE",
				messageTimestamp: new Date().toISOString(),
				messageId: uuidv4()
			}
		}
	};
}

describe("handleCreate database integration", () => {
	afterEach(async () => {
		await cleanupBalance(ACCOUNT_GUID);
	});

	it("should create a new balance with amount 0", async () => {
		await handleCreate(makeCreatePayload());

		const balance = await prisma.balance.findUnique({
			where: { accountGuid: ACCOUNT_GUID }
		});

		expect(balance).not.toBeNull();
		expect(balance!.accountGuid).toBe(ACCOUNT_GUID);
		expect(balance!.ownerId).toBe(OWNER_ID);
	});

	it("should create a balance detail with initial amount 0", async () => {
		await handleCreate(makeCreatePayload());

		const balance = await prisma.balance.findUnique({
			where: { accountGuid: ACCOUNT_GUID }
		});

		const details = await prisma.balanceDetail.findMany({
			where: { balanceId: balance!.id }
		});

		expect(details).toHaveLength(1);
		expect(details[0]!.amount).toBe("0");
	});

	it("should be idempotent when creating the same account twice", async () => {
		await handleCreate(makeCreatePayload());
		await handleCreate(makeCreatePayload());

		const balances = await prisma.balance.findMany({
			where: { accountGuid: ACCOUNT_GUID }
		});

		expect(balances).toHaveLength(1);
	});

	it("should not overwrite balance details on duplicate create", async () => {
		await handleCreate(makeCreatePayload());

		// Simulate a deposit by adding a balance detail
		const balance = await prisma.balance.findUnique({
			where: { accountGuid: ACCOUNT_GUID }
		});
		await prisma.balanceDetail.create({
			data: { balanceId: balance!.id, amount: "500" }
		});

		// Second create should not reset anything
		await handleCreate(makeCreatePayload());

		const details = await prisma.balanceDetail.findMany({
			where: { balanceId: balance!.id },
			orderBy: { id: "asc" }
		});

		expect(details).toHaveLength(2);
		expect(details[0]!.amount).toBe("0");
		expect(details[1]!.amount).toBe("500");
	});
});
