import "./styles/Navbar.css";

type NavBarProps = {
    showLogout?: boolean;
};

export const NavBar = ({ showLogout = false }: NavBarProps) => {
    return (
        <header className="navbar">
            <div className="navbar-inner">
                <p className="navbar-brand">HABO Banking</p>
                {showLogout && (
                    <button className="navbar-logout" type="button">
                        Logout
                    </button>
                )}
            </div>
        </header>
    );
};