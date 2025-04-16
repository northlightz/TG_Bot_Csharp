using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegramski_Botski;

public class LanguageManager
{
    private readonly TelegramBotClient _botClient;

    public LanguageManager(TelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public async Task ToggleLanguage(Chat chat)
    {
        var currentLanguage = await DatabaseManager.GetChatLanguage(chat.Id);
        var newLanguage = currentLanguage == "en" ? "fa" : "en";
        await DatabaseManager.SetChatLanguage(chat.Id, newLanguage);
        await ShowLanguageStatus(chat);
    }

    public async Task ShowLanguageStatus(Chat chat)
    {
        var currentLanguage = await DatabaseManager.GetChatLanguage(chat.Id);
        var buttonText = currentLanguage == "en" ? "English" : "فارسی";
        var statusText = currentLanguage == "en" ? "زبان فعلی: فارسی" : "Current language: English";

        await _botClient.SendMessage(
            chatId: chat.Id,
            text: statusText,
            replyMarkup: new InlineKeyboardMarkup(
                InlineKeyboardButton.WithCallbackData(buttonText, "/language/toggle")
            )
        );
    }

    public static async Task<string> GetLocalizedString(string key, long chatId)
    {
        var language = await DatabaseManager.GetChatLanguage(chatId);
        
        return language == "fa" ? GetPersianString(key) : GetEnglishString(key);
    }

    private static string GetPersianString(string key)
    {
        return key switch
        {
            "echo_on" => "اکو روشن است",
            "echo_off" => "اکو خاموش است",
            "toggle_on" => "روشن کردن",
            "toggle_off" => "خاموش کردن",
            _ => key
        };
    }

    private static string GetEnglishString(string key)
    {
        return key switch
        {
            "echo_on" => "Echo is ON",
            "echo_off" => "Echo is OFF",
            "toggle_on" => "Toggle On",
            "toggle_off" => "Toggle Off",
            _ => key
        };
    }
} 