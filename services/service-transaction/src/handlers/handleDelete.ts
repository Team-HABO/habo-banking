import type { TAccountPayload, TSynchronizeAccountDeletePayload } from "../events/account";
import { produceSynchronization } from "../producer";
import { deleteBalance } from "../repository";

export default async function handleDelete(payload: TAccountPayload) {
	console.log("Handling delete account:", payload);
	const { data, metadata } = payload.message;

	await deleteBalance(data.accountGuid);

	const message = {
		data: {
			ownerId: data.ownerId,
			account: {
				accountGuid: data.accountGuid,
				timestamp: data.timestamp
			}
		},
		metadata
	} as TSynchronizeAccountDeletePayload;

	await produceSynchronization<TSynchronizeAccountDeletePayload>(message, "synchronize-account-queue");
}
