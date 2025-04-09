using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegramski_Botski;

public class BotCommands(TelegramBotClient botClient)
{
    private readonly TelegramBotClient _botClient = botClient;

    public async Task HandleCommands(Message msg)
    {
        string MessageText = msg.Text;
        Chat CurrentBotChat = msg.Chat;

        // Check if the message starts with a command prefix ('/') and handle accordingly.
        if (MessageText != null && MessageText.StartsWith('/'))
        {
            var BotCommand = MessageText.Split('/')[1]; // Extract command text after '/'
            await HandleCommand(BotCommand, CurrentBotChat);
        }
    }

    public async Task HandleCommand(string command, Chat chat)
    {
        switch (command)
        {
            case "start":
                await StartCommand(chat);
                break;
            case "help":
                await HelpCommand(chat);
                break;
            default:
                break;
            // Add more commands as needed here.
        }
    }

    private async Task StartCommand(Chat chat)
    {
        // Send welcome message with an inline button for help.
        await _botClient.SendMessage(
            chatId: chat,
            parseMode: ParseMode.Html,
            text: "Hello and welcome to this bot! To see more information do /help.",
            replyMarkup: new InlineKeyboardButton(text: "Show Help", callbackDataOrUrl: "/help")
        );
    }

    public async Task HelpCommand(Chat chat)
    {
        // Provide help information about available commands and functionality.
        await _botClient.SendMessage(
            chatId: chat,
            parseMode: ParseMode.Html,
            text: "Welcome to this bot! It's heavily in development. Available commands are /start and /help.",
            replyMarkup: new InlineKeyboardButton[]
            {
                (text: "Show Help", callbackDataOrUrl: "/help"),
                (text: "/start", callbackDataOrUrl: "https://www.microsoft.com/"),
            }
        );
    }

    public static async Task HandleInlineButtonPress(
        BotCommands botCommands,
        string callbackData,
        Message msg
    )
    {
        Chat CurrentBotChat = msg.Chat;
        string commandstrip = callbackData.Split('/')[1];
        switch (commandstrip)
        {
            // Respond when 'help' button is pressed.
            case "help":
                await botCommands.HandleCommand(commandstrip, CurrentBotChat);
                break;
            default:
                break;
            // Add more inline button actions as needed here.
        }
    }
}
