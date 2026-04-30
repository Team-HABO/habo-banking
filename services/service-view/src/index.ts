import 'dotenv/config';
import { ApolloServer } from '@apollo/server';
import { startStandaloneServer } from '@apollo/server/standalone';
import mongoose from 'mongoose';
import { typeDefs } from './graphql/typeDefs';
import { resolvers } from './graphql/resolvers';    


async function startServer() {
  const mongoConnectionString = process.env.MONGODB_CONNECTION_STRING;

  if (!mongoConnectionString) {
    throw new Error('MONGODB_CONNECTION_STRING is not set');
  }

  await mongoose.connect(mongoConnectionString);
  console.log("Connected to MongoDB");

  const server = new ApolloServer({ typeDefs, resolvers });
  const { url } = await startStandaloneServer(server, { listen: { port: 4000 } });

  console.log(`🚀 Server ready at ${url}`);
}

startServer();