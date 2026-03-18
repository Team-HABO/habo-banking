import { logout } from "../services/authService";
import "./styles/Navbar.css";

type NavBarProps = {
    showLogout?: boolean;
};

export const NavBar = ({ showLogout = false }: NavBarProps) => {

      const handleLogout = () => {
            logout();
      };

    return (
        <header className="navbar">
            <div className="navbar-inner">
                <p className="navbar-brand">HABO Banking</p>
                {showLogout && (
                    <button className="navbar-logout" type="button" onClick={handleLogout}>
                        Logout
                    </button>
                )}
            </div>
        </header>
    );
};