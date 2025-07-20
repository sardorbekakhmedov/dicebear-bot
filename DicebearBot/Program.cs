using DicebearBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Polling;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddSerilog();

var logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
Directory.CreateDirectory(logDirectory);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddHttpClient();

var botToken = builder.Configuration["TelegramConfiguration:BotToken"];

builder.Services.AddSingleton<ITelegramBotClient, TelegramBotClient>(
    conf => new TelegramBotClient(botToken ?? throw new ArgumentException("Telegram BotToken not found!!!")));

builder.Services.AddSingleton<IUpdateHandler, BotUpdateHandlerSerive>();
builder.Services.AddTransient<IMessageSenderService, MessageSenderService>();
builder.Services.AddTransient<IHttpClientHelperService, HttpClientHelperService>();

builder.Services.AddHostedService<BotHostedService>();

await builder.Build().RunAsync(); 
