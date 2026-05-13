import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { createAccount } from "../services/accountService";
import "./styles/NewAccount.css";

const ACCOUNT_TYPES = ["savings", "checking", "pension"];

export default function NewAccount() {
    const navigate = useNavigate();
    const [accountGuid] = useState(() => crypto.randomUUID());
    const [name, setName] = useState("");
    const [type, setType] = useState(ACCOUNT_TYPES[0]);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        setIsLoading(true);

        try {
            await createAccount({ accountGuid, name, type });
            navigate("/dashboard");
        } catch (err) {
            if (typeof err === "object" && err !== null && "response" in err && typeof (err as { response: { data: unknown } }).response?.data === "object") {
                const respData = (err as { response: { data: Record<string, unknown> } }).response.data;
                const message = typeof respData.error === "string" ? respData.error : JSON.stringify(respData.errors ?? respData);
                setError(message);
            } else {
                setError("Failed to create account. Please try again.");
            }
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <section className="new-account">
            <h1 className="new-account-title">Create New Account</h1>

            <form className="new-account-form" onSubmit={handleSubmit}>
                <label className="new-account-label" htmlFor="account-name">
                    Account Name
                </label>
                <input
                    id="account-name"
                    className="new-account-input"
                    type="text"
                    required
                    maxLength={255}
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    placeholder="e.g. My Savings"
                />

                <label className="new-account-label" htmlFor="account-type">
                    Account Type
                </label>
                <select id="account-type" className="new-account-select" value={type} onChange={(e) => setType(e.target.value)}>
                    {ACCOUNT_TYPES.map((t) => (
                        <option key={t} value={t}>
                            {t.charAt(0).toUpperCase() + t.slice(1)}
                        </option>
                    ))}
                </select>

                {error && <p className="new-account-error">{error}</p>}

                <button className="new-account-submit" type="submit" disabled={isLoading || !name.trim()}>
                    {isLoading ? "Creating…" : "Create Account"}
                </button>
            </form>
        </section>
    );
}
