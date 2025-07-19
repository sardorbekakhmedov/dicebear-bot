using DicebearBot.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Polling;

try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Logging.AddSerilog();
    
    var logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
    Directory.CreateDirectory(logDirectory);

    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
        .CreateLogger();

    builder.Services.AddHttpClient();

    var botToken = builder.Configuration["TelegramConfiguration:BotToken"];

    builder.Services.AddSingleton<ITelegramBotClient, TelegramBotClient>(
        conf => new TelegramBotClient(botToken ?? throw new ArgumentException("Telegram BotToken not found!!!")));

    builder.Services.AddSingleton<IUpdateHandler, BotUpdateHandlerSerive>();
    builder.Services.AddTransient<IMessageSenderService, MessageSenderService>();
    builder.Services.AddTransient<IHttpClientHelperService, HttpClientHelperService>();

    builder.Services.AddHostedService<BotHostedService>();

    await builder.Build().RunAsync(); // RunAsync - bu yerda to‘g‘riroq
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application stopped");
}
finally
{
    Log.CloseAndFlush();
}