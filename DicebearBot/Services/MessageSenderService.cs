using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DicebearBot.Services;

public interface IMessageSenderService
{
    Task SendMessageAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken);
    Task SendTextMessageAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, string messageText);
}

public class MessageSenderService(
    IConfiguration configuration,
    IHttpClientHelperService httpClientHelperService, 
    IGenerateKeybordService generateKeybordService,
    ILogger<MessageSenderService> logger) : IMessageSenderService
{

    private string Command { get; set; }
    private string Seed { get; set; }
    private bool OptionImage { get; set; }
    private string FormatImage { get; set; }
    private string Color { get; set; }
    
    public async Task SendMessageAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.Message)
        {
            var chatId = update.Message?.Chat.Id ?? 0;
            var username = update.Message?.From?.Username ?? string.Empty;
            var messageText = update.Message?.Text ?? string.Empty;

            try
            {
                if (messageText == "/help" || messageText == "/start")
                {
                    ClearFiledData();
                    await HandleHelpCommandAsync(botClient, update, cancellationToken, chatId, username, messageText);
                    return;
                }

                if (messageText == "/commands")
                {
                    ClearFiledData();
                    await HandleSendCommandsAsync(botClient, update, cancellationToken, chatId, username, messageText);
                    return;
                }

                if (Command != null && FormatImage != null && !OptionImage && !messageText.StartsWith('/'))
                {
                    var seedTexts = messageText.Split([' ', ','], ',').ToArray();
                    
                    await HandleImageCommandAsync(Command, seedTexts, botClient, update, cancellationToken, chatId, FormatImage);
                    ClearFiledData();
                    return;
                }
                
                if (Command != null && FormatImage != null && Color != null && OptionImage && !messageText.StartsWith('/'))
                {
                    var seedTexts = messageText.Split([' ', ','], ',').ToArray();
                    
                    await HandleImageCommandAsync(Command, seedTexts, botClient, update, cancellationToken, chatId, FormatImage, Color);
                    ClearFiledData();
                    return;
                }

                var messageTexts = messageText.Split([' ', ','], ',').ToArray();
                var command = messageTexts.FirstOrDefault();
                var seeds = messageTexts.Skip(1).ToArray();

                if (command?.StartsWith('/') == true)
                {
                    var commands = GetCommands("Dicebear", "Commands");
                    if (commands.Contains(command))
                    {
                        await HandleImageCommandAsync(command, seeds, botClient, update, cancellationToken, chatId);
                        ClearFiledData();
                    }
                    else
                    {
                        await HandleUnknownCommandAsync(botClient, update, cancellationToken, chatId, username, messageText);
                    }
                }
                else
                {
                    await HandlePlainTextMessageAsync(botClient, update, cancellationToken, chatId, messageText);
                    return;
                }
            }
            catch (SendMessageException ex)
            {
                logger.LogInformation($"Error while sending image. ChatId: {chatId}, Error: {ex.Message}");
                throw new SendMessageException("Rasmni yuborishda xatolik yuz berdi. Iltimos keyinroq urinib ko‘ring.", ex);
            }
        }
        else if (update.Type == UpdateType.CallbackQuery)
        {
            var command = update.CallbackQuery.Data;

            var commands = GetCommands("Dicebear", "Commands");
            var formatImages = GetCommands("Dicebear", "FormatImages");
            var optionImages = GetCommands("Dicebear", "OptionImages");

            if (commands.Contains(command))
            {
                await HandleCallbackSendFormatImageAsync(botClient, update.CallbackQuery, cancellationToken);
                return;
            }
            else if (formatImages.Contains(command))
            {
                await HandleCallbackSendOptionImageAsync(botClient, callbackQuery: update.CallbackQuery, cancellationToken);
                return;
            }
            else if (optionImages.Contains(command) && command == "NO")
            {
                OptionImage = false;
                await HandleMessageFromCallbackAsync(botClient, update, cancellationToken);
                return;

            }
            else if (optionImages.Contains(command) && command == "YES")
            {
                OptionImage = true;
                await HandleCallbackSendImageColorsAsync(botClient, callbackQuery: update.CallbackQuery, cancellationToken);
                return;
            }
            
            var colors = GetCommands("Dicebear", "Colors");
            
            if (colors.Contains(command))
            {
                Color = command;
                await HandleMessageFromCallbackAsync(botClient, update, cancellationToken);
                return;
            }
        }
    }
    
    private async Task HandleMessageFromCallbackAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        await botClient.SendMessage(
            chatId: update.CallbackQuery?.Message?.Chat.Id ?? 0,
            text: $@"✉ Seed matn yuboring, agarda bir nechta seed jo'natsangiz ptobel yoki vergul bilan ajrating ☢

 Misol: ali,vali g'ani",
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken);
    }
    
    private async Task HandleHelpCommandAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId, string username, string messageText)
    {
        var helpText = GetHelpCommandText();
        await SendTextMessageAsync(botClient, update, cancellationToken, helpText);
        
        logger.LogInformation(
            $"Sended message:  {helpText}, ChatId: {chatId}, Username: {username}, MessageText: {messageText}, DateTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    }

    private async Task HandleCallbackSendOptionImageAsync(ITelegramBotClient botClient, CallbackQuery  callbackQuery, CancellationToken cancellationToken)
    {
        var data = callbackQuery.Data;
        
        await botClient.AnswerCallbackQuery(
            callbackQueryId: callbackQuery.Id,
            text: $"Siz '{data}' ni tanladingiz",
            showAlert: false, 
            cancellationToken: cancellationToken);
        
        FormatImage = data;
        logger.LogInformation($"FormatImage data:  {FormatImage}");   
        
        if (callbackQuery.Message != null)
        {
            await botClient.DeleteMessage(
                chatId: callbackQuery.Message.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                cancellationToken: cancellationToken
            );
        }

        var text = $@"Rasm orqa fonini o'zgartiramizni? ";
                   
        var options = configuration.GetSection("Dicebear:OptionImages")
            .GetChildren()
            .Select(x => InlineKeyboardButton.WithCallbackData(x.Key, x.Value))
            .ToList();
        
        var buttonsPerRow = configuration.GetSection("Dicebear").GetValue<int>("ButtonsPerRow", 2);

        var inlineKeyboard = generateKeybordService.GenerateInlineKeyboardWithOtherCallbackData(options, buttonsPerRow);
        
        await botClient.SendMessage(
            chatId: callbackQuery.Message.Chat.Id,
            text: text,
            replyMarkup: inlineKeyboard,
            cancellationToken: cancellationToken);
        
        logger.LogInformation(
            $"Sended inline command list. ChatId: {callbackQuery.Message.Chat.Id}, Username: {callbackQuery.Message.Chat.Username}, MessageText: {text}, DateTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    }
    
    private async Task HandleCallbackSendImageColorsAsync(ITelegramBotClient botClient, CallbackQuery  callbackQuery, CancellationToken cancellationToken)
    {
        var data = callbackQuery.Data;
        
        await botClient.AnswerCallbackQuery(
            callbackQueryId: callbackQuery.Id,
            text: $"Siz '{data}' ni tanladingiz",
            showAlert: false, 
            cancellationToken: cancellationToken);
        
        logger.LogInformation($"FormatImage data:  {FormatImage}");
        
        if (callbackQuery.Message != null)
        {
            await botClient.DeleteMessage(
                chatId: callbackQuery.Message.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                cancellationToken: cancellationToken
            );
        }

        var text = $@"Rasm orqa fon rangini tanlang? ";
                   
        var colorSection = configuration.GetSection("Dicebear:Colors");

        var buttons = colorSection.GetChildren()
            .Select(x => InlineKeyboardButton.WithCallbackData(x.Key, x.Value)) // Emoji nomi — Key, Color hex — Value
            .ToList();

        var buttonsPerRow = configuration.GetValue<int>("Dicebear:ColorsRow", 4);

        var inlineKeyboard = generateKeybordService.GenerateInlineKeyboardWithOtherCallbackData(buttons, buttonsPerRow);

        await botClient.SendMessage(
            chatId: callbackQuery.Message.Chat.Id,
            text: text,
            replyMarkup: inlineKeyboard,
            cancellationToken: cancellationToken
        );
        
        logger.LogInformation(
            $"Sended inline command list. ChatId: {callbackQuery.Message.Chat.Id}, Username: {callbackQuery.Message.Chat.Username}, MessageText: {text}, DateTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    }

    private async Task HandleCallbackSendFormatImageAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var data = callbackQuery.Data;
        
        await botClient.AnswerCallbackQuery(
            callbackQueryId: callbackQuery.Id,
            text: $"Siz '{data}' ni tanladingiz",
            showAlert: false, cancellationToken: cancellationToken); 
        
        if (callbackQuery.Message != null)
        {
            await botClient.DeleteMessage(
                chatId: callbackQuery.Message.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                cancellationToken: cancellationToken
            );
        }

        Command = data;
        
        logger.LogInformation($"Command data:  {Command}");
        
        var formatImages = configuration.GetSection("Dicebear:FormatImages")
            .GetChildren()
            .Select(x => x.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
        
        var buttonsPerRow = configuration.GetSection("Dicebear").GetValue<int>("ButtonsPerRow", 2);

        var inlineKeyboard = generateKeybordService.GenerateInlineKeyboard(formatImages, buttonsPerRow);
        
        var text = "✅ Rasm formatini tanlang:";

        await botClient.SendMessage(
            chatId: callbackQuery.Message.Chat.Id,
            text: text,
            replyMarkup: inlineKeyboard,
            cancellationToken: cancellationToken);

        logger.LogInformation(
            $"Sended inline command list. ChatId: {callbackQuery.Message.Chat.Id}, Username: {callbackQuery.Message.Chat.Username}, MessageText: {data}, DateTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    }

    private async Task HandleSendCommandsAsync(
    ITelegramBotClient botClient,
    Update update,
    CancellationToken cancellationToken,
    long chatId,
    string username,
    string messageText)
    {
        if (update.Message != null)
        {
            await botClient.DeleteMessage(
                chatId: update.Message.Chat.Id,
                messageId: update.Message.Id,
                cancellationToken: cancellationToken
            );
        }
        
        var commands = configuration.GetSection("Dicebear:Commands")
            .GetChildren()
            .Select(x => x.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        int buttonsPerRow = configuration.GetSection("Dicebear").GetValue<int>("ButtonsPerRow", 2);

        var inlineKeyboard = generateKeybordService.GenerateInlineKeyboard(commands, buttonsPerRow);

        var text = "Quyidagi buyruqlardan birini tanlang:";

        await botClient.SendMessage(
            chatId: chatId,
            text: text,
            replyMarkup: inlineKeyboard,
            cancellationToken: cancellationToken);

        logger.LogInformation(
            $"Sended inline command list. ChatId: {chatId}, Username: {username}, MessageText: {messageText}, DateTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    }


    private async Task HandleImageCommandAsync(string command, string[] seeds, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId, string formatImage, string? backgroundImage = null)
    {
        if (seeds.Length == 0)
        {
            await SendTextMessageAsync(botClient, update, cancellationToken,
                $@"Iltimos, buyruqdan keyin matn (seed) kiriting. Misol uchun: {command} Eshmat");
            logger.LogInformation($"Sended message:  Iltimos, buyruqdan keyin matn kiriting. ChatId: {chatId}");
            return;
        }

        foreach (var seed in seeds)
        {
            await using var imageStream = await httpClientHelperService.GetImageStreamAsync(command, seed, formatImage, backgroundImage, cancellationToken);
            
            if (formatImage != null && formatImage == "svg")
            {
                await botClient.SendDocument(
                    chatId: chatId,
                    document: InputFile.FromStream(imageStream, $"{seed}.svg"),
                    caption: $"Sizning Dicebear rasmingiz 🧸,  nomi:   {seed}.svg",
                    cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendPhoto(
                    chatId: chatId,
                    photo: InputFile.FromStream(imageStream, $"{seed}.png"),
                    caption: $"Sizning Dicebear rasmingiz 🧸,  nomi:   {seed}.png",
                    cancellationToken: cancellationToken);
            }
         
            logger.LogInformation($"Sended image: {seed}.png, ChatId: {chatId}");
        }
    }



    private async Task HandleImageCommandAsync(string command, string[] seeds, ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId)
    {
        if (seeds.Length == 0)
        {
            await SendTextMessageAsync(botClient, update, cancellationToken,
                $@"Iltimos, buyruqdan keyin bitta probel tashlab matn (seed) kiriting. Misol uchun: {command} Eshmat");
            logger.LogInformation($"Sended message:  Iltimos, buyruqdan keyin matn kiriting. ChatId: {chatId}");
            return;
        }

        foreach (var seed in seeds)
        {
            await using var imageStream = await httpClientHelperService.GetImageStreamAsync(command, seed, cancellationToken);
            await botClient.SendPhoto(
                chatId: chatId,
                photo: InputFile.FromStream(imageStream, $"{seed}.png"),
                caption: $"Sizning Dicebear rasmingiz 🧸,  nomi:   {seed}.png",
                cancellationToken: cancellationToken);

            logger.LogInformation($"Sended image: {seed}.png, ChatId: {chatId}");
        }
    }
    
    

    private async Task HandleUnknownCommandAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId, string username, string messageText)
    {
        var commands = GetCommands("Dicebear", "Commands");
        await SendTextMessageAsync(botClient, update, cancellationToken,
            $@"Noma’lum buyruq. 
Quyidagi buyruqlardan birini ishlating:
{string.Join("\n", commands)}");

        logger.LogInformation($"Unknown command. ChatId: {chatId}, MessageText: {messageText}");
    }

    private async Task HandlePlainTextMessageAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId, string messageText)
    {
        await SendTextMessageAsync(botClient, update, cancellationToken, "Iltimos, avatar olish uchun buyruqdan foydalaning.");
        await HandleSendCommandsAsync(botClient, update, cancellationToken,  chatId, "Inner sender", messageText);
        logger.LogInformation($"Plain text message. ChatId: {chatId}, MessageText: {messageText}");
    }

    public async Task SendTextMessageAsync(
        ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken,
        string messageText)
    {
        await botClient.SendMessage(
                   chatId: update.Message?.Chat.Id ?? 0,
                   text: messageText,
                   parseMode: ParseMode.Markdown,
                   cancellationToken: cancellationToken);
    }


    private List<string> GetCommands(string sectionName, string filedName)
    {
        var commandsSection = configuration.GetSection($"{sectionName}:{filedName}");
        var commands = commandsSection.GetChildren()
            .Select(x => x.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        return commands;
    }

    private string GetHelpCommandText()
    {
        var helpText = configuration["Dicebear:HelpText"];

        if (helpText == null)
        {
            logger.LogError("Help text not found in configuration! Path: DicebearBot/appsettings.json, Parameter: Dicebear:HelpText");
            throw new Exception("Help text not found in configuration! Path: DicebearBot/appsettings.json, Parameter: Dicebear:HelpText");
        }

        return helpText;
    }
    
    private void ClearFiledData()
    {
        Command = default;
        Seed = default;
        OptionImage = default;
        FormatImage = default;
    }
    
}

