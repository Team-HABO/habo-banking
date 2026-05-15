import { useState } from "react";
import { useParams, useSearchParams, useNavigate } from "react-router-dom";
import { initiateTransaction } from "../services/accountService";
import client from "@/services/apolloClient";
import { GET_USER_ACCOUNTS, GET_ACCOUNT_AUDITS } from "@/services/viewService";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

const TRANSACTION_TYPES = ["DEPOSIT", "WITHDRAW", "TRANSFER"] as const;
type TransactionType = (typeof TRANSACTION_TYPES)[number];

export default function TransactionPage() {
    const { id } = useParams<{ id: string }>();
    const [searchParams] = useSearchParams();
    const navigate = useNavigate();

    const initialType = TRANSACTION_TYPES.includes(searchParams.get("type") as TransactionType) ? (searchParams.get("type") as TransactionType) : "DEPOSIT";

    const [messageId, setMessageId] = useState(() => crypto.randomUUID());
    const [transactionType, setTransactionType] = useState<TransactionType>(initialType);
    const [amount, setAmount] = useState("");
    const [receiverAccountGuid, setReceiverAccountGuid] = useState("");
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState(false);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        setSuccess(false);
        setIsLoading(true);

        try {
            await initiateTransaction(id!, {
                amount,
                transactionType,
                messageId,
                ...(transactionType === "TRANSFER" ? { receiverAccountGuid } : {}),
            });
            await client.refetchQueries({ include: [GET_USER_ACCOUNTS, GET_ACCOUNT_AUDITS] });
            setSuccess(true);
            setMessageId(crypto.randomUUID());
            setAmount("");
            setReceiverAccountGuid("");
        } catch (err) {
            if (typeof err === "object" && err !== null && "response" in err && typeof (err as { response: { data: unknown } }).response?.data === "object") {
                const respData = (err as { response: { data: Record<string, unknown> } }).response.data;
                const message = typeof respData.error === "string" ? respData.error : JSON.stringify(respData.errors ?? respData);
                setError(message);
            } else {
                setError("Failed to initiate transaction. Please try again.");
            }
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <section className="mx-auto max-w-lg space-y-6">
            <h1 className="text-2xl font-semibold tracking-tight">New Transaction</h1>

            <Card>
                <CardHeader>
                    <CardTitle className="text-lg">Transaction Details</CardTitle>
                </CardHeader>
                <CardContent>
                    <form className="space-y-4" onSubmit={handleSubmit}>
                        <div className="space-y-2">
                            <Label htmlFor="transaction-type">Transaction Type</Label>
                            <Select id="transaction-type" value={transactionType} onChange={(e) => setTransactionType(e.target.value as TransactionType)}>
                                {TRANSACTION_TYPES.map((t) => (
                                    <option key={t} value={t}>
                                        {t.charAt(0) + t.slice(1).toLowerCase()}
                                    </option>
                                ))}
                            </Select>
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="transaction-amount">Amount</Label>
                            <Input
                                id="transaction-amount"
                                type="text"
                                required
                                maxLength={50}
                                value={amount}
                                onChange={(e) => setAmount(e.target.value)}
                                placeholder="e.g. 100.00"
                            />
                        </div>

                        {transactionType === "TRANSFER" && (
                            <div className="space-y-2">
                                <Label htmlFor="receiver-guid">Receiver Account GUID</Label>
                                <Input
                                    id="receiver-guid"
                                    type="text"
                                    required
                                    value={receiverAccountGuid}
                                    onChange={(e) => setReceiverAccountGuid(e.target.value)}
                                    placeholder="e.g. 12345678-1234-1234-1234-123456789abc"
                                />
                            </div>
                        )}

                        {error && <p className="text-sm text-destructive">{error}</p>}
                        {success && <p className="text-sm text-success">Transaction initiated successfully.</p>}

                        <div className="flex gap-3 pt-2">
                            <Button type="submit" disabled={isLoading || !amount.trim()}>
                                {isLoading ? "Processing…" : "Submit Transaction"}
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
