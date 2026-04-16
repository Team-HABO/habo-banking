import type { TAccountPayload } from "../events/account";

export default async function handleDelete(payload: TAccountPayload) {
	console.log("Handling delete account:", payload);
}
