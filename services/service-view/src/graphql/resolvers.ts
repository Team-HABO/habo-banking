import { User } from '../mongoose/schema.js';
import type { Context } from '../types/context.js';
import { GraphQLError } from 'graphql';

export const resolvers = {
  Query: {
    getUserAccounts: async (
      _: any,
      __: any,
      context: Context
    ) => {
      if (!context.userId) {
        throw new GraphQLError('Unauthorized', {
          extensions: {
            code: 'UNAUTHENTICATED',
          },
        });
      }

      const user = await User.findById(context.userId);

      return user?.accounts || [];
    },

    getAccountAudits: async (
      _: any,
      { accountGuid }: { accountGuid: string },
      context: Context
    ) => {
      if (!context.userId) {
        throw new GraphQLError('Unauthorized', {
          extensions: {
            code: 'UNAUTHENTICATED',
          },
        });
      }

      const user = await User.findOne(
        {
          _id: context.userId,
          'accounts.accountGuid': accountGuid,
        },
        {
          'accounts.$': 1,
        }
      );

      return user?.accounts?.[0]?.audits || [];
    },
  },
  Account: {
    // This converts the MongoDB Decimal128 to a readable string
    balance: (parent: any) => parent.balance?.amount?.toString() || "0"
  },
  Audit: {
    // Serialize Date values to a stable ISO-8601 string format.
    timestamp: (parent: any) => {
      const dateTime = parent?.timestamp;
      if (!dateTime) return null;

      if (dateTime instanceof Date) return dateTime.toISOString();
      if (typeof dateTime === 'number') return new Date(dateTime).toISOString();
      if (typeof dateTime === 'string' && /^\d+$/.test(dateTime)) return new Date(Number(dateTime)).toISOString();
      
      const d = new Date(dateTime);
      return Number.isNaN(d.getTime()) ? null : d.toISOString();
    }
  }
};
