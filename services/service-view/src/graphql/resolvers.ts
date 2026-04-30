import { User } from '../mongoose/schema';

export const resolvers = {
  Query: {
    getUserAccounts: async (_: any, { userId }: { userId: string }) => {
      const user = await User.findById(userId);
      const accounts = user?.accounts || [];

      // Normalize nested audit timestamps to ISO strings to avoid inconsistent serialization
      return accounts.map((acc: any) => {
        const plainAcc = acc && typeof acc.toObject === 'function' ? acc.toObject() : { ...acc };
        if (Array.isArray(plainAcc.audits)) {
          plainAcc.audits = plainAcc.audits.map((a: any) => ({
            ...a,
            timestamp: (() => {
              const v = a?.timestamp;
              if (!v) return null;
              if (v instanceof Date) return v.toISOString();
              if (typeof v === 'number') return new Date(v).toISOString();
              if (typeof v === 'string' && /^\d+$/.test(v)) return new Date(Number(v)).toISOString();
              const d = new Date(v);
              return Number.isNaN(d.getTime()) ? null : d.toISOString();
            })()
          }));
        }
        return plainAcc;
      });
    },
    getAccountAudits: async (_: any, { userId, accountGuid }: any) => {
      const user = await User.findOne(
        { _id: userId, "accounts.accountGuid": accountGuid },
        { "accounts.$": 1 }
      );
      const audits = user?.accounts?.[0]?.audits || [];
      return audits.map((a: any) => ({
        ...(a && typeof a.toObject === 'function' ? a.toObject() : a),
        timestamp: (() => {
          const v = a?.timestamp;
          if (!v) return null;
          if (v instanceof Date) return v.toISOString();
          if (typeof v === 'number') return new Date(v).toISOString();
          if (typeof v === 'string' && /^\d+$/.test(v)) return new Date(Number(v)).toISOString();
          const d = new Date(v);
          return Number.isNaN(d.getTime()) ? null : d.toISOString();
        })()
      }));
    }
  },
  Account: {
    // This converts the MongoDB Decimal128 to a readable string
    balance: (parent: any) => parent.balance?.amount?.toString() || "0"
  },
  Audit: {
    // Serialize Date values to a stable ISO-8601 string format.
    timestamp: (parent: any) => {
      if (!parent?.timestamp) {
        return null;
      }

      const v = parent.timestamp;
      if (v instanceof Date) return v.toISOString();
      if (typeof v === 'number') return new Date(v).toISOString();
      if (typeof v === 'string' && /^\d+$/.test(v)) return new Date(Number(v)).toISOString();
      const d = new Date(v);
      return Number.isNaN(d.getTime()) ? null : d.toISOString();
    }
  }
};
