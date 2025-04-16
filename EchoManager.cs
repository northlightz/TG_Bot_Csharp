using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegramski_Botski;

public class EchoManager
{
    private readonly TelegramBotClient _botClient;
    private static readonly Dictionary<long, bool> _echoStates = new();

    public EchoManager(TelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public async Task HandleEcho(Message msg)
    {
        if (_echoStates.GetValueOrDefault(msg.Chat.Id, false) && !string.IsNullOrEmpty(msg.Text) && !msg.Text.StartsWith('/'))
        {
            var echoText = await LanguageManager.GetLocalizedString("echo", msg.Chat.Id);
            await _botClient.SendMessage(msg.Chat.Id, $"{echoText}: {msg.Text}");
        }
    }

    public async Task ToggleEcho(Chat chat)
    {
        _echoStates[chat.Id] = !_echoStates.GetValueOrDefault(chat.Id, false);
        await ShowEchoStatus(chat);
    }

    public async Task ShowEchoStatus(Chat chat)
    {
        var currentState = _echoStates.GetValueOrDefault(chat.Id, false);
        var buttonText = await LanguageManager.GetLocalizedString(currentState ? "toggle_off" : "toggle_on", chat.Id);
        var statusText = await LanguageManager.GetLocalizedString(currentState ? "echo_on" : "echo_off", chat.Id);

        await _botClient.SendMessage(
            chatId: chat.Id,
            text: statusText,
            replyMarkup: new InlineKeyboardMarkup(
                InlineKeyboardButton.WithCallbackData(buttonText, "/echo/toggle")
            )
        );
    }
} 