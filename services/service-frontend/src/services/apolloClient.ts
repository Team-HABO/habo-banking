import { ApolloClient, InMemoryCache, HttpLink } from "@apollo/client/core";

const viewApiUrl = import.meta.env.VITE_VIEW_API_URL;

if (!viewApiUrl) {
    throw new Error("Missing VITE_VIEW_API_URL in environment variables.");
}

const httpLink = new HttpLink({
    uri: viewApiUrl,
    credentials: "include",
});

const client = new ApolloClient({
    link: httpLink,
    cache: new InMemoryCache(),
});

export default client;
