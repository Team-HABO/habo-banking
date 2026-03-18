import { isRouteErrorResponse, useRouteError } from "react-router-dom";
import { NavBar } from "../components/Navbar";


export default function ErrorPage(){
  const error = useRouteError();
  return (
    <>
    <NavBar />
      <h1>Ooops...</h1>
      <p>{isRouteErrorResponse(error)
          ? "Page not found"
          : "An unexpected error has occurred."}</p>
    </>
  );
};

