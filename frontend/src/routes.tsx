
import LoginPage from "./pages/LoginPage";
import ErrorPage from "./pages/ErrorPage";
import Layout from "./pages/Layout";
import Dashboard from "./pages/Dashboard";
import SeeAccount from "./pages/SeeAccount";
import NewAccount from "./pages/NewAccount";
import { createBrowserRouter } from "react-router-dom";

const router = createBrowserRouter([
  {
    path: "/",
    element: <Layout />,
    errorElement: <ErrorPage />,
    children: [
      { path: "/", element: <LoginPage /> },
      { path: "/dashboard", element: <Dashboard /> },
      { path: "/accounts/:id", element: <SeeAccount /> },
      { path: "/accounts/new", element: <NewAccount /> },

    ],
  },
]);

export default router;