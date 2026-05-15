import { gql } from "@apollo/client/core";

export const GET_USER_ACCOUNTS = gql`
    query GetUserAccounts {
        getUserAccounts {
            accountGuid
            type
            name
            balance
            isFrozen
        }
    }
`;

export const GET_ACCOUNT_AUDITS = gql`
    query GetAccountAudits($accountGuid: ID!) {
        getAccountAudits(accountGuid: $accountGuid) {
            auditId
            amount
            type
            timestamp
            receiver
            sender
        }
    }
`;
