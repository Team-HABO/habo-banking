import axios from "axios";

export interface Response<T> {
  statusCode: string;
  message: string;
  data: T;
}
export interface Transaction {
    receiverAccountGuid: string | null;
    senderAccountGuid: string;
    transactionType: "Withdrawal" | "Deposit" | "Transfer";
    amount: number;
    messageId: string; 
}
class AccountService {
//   private readonly baseUrl = import.meta.env["VITE_ACCOUNT_API_URL"];
  private readonly baseUrl = "http://localhost:5288/api";

  async postTransaction(amount: number, type: "Withdrawal" | "Deposit" | "Transfer"): Promise<any> {
    const senderId = "1"; // Replace with actual sender account GUID
    // const senderId = crypto.randomUUID();
    
    const payload: Transaction = {
      senderAccountGuid: senderId,
      receiverAccountGuid: (type === "Transfer") ? crypto.randomUUID() : null,
      transactionType: type,
      amount: amount,
      messageId: crypto.randomUUID(),
    };

    const response = await axios.post(`${this.baseUrl}/accounts/${senderId}`, payload, {
      withCredentials: true,
    });

    return response.data;
  }
}

export const accountService = new AccountService();