import type { TAccountPayload } from "../events/account";

export default async function handleCreate(payload: TAccountPayload) {
	console.log("Handling create account:", payload);
}
