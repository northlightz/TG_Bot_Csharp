using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegramski_Botski;

public class BotCommands(TelegramBotClient botClient)
{
    private readonly TelegramBotClient _botClient = botClient;
    private static readonly Dictionary<long, bool> _echoStates = new();

    public async Task HandleCommands(Message msg)
    {
        string MessageText = msg.Text;
        Chat CurrentBotChat = msg.Chat;

        // First check if echo is enabled and message isn't a command
        if (
            _echoStates.GetValueOrDefault(msg.Chat.Id, false)
            && !string.IsNullOrEmpty(msg.Text)
            && !msg.Text.StartsWith('/')
        )
        {
            await _botClient.SendMessage(msg.Chat.Id, $"Echo: {msg.Text}");
            return;
        }

        // Check if the message starts with a command prefix ('/') and handle accordingly.
        if (MessageText != null && MessageText.StartsWith('/'))
        {
            var BotCommand = MessageText.Split('/')[1]; // Extract command text after '/'
            await HandleCommand(BotCommand, CurrentBotChat);
        }
    }

    public async Task HandleCommand(string command, Chat chat)
    {
        if (CommandRegistry.Commands.TryGetValue(command, out var handler))
        {
            await handler(this, chat);
        }
    }

    // Send welcome message with an inline button for help.
    public async Task StartCommand(Chat chat)
    {
        await _botClient.SendMessage(
            chatId: chat,
            parseMode: ParseMode.Html,
            text: "Hello and welcome to this bot! To see more information do /help.",
            replyMarkup: new InlineKeyboardButton(text: "Show Help", callbackDataOrUrl: "/help")
        );
    }

    // Provide help information about available commands and functionality.
    public async Task HelpCommand(Chat chat)
    {
        var buttons = CommandRegistry
            .Commands.Keys.Select(c => InlineKeyboardButton.WithCallbackData(c, $"/{c}"))
            .ToArray();

        await _botClient.SendMessage(
            chatId: chat.Id,
            text: $"Available commands:\n{CommandRegistry.GetHelpText()}",
            replyMarkup: new InlineKeyboardMarkup(buttons)
        );
    }

    // Provide echo functionality, i.e. just send the same messages it gets back to you.
    public async Task EchoCommand(Chat chat)
    {
        var currentState = _echoStates.GetValueOrDefault(chat.Id, false);
        var buttonText = currentState ? "Toggle Off" : "Toggle On";

        await _botClient.SendMessage(
            chatId: chat.Id,
            text: $"Echo is currently {(currentState ? "ON" : "OFF")}",
            replyMarkup: new InlineKeyboardMarkup(
                InlineKeyboardButton.WithCallbackData(buttonText, "/echo/toggle")
            )
        );
    }

    // Add this new method to handle toggle requests
    public async Task HandleEchoToggle(Chat chat)
    {
        _echoStates[chat.Id] = !_echoStates.GetValueOrDefault(chat.Id, false);
        await EchoCommand(chat);
    }


    public static async Task HandleInlineButtonPress(
        BotCommands botCommands,
        string callbackData,
        Message msg
    )
    {
        Chat CurrentBotChat = msg.Chat;
        string commandstrip = callbackData.Split('/')[1];
        if (callbackData == "/echo/toggle")
        {
            await botCommands.HandleEchoToggle(msg.Chat);
            return;
        }

        switch (commandstrip)
        {
            // Respond when 'help' button is pressed.
            case "help":
                await botCommands.HandleCommand(commandstrip, CurrentBotChat);
                break;

            // Respond on 'start' button pressed
            case "start":
                await botCommands.HandleCommand(commandstrip, CurrentBotChat);
                break;
            // Respond on 'echo' button pressed
            case "echo":
                await botCommands.HandleCommand(commandstrip, CurrentBotChat);
                break;

            default:
                break;
                // Add more inline button actions as needed here.
        }
    }
}
