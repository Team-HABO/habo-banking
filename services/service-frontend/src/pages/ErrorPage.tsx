import { isRouteErrorResponse, useRouteError } from "react-router-dom";
import { NavBar } from "../components/Navbar";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";

export default function ErrorPage() {
    const error = useRouteError();
    return (
        <>
            <NavBar />
            <main className="mx-auto flex min-h-[60vh] max-w-7xl items-center justify-center px-4 py-6">
                <Card className="w-full max-w-md text-center">
                    <CardContent className="space-y-4 pt-6">
                        <h1 className="text-3xl font-semibold">Oops...</h1>
                        <p className="text-muted-foreground">{isRouteErrorResponse(error) ? "Page not found" : "An unexpected error has occurred."}</p>
                        <Button variant="outline" onClick={() => (globalThis.location.href = "/")}>
                            Go Home
                        </Button>
                    </CardContent>
                </Card>
            </main>
        </>
    );
}
