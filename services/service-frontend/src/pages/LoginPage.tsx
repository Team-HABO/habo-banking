import { useState } from "react";
import { login } from "../services/authService";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Shield, Zap, Lock } from "lucide-react";

export default function LoginPage() {
    const [isLoading, setIsLoading] = useState(false);
    const [errorMessage, setErrorMessage] = useState<string | null>(null);

    const handleLogin = () => {
        setIsLoading(true);
        setErrorMessage(null);

        try {
            login();
            setErrorMessage(null);
        } catch (error) {
            const message = error instanceof Error ? error.message : "Login request failed.";
            setIsLoading(false);
            setErrorMessage(message);
        }
    };

    return (
        <section className="flex min-h-[80vh] items-center justify-center">
            <Card className="w-full max-w-md">
                <CardHeader className="text-center">
                    <p className="text-xs font-medium uppercase tracking-widest text-muted-foreground">Trusted Digital Banking</p>
                    <CardTitle className="text-3xl">Welcome to HABO</CardTitle>
                    <CardDescription>Secure, simple banking for everyday life. Sign in with Google to access your dashboard.</CardDescription>
                </CardHeader>
                <CardContent className="space-y-6">
                    <div className="flex flex-wrap justify-center gap-2" aria-label="Security features">
                        <span className="inline-flex items-center gap-1 rounded-full bg-secondary px-3 py-1 text-xs font-medium text-secondary-foreground">
                            <Shield className="h-3 w-3" /> Bank-grade security
                        </span>
                        <span className="inline-flex items-center gap-1 rounded-full bg-secondary px-3 py-1 text-xs font-medium text-secondary-foreground">
                            <Zap className="h-3 w-3" /> Fast account overview
                        </span>
                        <span className="inline-flex items-center gap-1 rounded-full bg-secondary px-3 py-1 text-xs font-medium text-secondary-foreground">
                            <Lock className="h-3 w-3" /> Protected sign-in
                        </span>
                    </div>

                    <Button className="w-full" size="lg" onClick={handleLogin} disabled={isLoading}>
                        <span className="flex h-5 w-5 items-center justify-center rounded-sm bg-white text-sm font-bold text-foreground" aria-hidden="true">
                            G
                        </span>
                        {isLoading ? "Redirecting..." : "Login with Google"}
                    </Button>

                    {errorMessage && <p className="text-center text-sm text-destructive">{errorMessage}</p>}
                </CardContent>
            </Card>
        </section>
    );
}
