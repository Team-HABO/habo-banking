import type { TMetadata } from "./other";

export type TTransactionData = {
	ownerId: string;
	account: { guid: string; name: string; type: string };
	receiver?: { guid: string; name: string; type: string };
	amount: string;
	transactionType: string;
	currency?: string | null;
	exchangeRate?: number | null;
};

export type TTransactionPayload = {
	message: {
		data: TTransactionData;
		metadata: TMetadata;
	};
};

export type TSynchronizeTransactionPayload = {
	data: {
		ownerId: string;
		account: TSynchronizeData;
		receiver?: TSynchronizeData;
	};
	metadata: TMetadata;
};

type TSynchronizeData = {
	guid: string;
	balance: {
		amount: string;
		timestamp: Date;
	};
	audits: {
		receiver: string;
		amount: string;
		type: string;
		timestamp: Date;
	};
};
