import { useNavigate } from "react-router-dom";
import { logout } from "../services/authService";
import { Button } from "@/components/ui/button";
import { LogOut, LayoutDashboard } from "lucide-react";

type NavBarProps = {
    showLogout?: boolean;
};

export const NavBar = ({ showLogout = false }: NavBarProps) => {
    const navigate = useNavigate();

    const handleLogout = () => {
        logout();
    };

    return (
        <header className="sticky top-0 z-50 w-full border-b border-border bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
            <div className="mx-auto flex h-14 max-w-7xl items-center justify-between px-4 sm:px-6">
                <div className="flex items-center gap-4">
                    <p className="text-lg font-semibold tracking-tight">HABO Banking</p>
                    {showLogout && (
                        <Button variant="ghost" size="sm" onClick={() => navigate("/dashboard")}>
                            <LayoutDashboard className="h-4 w-4" />
                            Dashboard
                        </Button>
                    )}
                </div>
                {showLogout && (
                    <Button variant="ghost" size="sm" onClick={handleLogout}>
                        <LogOut className="h-4 w-4" />
                        Logout
                    </Button>
                )}
            </div>
        </header>
    );
};
