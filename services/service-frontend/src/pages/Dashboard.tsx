import { useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Plus } from "lucide-react";

export default function Dashboard() {
    const navigate = useNavigate();

    const accounts = [
        { guid: "11111111-1111-1111-1111-111111111111", name: "Konto", balance: "20.223.56" },
        { guid: "22222222-2222-2222-2222-222222222222", name: "Opsparing", balance: "120.223.78" },
        { guid: "33333333-3333-3333-3333-333333333333", name: "Pension", balance: "209.728.98" },
    ];

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
                                    <tr key={account.guid} className="border-b border-border last:border-0">
                                        <td className="py-3">
                                            <button
                                                className="font-medium text-primary hover:underline cursor-pointer"
                                                type="button"
                                                onClick={() => handleAccountClick(account.guid)}
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
                </CardContent>
            </Card>
        </section>
    );
}
