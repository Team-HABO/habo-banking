import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { freezeAccount, updateAccount, deleteAccount } from "../services/accountService";
import "./styles/SeeAccount.css";

const ACCOUNT_TYPES = ["savings", "checking", "pension"];

// Mock accounts matching the Dashboard data until the view-service is integrated.
const MOCK_ACCOUNTS: Record<string, { guid: string; name: string; type: string; isFrozen: boolean }> = {
    "1": { guid: "11111111-1111-1111-1111-111111111111", name: "Konto", type: "checking", isFrozen: false },
    "2": { guid: "22222222-2222-2222-2222-222222222222", name: "Opsparing", type: "savings", isFrozen: false },
    "3": { guid: "33333333-3333-3333-3333-333333333333", name: "Pension", type: "pension", isFrozen: true },
};

export default function SeeAccount() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const mock = id ? MOCK_ACCOUNTS[id] : undefined;

    const [name, setName] = useState(mock?.name ?? "");
    const [type, setType] = useState(mock?.type ?? ACCOUNT_TYPES[0]);
    const [isFrozen, setIsFrozen] = useState(mock?.isFrozen ?? false);
    const [isEditing, setIsEditing] = useState(false);
    const [loading, setLoading] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);

    if (!mock) {
        return (
            <section className="see-account">
                <h1 className="see-account-title">Account not found</h1>
            </section>
        );
    }

    const handleError = (err: unknown) => {
        if (typeof err === "object" && err !== null && "response" in err && typeof (err as { response: { data: unknown } }).response?.data === "object") {
            const respData = (err as { response: { data: Record<string, unknown> } }).response.data;
            const message = typeof respData.error === "string" ? respData.error : JSON.stringify(respData.errors ?? respData);
            setError(message);
        } else {
            setError("Something went wrong. Please try again.");
        }
    };

    const handleFreeze = async () => {
        setError(null);
        setLoading("freeze");
        try {
            await freezeAccount(mock.guid, !isFrozen);
            setIsFrozen(!isFrozen);
        } catch (err) {
            handleError(err);
        } finally {
            setLoading(null);
        }
    };

    const handleUpdate = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        setLoading("update");
        try {
            await updateAccount(mock.guid, { name, type });
            setIsEditing(false);
        } catch (err) {
            handleError(err);
        } finally {
            setLoading(null);
        }
    };

    const handleDelete = async () => {
        if (!window.confirm("Are you sure you want to delete this account?")) return;
        setError(null);
        setLoading("delete");
        try {
            await deleteAccount(mock.guid);
            navigate("/dashboard");
        } catch (err) {
            handleError(err);
        } finally {
            setLoading(null);
        }
    };

    return (
        <section className="see-account">
            <h1 className="see-account-title">{name}</h1>

            <div className="see-account-card">
                <div className="see-account-info">
                    <span className="see-account-badge">{type}</span>
                    <span className={`see-account-badge ${isFrozen ? "see-account-badge--frozen" : "see-account-badge--active"}`}>
                        {isFrozen ? "Frozen" : "Active"}
                    </span>
                </div>

                {error && <p className="see-account-error">{error}</p>}

                {/* Freeze / Unfreeze */}
                <button className="see-account-action-button" type="button" disabled={loading !== null} onClick={handleFreeze}>
                    {loading === "freeze" ? "Processing…" : isFrozen ? "Unfreeze Account" : "Freeze Account"}
                </button>

                {/* Edit toggle */}
                {!isEditing ? (
                    <button className="see-account-action-button" type="button" disabled={loading !== null} onClick={() => setIsEditing(true)}>
                        Edit Account
                    </button>
                ) : (
                    <form className="see-account-edit-form" onSubmit={handleUpdate}>
                        <label className="see-account-label" htmlFor="edit-name">
                            Name
                        </label>
                        <input
                            id="edit-name"
                            className="see-account-input"
                            type="text"
                            required
                            maxLength={255}
                            value={name}
                            onChange={(e) => setName(e.target.value)}
                        />

                        <label className="see-account-label" htmlFor="edit-type">
                            Type
                        </label>
                        <select id="edit-type" className="see-account-select" value={type} onChange={(e) => setType(e.target.value)}>
                            {ACCOUNT_TYPES.map((t) => (
                                <option key={t} value={t}>
                                    {t.charAt(0).toUpperCase() + t.slice(1)}
                                </option>
                            ))}
                        </select>

                        <div className="see-account-edit-actions">
                            <button className="see-account-action-button" type="submit" disabled={loading !== null || !name.trim()}>
                                {loading === "update" ? "Saving…" : "Save Changes"}
                            </button>
                            <button
                                className="see-account-cancel-button"
                                type="button"
                                onClick={() => {
                                    setIsEditing(false);
                                    setName(mock.name);
                                    setType(mock.type);
                                }}
                            >
                                Cancel
                            </button>
                        </div>
                    </form>
                )}

                {/* Delete */}
                <button className="see-account-delete-button" type="button" disabled={loading !== null} onClick={handleDelete}>
                    {loading === "delete" ? "Deleting…" : "Delete Account"}
                </button>
            </div>
        </section>
    );
}
