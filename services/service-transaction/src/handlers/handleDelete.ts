import type { TAccountPayload } from "../events/account";
import { deleteBalance } from "../repository";

export default async function handleDelete(payload: TAccountPayload) {
	console.log("Handling delete account:", payload);
	const { data } = payload.message;

	const deleted = await deleteBalance(data.accountGuid);
	console.log(deleted);
}
