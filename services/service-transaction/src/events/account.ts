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

// export type TSynchronizeTransactionPayload = {
//     data: {
//         ownerId: string;
//         account: TSynchronizeData;
//         receiver?: TSynchronizeData;
//     };
//     metadata: TMetadata;
// };

// type TSynchronizeData = {
//     guid: string;
//     balance: {
//         amount: string;
//         timestamp: Date;
//     };
//     audits: {
//         receiver: string;
//         amount: string;
//         type: string;
//         timestamp: Date;
//     };
// };
