
using Telegram.Bot.Types.ReplyMarkups;

public class GenerateKeybordService : IGenerateKeybordService
{

    public InlineKeyboardMarkup GenerateInlineKeyboard(List<string> commands, int columns = 2)
    {
        var keyboard = new List<List<InlineKeyboardButton>>();

        for (int i = 0; i < commands.Count; i += columns)
        {
            var row = commands
                .Skip(i)
                .Take(columns)
                .Select(cmd => InlineKeyboardButton.WithCallbackData(cmd, cmd))
                .ToList();

            keyboard.Add(row);
        }

        return new InlineKeyboardMarkup(keyboard);
    }

    public InlineKeyboardMarkup GenerateInlineKeyboardWithOtherCallbackData(List<InlineKeyboardButton> buttons, int columns = 4)
    {
        var keyboard = new List<List<InlineKeyboardButton>>();

        for (int i = 0; i < buttons.Count; i += columns)
        {
            var row = buttons.Skip(i).Take(columns).ToList();
            keyboard.Add(row);
        }

        return new InlineKeyboardMarkup(keyboard);
    }

}

public interface IGenerateKeybordService
{
    InlineKeyboardMarkup GenerateInlineKeyboard(List<string> commands, int columns = 2);
    InlineKeyboardMarkup GenerateInlineKeyboardWithOtherCallbackData(List<InlineKeyboardButton> buttons, int columns = 4);
}