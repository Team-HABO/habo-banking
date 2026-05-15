import { NavBar } from "../components/Navbar";
import { Outlet, useLocation } from "react-router-dom";

const Layout = () => {
    const { pathname } = useLocation();
    const showLogout = pathname !== "/";

    return (
        <>
            <NavBar showLogout={showLogout} />
            <main className="mx-auto max-w-7xl px-4 py-6 sm:px-6">
                <Outlet />
            </main>
        </>
    );
};

export default Layout;
