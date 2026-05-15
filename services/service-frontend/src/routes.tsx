import LoginPage from "./pages/LoginPage";
import ErrorPage from "./pages/ErrorPage";
import Layout from "./pages/Layout";
import Dashboard from "./pages/Dashboard";
import SeeAccount from "./pages/SeeAccount";
import NewAccount from "./pages/NewAccount";
import TransactionPage from "./pages/TransactionPage";
import ExchangePage from "./pages/ExchangePage";
import { createBrowserRouter } from "react-router-dom";

const router = createBrowserRouter([
    {
        path: "/",
        element: <Layout />,
        errorElement: <ErrorPage />,
        children: [
            { path: "/", element: <LoginPage /> },
            { path: "/dashboard", element: <Dashboard /> },
            { path: "/accounts/new", element: <NewAccount /> },
            { path: "/accounts/:id", element: <SeeAccount /> },
            { path: "/accounts/:id/transaction", element: <TransactionPage /> },
            { path: "/accounts/:id/exchange", element: <ExchangePage /> },
        ],
    },
]);

export default router;
