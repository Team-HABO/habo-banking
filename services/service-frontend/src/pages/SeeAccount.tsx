import { useState } from "react";
import { useNavigate, useParams, Link } from "react-router-dom";
import { freezeAccount, updateAccount, deleteAccount } from "../services/accountService";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ArrowDown, ArrowUp, ArrowLeftRight, DollarSign } from "lucide-react";

const ACCOUNT_TYPES = ["savings", "checking", "pension"];

// Mock accounts matching the Dashboard data until the view-service is integrated.
const MOCK_ACCOUNTS = [
    { guid: "11111111-1111-1111-1111-111111111111", name: "Konto", type: "checking", isFrozen: false },
    { guid: "22222222-2222-2222-2222-222222222222", name: "Opsparing", type: "savings", isFrozen: false },
    { guid: "33333333-3333-3333-3333-333333333333", name: "Pension", type: "pension", isFrozen: true },
];

export default function SeeAccount() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const mock = MOCK_ACCOUNTS.find((a) => a.guid === id);

    const [name, setName] = useState(mock?.name ?? "");
    const [type, setType] = useState(mock?.type ?? ACCOUNT_TYPES[0]);
    const [isFrozen, setIsFrozen] = useState(mock?.isFrozen ?? false);
    const [isEditing, setIsEditing] = useState(false);
    const [loading, setLoading] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);

    if (!mock) {
        return (
            <section className="space-y-4">
                <h1 className="text-2xl font-semibold tracking-tight">Account not found</h1>
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
        <section className="mx-auto max-w-2xl space-y-6">
            <div className="flex items-center justify-between">
                <div className="space-y-1">
                    <h1 className="text-2xl font-semibold tracking-tight">{name}</h1>
                    <div className="flex gap-2">
                        <Badge variant="secondary">{type}</Badge>
                        <Badge variant={isFrozen ? "destructive" : "success"}>{isFrozen ? "Frozen" : "Active"}</Badge>
                    </div>
                </div>
            </div>

            {error && <p className="text-sm text-destructive">{error}</p>}

            {/* Transactions Section */}
            <Card>
                <CardHeader>
                    <CardTitle className="text-lg">Transactions</CardTitle>
                </CardHeader>
                <CardContent>
                    <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
                        <Link
                            className="flex flex-col items-center gap-2 rounded-lg border border-border p-4 text-sm font-medium transition-colors hover:bg-accent"
                            to={`/accounts/${id}/transaction?type=DEPOSIT`}
                        >
                            <ArrowDown className="h-5 w-5 text-success" />
                            Deposit
                        </Link>
                        <Link
                            className="flex flex-col items-center gap-2 rounded-lg border border-border p-4 text-sm font-medium transition-colors hover:bg-accent"
                            to={`/accounts/${id}/transaction?type=WITHDRAW`}
                        >
                            <ArrowUp className="h-5 w-5 text-destructive" />
                            Withdraw
                        </Link>
                        <Link
                            className="flex flex-col items-center gap-2 rounded-lg border border-border p-4 text-sm font-medium transition-colors hover:bg-accent"
                            to={`/accounts/${id}/transaction?type=TRANSFER`}
                        >
                            <ArrowLeftRight className="h-5 w-5 text-primary" />
                            Transfer
                        </Link>
                        <Link
                            className="flex flex-col items-center gap-2 rounded-lg border border-border p-4 text-sm font-medium transition-colors hover:bg-accent"
                            to={`/accounts/${id}/exchange`}
                        >
                            <DollarSign className="h-5 w-5 text-primary" />
                            Exchange
                        </Link>
                    </div>
                </CardContent>
            </Card>

            {/* Account Management Section */}
            <Card>
                <CardHeader>
                    <CardTitle className="text-lg">Manage Account</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                    <div className="flex flex-wrap gap-3">
                        <Button variant="outline" disabled={loading !== null} onClick={handleFreeze}>
                            {loading === "freeze" ? "Processing…" : isFrozen ? "Unfreeze Account" : "Freeze Account"}
                        </Button>

                        {!isEditing && (
                            <Button variant="secondary" disabled={loading !== null} onClick={() => setIsEditing(true)}>
                                Edit Account
                            </Button>
                        )}
                    </div>

                    {isEditing && (
                        <form className="space-y-4 rounded-lg border border-border p-4" onSubmit={handleUpdate}>
                            <div className="space-y-2">
                                <Label htmlFor="edit-name">Name</Label>
                                <Input id="edit-name" type="text" required maxLength={255} value={name} onChange={(e) => setName(e.target.value)} />
                            </div>

                            <div className="space-y-2">
                                <Label htmlFor="edit-type">Type</Label>
                                <Select id="edit-type" value={type} onChange={(e) => setType(e.target.value)}>
                                    {ACCOUNT_TYPES.map((t) => (
                                        <option key={t} value={t}>
                                            {t.charAt(0).toUpperCase() + t.slice(1)}
                                        </option>
                                    ))}
                                </Select>
                            </div>

                            <div className="flex gap-3">
                                <Button type="submit" disabled={loading !== null || !name.trim()}>
                                    {loading === "update" ? "Saving…" : "Save Changes"}
                                </Button>
                                <Button
                                    type="button"
                                    variant="ghost"
                                    onClick={() => {
                                        setIsEditing(false);
                                        setName(mock.name);
                                        setType(mock.type);
                                    }}
                                >
                                    Cancel
                                </Button>
                            </div>
                        </form>
                    )}

                    <div className="border-t border-border pt-4">
                        <Button variant="destructive" disabled={loading !== null} onClick={handleDelete}>
                            {loading === "delete" ? "Deleting…" : "Delete Account"}
                        </Button>
                    </div>
                </CardContent>
            </Card>
        </section>
    );
}
