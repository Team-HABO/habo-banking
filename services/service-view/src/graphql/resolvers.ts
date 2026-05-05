import { User } from '../mongoose/schema.js';

export const resolvers = {
  Query: {
    getUserAccounts: async (_: any, { userId }: { userId: string }) => {
      const user = await User.findById(userId);
      return user?.accounts || [];
    },
    getAccountAudits: async (_: any, { userId, accountGuid }: any) => {
      const user = await User.findOne(
        { _id: userId, "accounts.accountGuid": accountGuid },
        { "accounts.$": 1 }
      );
      return user?.accounts?.[0]?.audits || [];
    }
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
