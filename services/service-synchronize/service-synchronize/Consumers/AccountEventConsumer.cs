using MassTransit;
using Microsoft.Extensions.Logging;
using service_synchronize.Messages;
using service_synchronize.Services;

namespace service_synchronize.Consumers
{
    public class AccountEventConsumer(IAccountService accountService, ILogger<AccountEventConsumer> logger) : IConsumer<AccountEventEnvelope>
    {
        public async Task Consume(ConsumeContext<AccountEventEnvelope> context)
        {
            if (context.Message == null)
            {
                logger.LogWarning("Received an empty message envelope.");
                return;
            }

            AccountMetadata? metadata = context.Message.Metadata;
            AccountEventData? data = context.Message.Data;

            try
            {
                if (metadata == null || data == null || data.Account == null)
                {
                    throw new InvalidDataException("Message is missing required Metadata, Data, or Account blocks.");
                }

                switch (metadata.MessageType)
                {
                    case "ACCOUNT_CREATE":
                        await accountService.ProcessAccountCreationAsync(data.OwnerId, data.Account);
                        break;

                    case "ACCOUNT_DELETE":
                        await accountService.ProcessAccountDeletionAsync(data.OwnerId, data.Account.AccountGuid);
                        break;

                    case "ACCOUNT_UPDATE":
                        await accountService.ProcessAccountUpdateAsync(data.OwnerId, data.Account);
                        break;

                    case "ACCOUNT_STATUS":
                        if (!data.Account.IsFrozen.HasValue)
                        {
                            throw new InvalidDataException($"Status update failed: 'isFrozen' field is missing for Account {data.Account.AccountGuid}");
                        }
                        await accountService.ProcessStatusChangeAsync(data.OwnerId, data.Account.AccountGuid, data.Account.IsFrozen.Value, metadata.MessageTimestamp);
                        break;

                    default:
                        logger.LogWarning("Unhandled message type: {Type}", metadata.MessageType);
                        break;
                }
            }
            catch (InvalidDataException ex)
            {
                logger.LogWarning(ex, "Discarding malformed account message. MessageType: {Type}", metadata?.MessageType ?? "Unknown");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error processing account message.");
                throw;
            }
        }
    }
}
