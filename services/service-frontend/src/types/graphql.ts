export interface Audit {
    auditId: string;
    amount: string;
    type: string;
    timestamp: string;
    receiver: string | null;
    sender: string | null;
}

export interface Account {
    accountGuid: string;
    type: string;
    name: string;
    balance: string;
    isFrozen: boolean;
    audits: Audit[];
}
