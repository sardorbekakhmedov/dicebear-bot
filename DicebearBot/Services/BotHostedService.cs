using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace DicebearBot.Services;

public class BotHostedService(
    ILogger<BotHostedService> logger,
    IUpdateHandler updateHandler,
    ITelegramBotClient botClient) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await botClient.DeleteWebhook(cancellationToken: cancellationToken);

        var bot = await botClient.GetMe(cancellationToken);

        logger.LogInformation($@"======>>> {bot.FirstName ?? bot.Username} bot started, Username: {bot.Username},  BOTID: {bot.Id}, DateTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss} <<<=====");

        await botClient.ReceiveAsync(
            updateHandler: updateHandler,
            cancellationToken: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation($"{nameof(BotHostedService)}  stopping!...");
        return Task.CompletedTask;
    }
}