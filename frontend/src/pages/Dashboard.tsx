import { useNavigate } from "react-router-dom";
import "./styles/Dashboard.css";
import { Transaction } from "../components/Transaction";

export default function Dashboard() {
    const navigate = useNavigate();

    const accounts = [
        { id: 1, name: "Konto", balance: "20.223.56" },
        { id: 2, name: "Opsparing", balance: "120.223.78" },
        { id: 3, name: "Pension", balance: "209.728.98" },
    ];

    const handleAccountClick = (id: number) => {
        navigate(`/accounts/${id}`);
    };

    return (
        <section className="dashboard">
            <h1 className="dashboard-title">Dashboard</h1>
            <div className="dashboard-table-wrap">
            <table className="dashboard-table">
                <thead>
                    <tr>
                        <th>Account Name</th>
                        <th>Balance</th>
                    </tr>
                </thead>
                <tbody>
                    {accounts.map((account) => (
                        <tr key={account.id}>
                            <td>
                                <button
                                    className="dashboard-account-link"
                                    type="button"
                                    onClick={() => handleAccountClick(account.id)}
                                >
                                    {account.name}
                                </button>
                            </td>
                            <td className="dashboard-balance">{account.balance}</td>
                        </tr>
                    ))}
                </tbody>
            </table>
            </div>
            <Transaction />
        </section>
    );
}