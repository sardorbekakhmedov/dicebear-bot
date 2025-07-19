
using DicebearBot.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Polling;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHttpClient();

var botToken = builder.Configuration["TelegramConfiguration:BotToken"];

builder.Services.AddSingleton<ITelegramBotClient, TelegramBotClient>(
    conf => new TelegramBotClient(botToken ?? throw new ArgumentException("Telegram BotToken not found!!!")));
builder.Services.AddSingleton<IUpdateHandler, BotUpdateHandlerSerive>();
builder.Services.AddTransient<IMessageSenderService, MessageSenderService>();
builder.Services.AddTransient<IHttpClientHelperService, HttpClientHelperService>();

builder.Services.AddHostedService<BotHostedService>();

await builder.Build().StartAsync();