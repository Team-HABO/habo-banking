import 'dotenv/config';
import { ApolloServer } from '@apollo/server';
import { startStandaloneServer } from '@apollo/server/standalone';
import mongoose from 'mongoose';
import { typeDefs } from './graphql/typeDefs.js';
import { resolvers } from './graphql/resolvers.js';    


async function startServer() {
  const mongoConnectionString = process.env.MONGODB_CONNECTION_STRING;
  const serverHost = process.env.SERVER_HOST ?? '0.0.0.0';
  const serverPort = Number(process.env.SERVER_PORT ?? '4000');

  if (!mongoConnectionString) {
    throw new Error('MONGODB_CONNECTION_STRING is not set');
  }

  if (Number.isNaN(serverPort) || serverPort <= 0) {
    throw new Error('SERVER_PORT must be a positive number');
  }

  await mongoose.connect(mongoConnectionString);
  console.log("Connected to MongoDB");

  const server = new ApolloServer({ typeDefs, resolvers });
  const { url } = await startStandaloneServer(server, {
    listen: { host: serverHost, port: serverPort },
  });

  console.log(`🚀 Server ready at ${url}`);
}

 startServer().catch((error) => {
   console.error('Failed to start server', error);
   process.exit(1);
 });
