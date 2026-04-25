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

// Received from AI service (FraudChecked) - raw JSON, no MassTransit envelope
export type TTransactionPayload = {
	data: TData;
	metadata: TMetadata;
};

// Sent to currency-exchange service (ExchangeRequested)
export type TExchangeRequestedPayload = {
	data: {
		ownerId: string;
		accountGuid: string;
		accountName: string;
		amount: string;
		currency: string;
		transactionType: string;
	};
	metadata: TMetadata;
};

// Received from currency-exchange service (ExchangeProcessed)
export type TExchangeProcessedPayload = {
	data: {
		ownerId: string;
		accountGuid: string;
		accountName: string;
		amount: string;
		currency: string;
		transactionType: string;
		exchangeRate: number;
	};
	metadata: TMetadata;
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
