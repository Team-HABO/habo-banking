import { useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { initiateExchange } from "../services/accountService";
import client from "@/services/apolloClient";
import { GET_USER_ACCOUNTS, GET_ACCOUNT_AUDITS } from "@/services/viewService";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

const CURRENCIES = ["USD", "EUR", "GBP", "NOK", "SEK", "CHF", "JPY", "CAD", "AUD"];

export default function ExchangePage() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();

    const [messageId, setMessageId] = useState(() => crypto.randomUUID());
    const [amount, setAmount] = useState("");
    const [currency, setCurrency] = useState(CURRENCIES[0]);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState(false);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        setSuccess(false);
        setIsLoading(true);

        try {
            await initiateExchange(id!, { amount, currency, messageId });
            await client.refetchQueries({ include: [GET_USER_ACCOUNTS, GET_ACCOUNT_AUDITS] });
            setSuccess(true);
            setMessageId(crypto.randomUUID());
            setAmount("");
        } catch (err) {
            if (typeof err === "object" && err !== null && "response" in err && typeof (err as { response: { data: unknown } }).response?.data === "object") {
                const respData = (err as { response: { data: Record<string, unknown> } }).response.data;
                const message = typeof respData.error === "string" ? respData.error : JSON.stringify(respData.errors ?? respData);
                setError(message);
            } else {
                setError("Failed to initiate exchange. Please try again.");
            }
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <section className="mx-auto max-w-lg space-y-6">
            <h1 className="text-2xl font-semibold tracking-tight">Currency Exchange</h1>

            <Card>
                <CardHeader>
                    <CardTitle className="text-lg">Exchange Details</CardTitle>
                </CardHeader>
                <CardContent>
                    <form className="space-y-4" onSubmit={handleSubmit}>
                        <div className="space-y-2">
                            <Label htmlFor="exchange-amount">Amount (DKK)</Label>
                            <Input
                                id="exchange-amount"
                                type="text"
                                required
                                maxLength={50}
                                value={amount}
                                onChange={(e) => setAmount(e.target.value)}
                                placeholder="e.g. 500.00"
                            />
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="exchange-currency">Exchange To</Label>
                            <Select id="exchange-currency" value={currency} onChange={(e) => setCurrency(e.target.value)}>
                                {CURRENCIES.map((c) => (
                                    <option key={c} value={c}>
                                        {c}
                                    </option>
                                ))}
                            </Select>
                        </div>

                        {error && <p className="text-sm text-destructive">{error}</p>}
                        {success && <p className="text-sm text-success">Exchange initiated successfully.</p>}

                        <div className="flex gap-3 pt-2">
                            <Button type="submit" disabled={isLoading || !amount.trim()}>
                                {isLoading ? "Processing…" : "Exchange Currency"}
                            </Button>
                            <Button type="button" variant="outline" onClick={() => navigate(`/accounts/${id}`)}>
                                Back
                            </Button>
                        </div>
                    </form>
                </CardContent>
            </Card>
        </section>
    );
}
