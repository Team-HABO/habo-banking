using System.Globalization;
using DnsClient.Internal;
using Microsoft.Extensions.Logging;
using service_synchronize.Database;
using service_synchronize.Messages;
using service_synchronize.Models;

namespace service_synchronize.Services
{
    public class TransactionService(IUsersRepository usersRepository, ILogger<TransactionService> logger) : ITransactionService
    {
        public enum TransactionMessageType
        {
            TRANSACTION_TRANSFER, WITHDRAW, DEPOSIT
        }
        private async Task ProcessDeposit(string ownerId, string accountGuid, decimal amount, Audit newAudit)
        {
            await usersRepository.UpdateUserWithNewTransaction(ownerId, accountGuid, amount, newAudit);
        }
        private async Task ProcessWithdraw(string ownerId, string accountGuid, decimal amount, Audit newAudit)
        {
            await usersRepository.UpdateUserWithNewTransaction(ownerId, accountGuid, amount * -1, newAudit);
        }
        private async Task ProcessTransfer(string ownerId, string ownerAccountGuid, decimal amount, Audit senderAudit, string receiverAccountGuid, Audit receiverAudit)
        {
            string? receiverUserId = await usersRepository.GetUserIdByAccountGuidAsync(receiverAccountGuid) ?? throw new InvalidDataException($"Invalid user with account GUID ${receiverAccountGuid} could not be found");
            await usersRepository.ExecuteTransferAsync(ownerId, ownerAccountGuid, amount, senderAudit, receiverUserId, receiverAccountGuid, receiverAudit);
        }

        public async Task ProcessTransaction(TransactionCreated message)
        {
            TransactionCreatedData messageData = message.Data;
            string ownerId = messageData.OwnerId;
            string ownerAccountId = messageData.Account.Guid;
            string auditId = message.Metadata.MessageId;

            if (!Enum.TryParse(message.Metadata.MessageType, true, out TransactionMessageType transactionType))
            {
                logger.LogWarning("Discarded: Message {Guid} has an invalid Type '{Type}'",
                    message.Metadata.MessageId, message.Metadata.MessageType);
                return;
            }
            if (!decimal.TryParse(messageData.Account.Audit.Amount, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount) || amount <= 0)
            {
                throw new InvalidDataException($"Invalid amount format: {messageData.Account.Audit.Amount}");
            }

            // Check idempotency
            if (await usersRepository.AuditExistsAsync(ownerId, auditId))
            {
                logger.LogInformation("Message {Id} already processed. Skipping.", auditId);
                return;
            }
            Audit ownerAudit = MapAudit(messageData.Account.Audit, message.Metadata.MessageId);
            switch (transactionType)
            {
                case TransactionMessageType.DEPOSIT:
                    await ProcessDeposit(ownerId, ownerAccountId, amount, ownerAudit);
                    break;
                case TransactionMessageType.WITHDRAW:
                    await ProcessWithdraw(ownerId, ownerAccountId, amount, ownerAudit);
                    break;
                case TransactionMessageType.TRANSACTION_TRANSFER:
                    TransactionCreatedAccountDto? receiver = messageData.Receiver ?? throw new InvalidOperationException($"Transfer {auditId} is missing receiver data.");
                    Audit receiverAudit = MapAudit(messageData.Receiver.Audit, message.Metadata.MessageId);

                    receiverAudit.SenderAccountName = messageData.Receiver.Audit.ReceiverAccountName;
                    ownerAudit.ReceiverAccountName = messageData.Account.Audit.ReceiverAccountName;
                    await ProcessTransfer(ownerId, ownerAccountId, amount, ownerAudit, receiver.Guid, receiverAudit);
                    break;
            }
        }
        public static Audit MapAudit(AuditDto dto, string auditGuid)
        {
            return !Enum.TryParse(dto.Type, true, out Audit.AuditType type)
                ? throw new InvalidDataException($"Invalid audit type: {dto.Type}")
                : new Audit
                {
                    AuditId = auditGuid,
                    Type = type,
                    Amount = dto.Amount,
                    Timestamp = dto.Timestamp
                };
        }

    }
}
