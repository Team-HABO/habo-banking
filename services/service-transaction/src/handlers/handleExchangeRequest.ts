import type { TTransactionPayload } from "../events/transaction";
import { produceCurrencyExchanger } from "../producer";

export default async function handleExchangeRequest(payload: TTransactionPayload) {
	console.log("Handling exchange request:", payload);
	const { data, metadata } = payload;

	if (!data.currency) {
		throw new Error(`'currency' is undefined`);
	}

	await produceCurrencyExchanger({
		data: {
			ownerId: data.ownerId,
			accountGuid: data.account.guid,
			accountName: data.account.name,
			amount: data.amount,
			currency: data.currency,
			transactionType: data.transactionType
		},
		metadata
	});
}
