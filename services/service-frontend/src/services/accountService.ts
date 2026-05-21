import axios from "axios";

const accountApiUrl = import.meta.env.VITE_ACCOUNT_API_URL;

function getBaseUrl(): string {
    if (!accountApiUrl) {
        throw new Error("Missing VITE_ACCOUNT_API_URL in environment variables.");
    }
    return accountApiUrl;
}

export async function createAccount(payload: { accountGuid: string; name: string; type: string }) {
    const { data } = await axios.post(`${getBaseUrl()}/v1/accounts/`, payload, { withCredentials: true });
    return data;
}

export async function freezeAccount(guid: string, freeze: boolean) {
    const { data } = await axios.patch(`${getBaseUrl()}/v1/accounts/${guid}/`, { freeze }, { withCredentials: true });
    return data;
}

export async function updateAccount(guid: string, payload: { name: string; type: string }) {
    const { data } = await axios.put(`${getBaseUrl()}/v1/accounts/${guid}/`, payload, { withCredentials: true });
    return data;
}

export async function deleteAccount(guid: string) {
    const { data } = await axios.delete(`${getBaseUrl()}/v1/accounts/${guid}/`, { withCredentials: true });
    return data;
}

export async function initiateTransaction(
    guid: string,
    payload: { receiverAccountGuid?: string; amount: string; transactionType: "TRANSFER" | "WITHDRAW" | "DEPOSIT"; messageId: string },
) {
    const { data } = await axios.post(`${getBaseUrl()}/v1/accounts/${guid}/transactions/`, payload, { withCredentials: true });
    return data;
}

export async function initiateExchange(guid: string, payload: { amount: string; currency: string; messageId: string }) {
    const { data } = await axios.post(`${getBaseUrl()}/v1/accounts/${guid}/exchanges/`, payload, { withCredentials: true });
    return data;
}
