export type TMessagePayload = {
	message: string;
};

export type TMetadata = {
	messageType: string;
	messageTimestamp: string;
	messageId: string;
};

export type TData = {
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
		data: TData;
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
