export const typeDefs = `#graphql
  type Audit {
    auditId: ID
    amount: String
    type: String
    timestamp: String
    receiver: String
    sender: String
  }

  type Account {
    accountGuid: ID!
    type: String
    name: String
    balance: String
    audits: [Audit]
  }

  type Query {
    getUserAccounts: [Account]
    getAccountAudits(accountGuid: ID!): [Audit]
  }
`;