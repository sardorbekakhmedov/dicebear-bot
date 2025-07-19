using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace DicebearBot.Services;

public class BotUpdateHandlerSerive(
    IMessageSenderService messageSenderService,
    ILogger<BotUpdateHandlerSerive> logger) : IUpdateHandler
{
    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, $"Sorry, something went wrong. Details: {exception.Message}", cancellationToken);
        return Task.CompletedTask;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            var from = update.Message?.From;
            logger.LogInformation($"From: {from?.Username ?? from?.FirstName ?? from!.Id.ToString()}, Message type: {update.Type}, DateTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            await messageSenderService.SendMessageToBotAsync(botClient, update, cancellationToken);
        }
        catch (DicebearBotException ex)
        {
            logger.LogError(ex, $"Failed to complete the request,  error message: {ex.Message}", ex.Message);
            await messageSenderService.SendTextMessageAsync(
                botClient: botClient,
                update: update,
                cancellationToken: cancellationToken,
                messageText: @"Avatar yaratishda xatolik yuz berdi. Iltimos keyinroq urinib ko‘ring.");
        }
        catch (SendMessageException ex)
        {
            logger.LogError(ex, $"Failed to complete the request,  error message: {ex.Message}", ex.Message);
            await messageSenderService.SendTextMessageAsync(
                botClient: botClient,
                update: update,
                cancellationToken: cancellationToken,
                messageText: @"Rasmni yuborishda xatolik yuz berdi.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to complete the request,  error message: {ex.Message}", ex.Message);
            await messageSenderService.SendTextMessageAsync(
                botClient: botClient,
                update: update,
                cancellationToken: cancellationToken,
                messageText: @"Kutilmagan xatolik yuz berdi. Keyinroq urinib ko'ring.");
        }
    
    }
}