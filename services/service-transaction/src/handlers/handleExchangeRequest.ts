import type { TTransactionPayload } from "../events/transaction";

export default async function handleExchangeRequest(payload: TTransactionPayload) {
	console.log("Handling exchange request:", payload);
	const { data } = payload.message;

	if (!data.currency) {
		throw new Error(`'currency' is undefined`);
	}

	//TODO: Produce to currency exchange service
}
