using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegramski_Botski.Models;

namespace Telegramski_Botski;

public class BotCommands(TelegramBotClient botClient)
{
    private static readonly Dictionary<long, string> _userStates = new();
    private static readonly List<Developer> _developers = new()
    {
        new Developer
        {
            Id = "northlightz",
            FullName = "Seyed Ali Goushegir",
            Username = "northlightz",
            Bio = "Rarely coding, prefer IT Support",
            Skills = new List<string> { "Linux Sysadmin" },
            Links = new Dictionary<string, string> { { "GitHub", "github.com/northlightz" } },
            ProfilePhoto = null,
        },
        new Developer
        {
            Id = "leo69x",
            FullName = "Seyed Mohammad Mousavi-Kia",
            Username = "leo69x",
            Bio = "Trying to learn C",
            Skills = new List<string> { "Python programming" },
            Links = new Dictionary<string, string> { { "GitHub", "github.com/leo69x" } },
            ProfilePhoto = null,
        },
    };

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

    public async Task ShowDevelopersList(Chat chat)
    {
        // Correctly create buttons dynamically from the list of developers
        var buttons = _developers
            .Select(dev =>
                new[] { InlineKeyboardButton.WithCallbackData(dev.FullName, $"/devs/{dev.Id}") }
            )
            .ToArray();

        await _botClient.SendMessage(
            chatId: chat.Id,
            text: "🚀 Meet Our Developers:",
            replyMarkup: new InlineKeyboardMarkup(buttons)
        );
    }

    public async Task ShowDeveloperProfile(Chat chat, string devId)
    {
        var developer = _developers.FirstOrDefault(dev => dev.Id == devId);

        if (developer == null)
        {
            await _botClient.SendMessage(chatId: chat.Id, text: "Developer not found.");
            return;
        }

        // Format the developer's profile
        var profileText =
            $"\u200D <b>{developer.FullName}</b> (@{developer.Username})\n\n"
            + $"📝 <i>{developer.Bio}</i>\n\n"
            + $"Skills: {string.Join(", ", developer.Skills)}\n\n"
            + $"Links:\n"
            + string.Join(
                "\n",
                developer.Links.Select(link => $"<a href=\"{link.Value}\">{link.Key}</a>")
            );

        await _botClient.SendMessage(
            chatId: chat.Id,
            text: profileText,
            parseMode: ParseMode.Html,
            replyMarkup: new InlineKeyboardMarkup(
                InlineKeyboardButton.WithCallbackData("← Back to Devs", "/devs")
            )
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
        if (callbackData == "/echo/toggle")
        {
            await botCommands.HandleEchoToggle(msg.Chat);
            return;
        }

        if (callbackData == "/devs")
        {
            await botCommands.ShowDevelopersList(msg.Chat);
            return;
        }

        if (callbackData.StartsWith("/devs/"))
        {
            var devId = callbackData.Split('/')[2];
            await botCommands.ShowDeveloperProfile(msg.Chat, devId);
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
