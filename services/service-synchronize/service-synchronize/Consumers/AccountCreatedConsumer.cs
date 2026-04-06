
using MassTransit;
using Microsoft.Extensions.Logging;
using service_synchronize.Messages;
using service_synchronize.Services;

namespace service_synchronize.Consumers
{
    public class AccountCreatedConsumer(IAccountService accountService, ILogger<AccountCreatedConsumer> logger) : IConsumer<AccountCreated>
    {
        public async Task Consume(ConsumeContext<AccountCreated> context)
        {
            try
            {
                AccountCreated message = context.Message;

                if (message.Metadata.MessageType != "ACCOUNT_CREATE")
                {
                    logger.LogWarning("Discarded message with unexpected type: {Type}", message.Metadata.MessageType);
                    return;
                }
                await accountService.ProcessAccountCreationAsync(message.Data.OwnerId, message.Data.Account);
            }
            catch (InvalidDataException ex)
            {
                // No retry
                logger.LogWarning(ex, "Permanent data error for User {UserId}. Moving to error queue.", context.Message.Data.OwnerId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transient error processing user {UserId}. Triggering retry.", context.Message.Data.    OwnerId);
                // Using throw will make it retry
                throw;
            }
        }
    }
}
