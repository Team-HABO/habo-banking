import { prisma } from "../../prisma/prisma";
import type { TAccountPayload, TSynchronizeAccountDeletePayload } from "../events/account";
import { produceSynchronization } from "../producer";
import { deleteBalance, getLatestBalance } from "../repository";

export default async function handleDelete(payload: TAccountPayload) {
	console.log("Handling delete account:", payload);
	const { data, metadata } = payload.message;

	const message = await prisma.$transaction(async (tx) => {
		const balanceExists = await getLatestBalance(tx, data.accountGuid);
		// Already deleted, idempotency
		if (!balanceExists) {
			return;
		}

		await deleteBalance(tx, data.accountGuid);

		return {
			data: {
				ownerId: data.ownerId,
				account: {
					accountGuid: data.accountGuid,
					timestamp: data.timestamp
				}
			},
			metadata
		} as TSynchronizeAccountDeletePayload;
	});

	if (message) {
		await produceSynchronization<TSynchronizeAccountDeletePayload>(message, "synchronize-account-queue");
	}
}
