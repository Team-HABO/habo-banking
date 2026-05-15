import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { createAccount } from "../services/accountService";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

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
        <section className="mx-auto max-w-lg space-y-6">
            <h1 className="text-2xl font-semibold tracking-tight">Create New Account</h1>

            <Card>
                <CardHeader>
                    <CardTitle className="text-lg">Account Details</CardTitle>
                </CardHeader>
                <CardContent>
                    <form className="space-y-4" onSubmit={handleSubmit}>
                        <div className="space-y-2">
                            <Label htmlFor="account-name">Account Name</Label>
                            <Input
                                id="account-name"
                                type="text"
                                required
                                maxLength={255}
                                value={name}
                                onChange={(e) => setName(e.target.value)}
                                placeholder="e.g. My Savings"
                            />
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="account-type">Account Type</Label>
                            <Select id="account-type" value={type} onChange={(e) => setType(e.target.value)}>
                                {ACCOUNT_TYPES.map((t) => (
                                    <option key={t} value={t}>
                                        {t.charAt(0).toUpperCase() + t.slice(1)}
                                    </option>
                                ))}
                            </Select>
                        </div>

                        {error && <p className="text-sm text-destructive">{error}</p>}

                        <div className="flex gap-3 pt-2">
                            <Button type="submit" disabled={isLoading || !name.trim()}>
                                {isLoading ? "Creating…" : "Create Account"}
                            </Button>
                            <Button type="button" variant="outline" onClick={() => navigate("/dashboard")}>
                                Cancel
                            </Button>
                        </div>
                    </form>
                </CardContent>
            </Card>
        </section>
    );
}
