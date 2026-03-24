import { useState } from "react";
import { accountService } from "../services/accountService";
import "./styles/Transaction.css";


export const Transaction = () => {
    const [amount, setAmount] = useState<number>(0);
    const [type, setType] = useState<"Withdrawal" | "Deposit">("Deposit");
    const senderId = "1"; // Replace with actual sender account GUID
    // const senderId = crypto.randomUUID();
    const handleSubmit = async () => {
        accountService.postTransaction(amount, type, senderId).then((response) => {
            console.log(response);
        });
    };

    return (
        <div className="transaction-container">
            <h1>Transaction</h1>
                <select className="transaction-select" value={type} onChange={(e) => setType(e.target.value as "Withdrawal" | "Deposit")}>
                    <option value="Withdrawal">Withdrawal</option>
                    <option value="Deposit">Deposit</option>
                    <option value="Transfer">Transfer</option>
                </select>
        <input 
            className="transaction-input"
            type="text" 
            value={amount} 
            onChange={(e) => setAmount(Number(e.target.value))} 
            placeholder="Amount"/>

        <button className="transaction-button" onClick={handleSubmit}>Submit</button>
        </div>
    );
}