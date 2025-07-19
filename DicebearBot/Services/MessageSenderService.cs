using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;


namespace DicebearBot.Services;

public interface IMessageSenderService
{
    Task SendMessageToBotAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken);
    Task SendTextMessageAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, string messageText);
}

public class MessageSenderService(
    IHttpClientHelperService httpClientHelperService,
    ILogger<MessageSenderService> logger) : IMessageSenderService
{
    public async Task SendMessageToBotAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type is Telegram.Bot.Types.Enums.UpdateType.Message)
        {
            var chatId = update.Message?.Chat.Id ?? 0;
            var username = update.Message?.From?.Username ?? string.Empty;
            var messageText = update.Message?.Text;
            try
            {
                if (messageText == "/help")
                {
                    await SendTextMessageAsync(
                        botClient: botClient,
                        update: update,
                        cancellationToken: cancellationToken,
                        messageText: GetHelpCommandText());
                    
                    logger.LogInformation(
                        $@"{GetHelpCommandText()}, ChatId: {chatId},  Username: {username}, MessageText: {messageText}, DateTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    
                    return;
                }

                var messageTexts = (messageText ?? "avataaars robot").Split(' ', ',');

                System.Console.WriteLine("++++++++++++++++   PRINT ++++++++++++++++++++++");
                System.Console.WriteLine(string.Join(" ", messageTexts));

                if (messageTexts.Length > 0 && messageTexts[0].StartsWith('/'))
                {
                    var commands = GetCommands();
                    var newCommand = messageTexts[0];

                    if (commands.Contains(newCommand))
                    {
                        if (messageTexts.Length == 1)
                        {
                            await SendTextMessageAsync(
                                botClient: botClient,
                                update: update,
                                cancellationToken: cancellationToken,
                                messageText: $@"Iltimos, buyruqdan keyin bitta probel tashlab matn (seed) kiriting. 
Misol uchun: {newCommand} Eshmat");
                            
                            logger.LogInformation($"Iltimos, buyruqdan keyin matn (seed) kiriting. Misol: /fun-emoji Eshmat. ChatId: {chatId},  Username: {username}, MessageText: {messageText}, DateTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                        }
                        else
                        {
                            for (var i = 1; i < messageTexts.Length; i++)
                            {
                                var seed = messageTexts[i];

                                var (imageStream, isSuccess) =
                                    await httpClientHelperService.GetImageStreamAsync(newCommand, seed);

                                if (isSuccess)
                                {
                                    await botClient.SendPhoto(
                                        chatId: chatId,
                                        photo: InputFile.FromStream(imageStream, $"{seed}.png"),
                                        caption: $"Sizning Dicebear rasmingiz,  nomi:   {seed}.png",
                                        cancellationToken: cancellationToken);
                                
                                    logger.LogInformation(
                                        $"Sizning Dicebear rasmingiz,  nomi: {seed}.png. ChatId: {chatId},  Username: {username}, MessageText: {messageText}, DateTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                                }
                                else
                                {
                                    await SendTextMessageAsync(
                                        botClient: botClient,
                                        update: update,
                                        cancellationToken: cancellationToken,
                                        messageText:
                                        @"Avatar yaratishda xatolik yuz berdi. Iltimos keyinroq urinib ko‘ring.");
                                
                                    logger.LogInformation(
                                        $"Avatar yaratishda xatolik yuz berdi. Iltimos keyinroq urinib ko‘ring. ChatId: {chatId},  Username: {username}, MessageText: {messageText}, DateTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                                }
                            }
                        }
                    }
                    else
                    {
                        await SendTextMessageAsync(
                            botClient: botClient,
                            update: update,
                            cancellationToken: cancellationToken,
                            messageText: $@"Noma’lum buyruq.
Quyidagi buyruqlardan birini ishlating:

/fun-emoji
/bottts
/avataaars
/pixel-art");
                        
                        logger.LogInformation(
                            $@"Noma’lum buyruq.Quyidagi buyruqlardan birini ishlating: /fun-emoji, /bottts, /avataaars, /pixel-art. ChatId: {chatId},  Username: {username}, MessageText: {messageText}, DateTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    }
                }
                else
                {
                    await SendTextMessageAsync(
                        botClient: botClient,
                        update: update,
                        cancellationToken: cancellationToken,
                        messageText: @"Iltimos, avatar olish uchun buyruqdan foydalaning.");
                    
                    logger.LogInformation(
                        $"Iltimos, avatar olish uchun buyruqdan foydalaning. ChatId: {chatId},  Username: {username}, MessageText: {messageText}, DateTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                }


            }
            catch (SendMessageException e)
            {
                var from = update.Message?.From;
                logger.LogInformation(
                    $"Rasmni yuborishda xatolik yuz berdi. ChatId: {chatId},  Username: {username}, MessageText: {messageText}, DateTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                throw;
            }
        }
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
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    cancellationToken: cancellationToken);
    }


    private List<string> GetCommands()
    {
        return new List<string>()
        {
            "/fun-emoji",
            "/avataaars", 
            "/bottts",
            "/pixel-art", 
        };
    }

    private string GetHelpCommandText()
    {
        return "\ud83d\udc4b Salom! Men Dicebear avatar botiman.\n\n\ud83c\udfaf Men sizga turli uslubdagi avatarlar yaratishda yordam beraman. Buning uchun quyidagi buyruqlardan birini yuboring:\n\n\ud83d\udccc Avatar yaratish buyruqlari:\n- /fun-emoji [matn] — fun-emoji uslubida avatar\n- /avataaars [matn] — avataaars uslubida avatar\n- /bottts [matn] — bottts uslubida avatar\n- /pixel-art [matn] — pixel-art uslubida avatar\n\n\ud83d\udcdd Masalan:\n`/fun-emoji Ali` — bu \"Ali\" so'zidan fun-emoji uslubidagi avatar yaratadi.\n\n\u26a0\ufe0f Eslatma:\n- Buyruqdan keyin matn yozishni unutmang!\n- Agar noto‘g‘ri buyruq yuborsangiz, sizga qanday buyruqlar borligini eslataman.\n- Oddiy matn yuborsangiz, sizni buyruqdan foydalanishga undayman.\n\n\ud83d\udd04 Har safar siz yuborgan buyruq va holat haqida log yozib boraman.\n\n\u2705 Bonus:\nSiz bir nechta so‘zdan iborat matn yuborsangiz ham ishlaydi:\n`/bottts John Doe`\n\nYordam kerak bo‘lsa, har doim shu buyrug‘ni (/help) yozishingiz mumkin.\n\n\ud83d\ude80 Hozir sinab ko‘ring!";
    }
}

public class SendMessageException(string message) : Exception(message);