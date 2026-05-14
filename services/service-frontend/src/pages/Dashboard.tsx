import { useNavigate } from "react-router-dom";
import { useQuery } from "@apollo/client/react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Plus } from "lucide-react";
import { GET_USER_ACCOUNTS } from "@/services/viewService";
import type { Account } from "@/types/graphql";

export default function Dashboard() {
    const navigate = useNavigate();
    const { data, loading, error } = useQuery<{ getUserAccounts: Account[] }>(GET_USER_ACCOUNTS);

    const accounts = data?.getUserAccounts ?? [];

    const handleAccountClick = (guid: string) => {
        navigate(`/accounts/${guid}`);
    };

    return (
        <section className="space-y-6">
            <div className="flex items-center justify-between">
                <h1 className="text-2xl font-semibold tracking-tight">Dashboard</h1>
                <Button onClick={() => navigate("/accounts/new")}>
                    <Plus className="h-4 w-4" />
                    Create Account
                </Button>
            </div>

            <Card>
                <CardHeader>
                    <CardTitle className="text-lg">Your Accounts</CardTitle>
                </CardHeader>
                <CardContent>
                    {loading && <p className="text-sm text-muted-foreground">Loading accounts…</p>}
                    {error && <p className="text-sm text-destructive">Failed to load accounts: {error.message}</p>}
                    {!loading && !error && accounts.length === 0 && (
                        <p className="text-sm text-muted-foreground">No accounts yet. Create one to get started.</p>
                    )}
                    {accounts.length > 0 && (
                        <div className="overflow-x-auto">
                            <table className="w-full text-sm">
                                <thead>
                                    <tr className="border-b border-border">
                                        <th className="pb-3 text-left font-medium text-muted-foreground">Account Name</th>
                                        <th className="pb-3 text-right font-medium text-muted-foreground">Balance</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {accounts.map((account) => (
                                        <tr key={account.accountGuid} className="border-b border-border last:border-0">
                                            <td className="py-3">
                                                <button
                                                    className="font-medium text-primary hover:underline cursor-pointer"
                                                    type="button"
                                                    onClick={() => handleAccountClick(account.accountGuid)}
                                                >
                                                    {account.name}
                                                </button>
                                            </td>
                                            <td className="py-3 text-right tabular-nums">{account.balance}</td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    )}
                </CardContent>
            </Card>
        </section>
    );
}
