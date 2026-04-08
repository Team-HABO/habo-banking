
using MassTransit;
using Microsoft.Extensions.Logging;
using service_synchronize.Messages;
using service_synchronize.Models;
using service_synchronize.Services;

namespace service_synchronize.Consumers
{
    public class AccountCreatedConsumer(IAccountService accountService, ILogger<AccountCreatedConsumer> logger) : IConsumer<AccountCreated>
    {
        public async Task Consume(ConsumeContext<AccountCreated> context)
        {
            AccountCreated message = context.Message;

            if (message.Metadata.MessageType != "ACCOUNT_CREATE")
            {
                logger.LogWarning("Discarded message with unexpected type: {Type}", message.Metadata.MessageType);
                return;
            }
            if (!Enum.TryParse<Account.AccountType>(message.Data.Account.Type, true, out _))
            {
                logger.LogWarning("Discarded: Account {Guid} has an invalid Type '{Type}'",
                    message.Data.Account.AccountGuid, message.Data.Account.Type);
                return;
            }

            try
            {
                await accountService.ProcessAccountCreationAsync(message.Data.OwnerId, message.Data.Account);
            }
            catch (InvalidDataException ex)
            {
                logger.LogWarning(ex,
                    "Discarded invalid account message. AccountGuid: {Guid}, OwnerId: {OwnerId}",
                    message.Data.Account.AccountGuid,
                    message.Data.OwnerId);
            }
        }
    }
}
