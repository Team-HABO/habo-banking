using MassTransit;
using Microsoft.Extensions.Logging;
using service_synchronize.Messages;
using service_synchronize.Services;

namespace service_synchronize.Consumers
{
    public class AccountCreatedConsumer(IAccountService accountService, ILogger<AccountCreatedConsumer> logger) : IConsumer<AccountEventEnvelope>
    {
        public async Task Consume(ConsumeContext<AccountEventEnvelope> context)
        {
            AccountMetadata metadata = context.Message.Metadata;
            AccountEventData data = context.Message.Data;

            if (data == null)
            {
                logger.LogWarning("Discarded account message with type {Type} because the payload was null.", metadata.MessageType);
                return;
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
                    await accountService.ProcessStatusChangeAsync(data.OwnerId, data.Account.AccountGuid, data.Account.IsFrozen ?? false);
                    break;

                default:
                    logger.LogWarning("Unhandled message type: {Type}", metadata.MessageType);
                    break;
            }
        }
    }
}
