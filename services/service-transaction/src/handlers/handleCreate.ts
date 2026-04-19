import type { TAccountPayload, TSynchronizeAccountCreatePayload } from "../events/account";
import { prisma } from "../../prisma/prisma";
import { createBalance, getLatestBalance } from "../repository";
import { produceSynchronization } from "../producer";

export default async function handleCreate(payload: TAccountPayload) {
	console.log("Handling create account:", payload);
	const { data, metadata } = payload.message;

	const message = await prisma.$transaction(async (tx) => {
		const latestBalance = await getLatestBalance(tx, data.accountGuid);

		// Idempotency
		if (latestBalance) {
			return;
		}

		const balanceDetail = await createBalance(tx, data.accountGuid, data.ownerId);

		return {
			data: {
				ownerId: data.ownerId,
				account: {
					accountGuid: data.accountGuid,
					type: data.type,
					isFrozen: data.isFrozen,
					name: data.name,
					timestamp: data.timestamp,
					balance: {
						amount: balanceDetail.amount,
						timestamp: balanceDetail.createdAt
					}
				}
			},
			metadata
		} as TSynchronizeAccountCreatePayload;
	});

	if (message) {
		await produceSynchronization<TSynchronizeAccountCreatePayload>(message, "synchronize-account-queue");
	}
}
