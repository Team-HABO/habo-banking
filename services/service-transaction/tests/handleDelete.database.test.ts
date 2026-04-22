import "dotenv/config";
import { describe, it, expect, beforeEach, afterEach } from "vitest";
import { prisma } from "../prisma/prisma";
import handleDelete from "../src/handlers/handleDelete";
import type { TAccountPayload } from "../src/events/account";
import { ACCOUNT_GUID, cleanupBalance, OWNER_ID } from "./helpers";
import { v4 as uuidv4 } from "uuid";

function makeDeletePayload(accountGuid = ACCOUNT_GUID): TAccountPayload {
	return {
		message: {
			data: {
				accountGuid,
				ownerId: OWNER_ID,
				timestamp: new Date().toISOString()
			},
			metadata: {
				messageType: "ACCOUNT_DELETE",
				messageTimestamp: new Date().toISOString(),
				messageId: uuidv4()
			}
		}
	};
}

describe("handleDelete database integration", () => {
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

	it("should create a deleted balance record", async () => {
		await handleDelete(makeDeletePayload());

		const deleted = await prisma.deletedBalance.findFirst({
			where: { balanceId }
		});

		expect(deleted).not.toBeNull();
		expect(deleted!.balanceId).toBe(balanceId);
	});

	it("should be idempotent when deleting the same balance twice", async () => {
		await handleDelete(makeDeletePayload());
		await handleDelete(makeDeletePayload());

		const deletedRecords = await prisma.deletedBalance.findMany({
			where: { balanceId }
		});

		expect(deletedRecords).toHaveLength(1);
	});

	it("should not affect balance details when deleting", async () => {
		await handleDelete(makeDeletePayload());

		const details = await prisma.balanceDetail.findMany({
			where: { balanceId }
		});

		expect(details).toHaveLength(1);
		expect(details[0]!.amount).toBe("100");
	});
});
