import { User } from '../mongoose/schema';

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
      return user?.accounts[0]?.audits || [];
    }
  },
  Account: {
    // This converts the MongoDB Decimal128 to a readable string
    balance: (parent: any) => parent.balance?.amount?.toString() || "0"
  }
};