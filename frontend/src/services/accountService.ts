import axios from "axios";

export interface Response<T> {
  statusCode: string;
  message: string;
  data: T;
}
export interface Transaction {
    receiverAccountGuid: string | null;
    transactionType: "Withdrawal" | "Deposit" | "Transfer";
    amount: number;
    messageId: string; 
}
class AccountService {
//   private readonly baseUrl = import.meta.env["VITE_ACCOUNT_API_URL"];
  private readonly baseUrl = "http://localhost:5288/api";
  async postTransaction(transaction: Transaction, senderId: string): Promise<any> {

    const response = await axios.post(`${this.baseUrl}/accounts/${senderId}`, transaction, {
      withCredentials: true,
    });

    return response.data;
  }
}

export const accountService = new AccountService();