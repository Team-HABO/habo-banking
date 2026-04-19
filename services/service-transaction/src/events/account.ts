import type { TMetadata } from "./other";

export type TAccountData = {
	accountGuid: string;
	ownerId: string;
	type?: string | null;
	name?: string | null;
	isFrozen?: boolean | null;
	timestamp: string;
};

export type TAccountPayload = {
	message: {
		data: TAccountData;
		metadata: TMetadata;
	};
};

export type TSynchronizeAccountDeletePayload = {
	data: {
		ownerId: string;
		account: {
			accountGuid: string;
			timestamp: string;
		};
	};
	metadata: TMetadata;
};

export type TSynchronizeAccountCreatePayload = {
	data: {
		ownerId: string;
		account: {
			accountGuid: string;
			type?: string | null;
			name?: string | null;
			isFrozen?: boolean | null;
			timestamp: string;
			balance: {
				amount: string;
				timestamp: Date;
			};
		};
	};
	metadata: TMetadata;
};
