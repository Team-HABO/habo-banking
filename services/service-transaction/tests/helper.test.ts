import { beforeEach, describe, expect, it, vi } from "vitest";

const { findUniqueMock } = vi.hoisted(() => ({
	findUniqueMock: vi.fn()
}));

vi.mock("../prisma/prisma", () => ({
	prisma: {
		transactionAudit: {
			findUnique: findUniqueMock
		}
	}
}));

import { isTransactionAlreadyProcessed, isOlderEvent } from "../src/utils/helper";

describe("utils/helper", () => {
	beforeEach(() => {
		findUniqueMock.mockReset();
	});

	describe("isOlderEvent", () => {
		it("returns true when saved date is newer than payload date", () => {
			const savedDate = new Date("2026-04-01T10:00:00.000Z");
			const payloadDate = new Date("2026-04-01T09:59:59.000Z");

			expect(isOlderEvent(savedDate, payloadDate)).toBe(true);
		});

		it("returns true when saved date equals payload date", () => {
			const savedDate = new Date("2026-04-01T10:00:00.000Z");
			const payloadDate = new Date("2026-04-01T10:00:00.000Z");

			expect(isOlderEvent(savedDate, payloadDate)).toBe(true);
		});

		it("returns false when saved date is older than payload date", () => {
			const savedDate = new Date("2026-04-01T09:59:59.000Z");
			const payloadDate = new Date("2026-04-01T10:00:00.000Z");

			expect(isOlderEvent(savedDate, payloadDate)).toBe(false);
		});
	});

	describe("isAlreadyProcessed", () => {
		it("returns true when an audit exists", async () => {
			findUniqueMock.mockResolvedValue({ id: 1, transactionId: "msg-1" });

			const result = await isTransactionAlreadyProcessed("msg-1");

			expect(findUniqueMock).toHaveBeenCalledWith({ where: { transactionId: "msg-1" } });
			expect(result).toBe(true);
		});

		it("returns false when no audit exists", async () => {
			findUniqueMock.mockResolvedValue(null);

			const result = await isTransactionAlreadyProcessed("msg-2");

			expect(findUniqueMock).toHaveBeenCalledWith({ where: { transactionId: "msg-2" } });
			expect(result).toBe(false);
		});
	});
});
