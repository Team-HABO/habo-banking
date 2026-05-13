import 'dotenv/config';
import { ApolloServer } from '@apollo/server';
import { startStandaloneServer } from '@apollo/server/standalone';
import mongoose from 'mongoose';
import { typeDefs } from './graphql/typeDefs.js';
import { resolvers } from './graphql/resolvers.js';    
import jwt from 'jsonwebtoken';
import cookie from 'cookie';
import type { Context } from './types/context.js';

interface JwtPayload {
  nameid: string; 
  email: string;
  aud: string[];
  iss: string;
}

async function startServer() {
  const mongoConnectionString = process.env.MONGODB_CONNECTION_STRING;
  const jwtSecret = process.env.JWT_SECRET;

  const serverHost = process.env.SERVER_HOST ?? '0.0.0.0';
  const serverPort = Number(process.env.SERVER_PORT ?? '4000');

  if (!mongoConnectionString) {
    throw new Error('MONGODB_CONNECTION_STRING is not set');
  }

  if (Number.isNaN(serverPort) || serverPort <= 0) {
    throw new Error('SERVER_PORT must be a positive number');
  }

    if (!jwtSecret) {
    throw new Error('JWT_SECRET is not set');
  }

  await mongoose.connect(mongoConnectionString);
  console.log("Connected to MongoDB");

  const server = new ApolloServer<Context>({ typeDefs, resolvers });
  const { url } = await startStandaloneServer(server, {
    listen: { host: serverHost, port: serverPort },
    context: async ({ req }) => {
      try {
        const cookies = cookie.parse(req.headers.cookie || '');
        const token = cookies.token;
        if (!token) {
          return { userId: null };
        }

        const decoded = jwt.verify(token, jwtSecret) as JwtPayload;

        return {
          userId: decoded.nameid,
        };
      } catch (error: any) {
        console.error('JWT Verification Failed:', error.message);
        return {
          userId: null,
        };
      }
    },
  });

  console.log(`🚀 Server ready at ${url}`);
}

 startServer().catch((error) => {
   console.error('Failed to start server', error);
   process.exit(1);
 });
