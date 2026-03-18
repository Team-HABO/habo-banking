export type TMessagePayload = {
	message: string;
};

export type TMetadata = {
	messageType: string;
	messageTimestamp: string;
	messageId: string;
};

export type TData = {
	account: { guid: string; name: string; type: string };
	receiver?: { guid: string; name: string; type: string };
	amount: string;
	transactionType: string;
};

export type TTransactionPayload = {
	message: {
		data: TData;
		metadata: TMetadata;
	};
};
