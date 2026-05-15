import "dotenv/config";
import { ApolloServer } from "@apollo/server";
import { expressMiddleware } from "@as-integrations/express5";
import express from "express";
import cors from "cors";
import http from "http";
import mongoose from "mongoose";
import { typeDefs } from "./graphql/typeDefs.js";
import { resolvers } from "./graphql/resolvers.js";
import jwt from "jsonwebtoken";
import cookie from "cookie";
import type { Context } from "./types/context.js";

interface JwtPayload {
    nameid: string;
    email: string;
    aud: string[];
    iss: string;
}

async function startServer() {
    const mongoConnectionString = process.env.MONGODB_CONNECTION_STRING;
    const jwtSecret = process.env.JWT_SECRET;

    const serverHost = process.env.SERVER_HOST ?? "0.0.0.0";
    const serverPort = Number(process.env.SERVER_PORT ?? "4000");

    if (!mongoConnectionString) {
        throw new Error("MONGODB_CONNECTION_STRING is not set");
    }

    if (Number.isNaN(serverPort) || serverPort <= 0) {
        throw new Error("SERVER_PORT must be a positive number");
    }

    if (!jwtSecret) {
        throw new Error("JWT_SECRET is not set");
    }

    await mongoose.connect(mongoConnectionString);
    console.log("Connected to MongoDB");

    const corsOrigin = process.env.CORS_ORIGIN ? process.env.CORS_ORIGIN.split(",") : ["http://localhost:3000", "http://localhost:80"];

    const app = express();
    const httpServer = http.createServer(app);

    const server = new ApolloServer<Context>({ typeDefs, resolvers });
    await server.start();

    app.use(
        "/",
        cors<cors.CorsRequest>({
            origin: corsOrigin,
            credentials: true,
        }),
        express.json(),
        expressMiddleware(server, {
            context: async ({ req }) => {
                try {
                    const cookies = cookie.parse(req.headers.cookie || "");
                    const token = cookies.auth_token;
                    if (!token) {
                        return { userId: null };
                    }

                    const decoded = jwt.verify(token, jwtSecret) as JwtPayload;

                    return {
                        userId: decoded.nameid,
                    };
                } catch (error: any) {
                    console.error("JWT Verification Failed:", error.message);
                    return {
                        userId: null,
                    };
                }
            },
        }),
    );

    await new Promise<void>((resolve) => httpServer.listen({ host: serverHost, port: serverPort }, resolve));
    console.log(`🚀 Server ready at http://${serverHost}:${serverPort}/`);
}

startServer().catch((error) => {
    console.error("Failed to start server", error);
    process.exit(1);
});
