import { NavBar } from "../components/Navbar";
import { Outlet, useLocation } from "react-router-dom";



const Layout = () => {
  const { pathname } = useLocation();
  const showLogout = pathname !== "/";

  return (
    <>
      <NavBar showLogout={showLogout} />
      <main className="page-content">
        <Outlet />
      </main>
    </>
  );
};

export default Layout;