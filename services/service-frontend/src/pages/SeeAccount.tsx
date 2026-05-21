import { useState, useEffect } from "react";
import { useNavigate, useParams, Link } from "react-router-dom";
import { useQuery } from "@apollo/client/react";
import { freezeAccount, updateAccount, deleteAccount } from "../services/accountService";
import { GET_USER_ACCOUNTS, GET_ACCOUNT_AUDITS } from "@/services/viewService";
import type { Account, Audit } from "@/types/graphql";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ArrowDown, ArrowUp, ArrowLeftRight, DollarSign } from "lucide-react";

const ACCOUNT_TYPES = ["savings", "checking", "pension"];

export default function SeeAccount() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();

    const {
        data: accountsData,
        loading: accountsLoading,
        error: accountsError,
        refetch: refetchAccounts,
    } = useQuery<{ getUserAccounts: Account[] }>(GET_USER_ACCOUNTS);
    const { data: auditsData, loading: auditsLoading } = useQuery<{ getAccountAudits: Audit[] }>(GET_ACCOUNT_AUDITS, {
        variables: { accountGuid: id },
        skip: !id,
    });

    const account = accountsData?.getUserAccounts?.find((a: Account) => a.accountGuid === id);
    const audits = [...(auditsData?.getAccountAudits ?? [])].sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime());

    const [name, setName] = useState("");
    const [type, setType] = useState(ACCOUNT_TYPES[0]);
    const [isFrozen, setIsFrozen] = useState(false);
    const [isEditing, setIsEditing] = useState(false);
    const [loading, setLoading] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        if (account) {
            setName(account.name);
            setType(account.type);
            setIsFrozen(account.isFrozen);
        }
    }, [account]);

    if (accountsLoading) {
        return (
            <section className="space-y-4">
                <p className="text-sm text-muted-foreground">Loading account…</p>
            </section>
        );
    }

    if (accountsError) {
        return (
            <section className="space-y-4">
                <p className="text-sm text-destructive">Failed to load account: {accountsError.message}</p>
            </section>
        );
    }

    if (!account) {
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
            await freezeAccount(account.accountGuid, !isFrozen);
            setIsFrozen(!isFrozen);
            await refetchAccounts();
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
            await updateAccount(account.accountGuid, { name, type });
            setIsEditing(false);
            await refetchAccounts();
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
            await deleteAccount(account.accountGuid);
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
                    <p className="text-xs text-muted-foreground">{account.accountGuid}</p>
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

            {/* Audit History Section */}
            <Card>
                <CardHeader>
                    <CardTitle className="text-lg">Transaction History</CardTitle>
                </CardHeader>
                <CardContent>
                    {auditsLoading && <p className="text-sm text-muted-foreground">Loading history…</p>}
                    {!auditsLoading && audits.length === 0 && <p className="text-sm text-muted-foreground">No transactions yet.</p>}
                    {audits.length > 0 && (
                        <div className="overflow-x-auto">
                            <table className="w-full text-sm">
                                <thead>
                                    <tr className="border-b border-border">
                                        <th className="px-3 pb-3 text-left font-medium text-muted-foreground">Type</th>
                                        <th className="px-3 pb-3 text-right font-medium text-muted-foreground">Amount</th>
                                        <th className="px-3 pb-3 text-left font-medium text-muted-foreground">From</th>
                                        <th className="px-3 pb-3 text-left font-medium text-muted-foreground">To</th>
                                        <th className="px-3 pb-3 text-left font-medium text-muted-foreground">Date</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {audits.map((audit) => (
                                        <tr key={audit.auditId} className="border-b border-border last:border-0">
                                            <td className="px-3 py-3 capitalize">{audit.type?.toLowerCase()}</td>
                                            <td className="px-3 py-3 text-right tabular-nums">{audit.amount} DKK</td>
                                            <td className="px-3 py-3">{audit.sender ?? "—"}</td>
                                            <td className="px-3 py-3">{audit.receiver ?? "—"}</td>
                                            <td className="px-3 py-3 text-muted-foreground">
                                                {audit.timestamp ? new Date(audit.timestamp).toLocaleDateString() : "—"}
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    )}
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
                                        setName(account.name);
                                        setType(account.type);
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
