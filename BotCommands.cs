using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Text;

namespace Telegramski_Botski;

public class BotCommands(TelegramBotClient botClient, string botUsername)
{
    private readonly TelegramBotClient _botClient = botClient;
    private readonly string _botUsername = botUsername;
    private static readonly Dictionary<long, bool> _echoStates = new();

    public async Task HandleCommands(Message msg)
    {
        if (msg.From != null)
        {
        // Track the user who sent the message
        // پیگیری کاربری که پیام را ارسال کرده است
            await DatabaseManager.TrackUser(msg.From);
        // Track the chat where the message was sent
        // پیگیری چتی که پیام در آن ارسال شده است
            await DatabaseManager.TrackChat(msg.Chat);
        }

        string MessageText = msg.Text;
        Chat CurrentBotChat = msg.Chat;

        // First check if echo is enabled and message isn't a command
        // ابتدا بررسی کنید که آیا اکو فعال است و پیام یک دستور نیست
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
        // بررسی کنید که آیا پیام با پیشوند دستور ('/') شروع می‌شود و بر اساس آن اقدام کنید.
        if (MessageText != null && MessageText.StartsWith('/'))
        {
            // Extract command text after '/'
            // استخراج متن دستور بعد از '/'
            string fullCommand = MessageText.Substring(1); // Remove the leading '/'

            // Handle commands with username (like "start@PlutoRunnerBot")
            // مدیریت دستورات با نام کاربری (مانند "start@PlutoRunnerBot")
            if (fullCommand.Contains("@PlutoRunnerBot"))
            {
                // Split at @ to get command and username
                string[] parts = fullCommand.Split('@');
                string botcommand = parts[0];
                string targetUsername = parts[1];

                // Only process if the command is for this bot or in a private chat
                if (targetUsername.Equals(_botUsername, StringComparison.OrdinalIgnoreCase) ||
                    msg.Chat.Type == ChatType.Private)
                {
                    await HandleCommand(botcommand, CurrentBotChat);
                }
            }
            else
            {
                await HandleCommand(fullCommand, CurrentBotChat);
            }
        }
    }

    public async Task HandleCommand(string command, Chat chat)
    {
        await Logger.WriteToLogFile($"Processing command: '{command}'", "BotCommands");

        if (CommandRegistry.Commands.TryGetValue(command, out var handler))
        {
            await handler(this, chat);
        }
        else
        {
            await Logger.WriteToLogFile($"Unknown command: '{command}'", "BotCommands");
        }
    }

    // Send welcome message with an inline button for help.
    // ارسال پیام خوش‌آمدگویی با یک دکمه داخلی برای راهنمایی.
    public async Task StartCommand(Chat chat)
    {
        await _botClient.SendMessage(
            chatId: chat,
            parseMode: ParseMode.Html,
            text: "Hello and welcome to this bot! To see more information do /help.",
            replyMarkup: new InlineKeyboardMarkup(
                new[] {
                    new[] { InlineKeyboardButton.WithCallbackData("Show Help", "/help") }
                }
            )
        );
    }

    // Provide help information about available commands and functionality.
    // ارائه اطلاعات راهنما درباره دستورات و قابلیت‌های موجود.
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
    // ارائه قابلیت اکو، یعنی فقط همان پیام‌هایی را که دریافت می‌کند به شما بازگرداند.
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
    // این متد جدید را برای مدیریت درخواست‌های تغییر وضعیت اضافه کنید
    public async Task HandleEchoToggle(Chat chat)
    {
        _echoStates[chat.Id] = !_echoStates.GetValueOrDefault(chat.Id, false);
        await EchoCommand(chat);
    }

    // Display a list of all tracked users
    // نمایش لیستی از تمام کاربران ردیابی شده
    public async Task ListUsersCommand(Chat chat)
    {
        var users = await BotMain.GetAllUsers();

        if (users.Count == 0)
        {
            await _botClient.SendMessage(
                chatId: chat.Id,
                text: "No users have been tracked yet."
            );
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("**Tracked Users:**");
        sb.AppendLine();

        // Limit to 50 users to avoid message length issues
        // محدود به ۵۰ کاربر برای جلوگیری از مشکلات طول پیام
        int displayCount = Math.Min(users.Count, 50);

        for (int i = 0; i < displayCount; i++)
        {
            var (id, username, firstName, lastName) = users[i];
            string displayName = !string.IsNullOrEmpty(firstName)
                ? $"{firstName} {lastName ?? ""}".Trim()
                : username ?? "Unknown";

            sb.AppendLine($"{i + 1}. {displayName} (ID: {id})");
        }

        if (users.Count > displayCount)
        {
            sb.AppendLine();
            sb.AppendLine($"...and {users.Count - displayCount} more users.");
        }

        await _botClient.SendMessage(
            chatId: chat.Id,
            text: sb.ToString(),
            parseMode: ParseMode.Markdown
        );
    }

    // Display a list of all tracked chats
    // نمایش لیستی از تمام چت‌های ردیابی شده
    public async Task ListChatsCommand(Chat chat)
    {
        var chats = await BotMain.GetAllChats();

        if (chats.Count == 0)
        {
            await _botClient.SendMessage(
                chatId: chat.Id,
                text: "No chats have been tracked yet."
            );
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("**Tracked Chats:**");
        sb.AppendLine();

        // Group chats by type
        // گروه‌بندی چت‌ها بر اساس نوع
        var chatsByType = chats.GroupBy(c => c.Type);

        foreach (var group in chatsByType)
        {
            sb.AppendLine($"**{group.Key}s:**");

            // Limit to 15 chats per type to avoid message length issues
            // محدود به ۱۵ چت برای هر نوع برای جلوگیری از مشکلات طول پیام
            int displayCount = Math.Min(group.Count(), 15);
            int i = 0;

            foreach (var (id, title, _) in group.Take(displayCount))
            {
                string displayName = !string.IsNullOrEmpty(title) ? title : $"{group.Key} {id}";
                sb.AppendLine($"{++i}. {displayName} (ID: {id})");
            }

            if (group.Count() > displayCount)
            {
                sb.AppendLine($"...and {group.Count() - displayCount} more {group.Key}s.");
            }

            sb.AppendLine();
        }

        await _botClient.SendMessage(
            chatId: chat.Id,
            text: sb.ToString(),
            parseMode: ParseMode.Markdown
        );
    }

    // Display statistics about tracked data
    // نمایش آمار در مورد داده‌های ردیابی شده
    public async Task StatsCommand(Chat chat)
    {
        var users = await BotMain.GetAllUsers();
        var chats = await BotMain.GetAllChats();

        var chatTypes = chats
            .GroupBy(c => c.Type)
            .Select(g => (Type: g.Key, Count: g.Count()))
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("**Bot Statistics:**");
        sb.AppendLine();
        sb.AppendLine($"Total tracked users: **{users.Count}**");
        sb.AppendLine($"Total tracked chats: **{chats.Count}**");
        sb.AppendLine();

        // Chat type breakdown
        // تفکیک انواع چت
        sb.AppendLine("**Chat types:**");
        foreach (var (type, count) in chatTypes)
        {
            sb.AppendLine($"- {type}s: **{count}**");
        }

        // Bot version info
        // اطلاعات نسخه ربات
        sb.AppendLine();
        sb.AppendLine("**Bot Information:**");
        sb.AppendLine("- Version: 1.0.0");
        sb.AppendLine("- Database: SQLite");
        sb.AppendLine("- Language: C#");

        await _botClient.SendMessage(
            chatId: chat.Id,
            text: sb.ToString(),
            parseMode: ParseMode.Markdown
        );
    }

    public static async Task HandleInlineButtonPress(
        BotCommands botCommands,
        string callbackData,
        Message msg
    )
    {
        // Track user and chat for inline button presses too
        // پیگیری کاربر و چت برای فشردن دکمه‌های درون‌خطی نیز
        if (msg.From != null)
        {
            await DatabaseManager.TrackUser(msg.From);
        }

        if (msg.Chat != null)
        {
            await DatabaseManager.TrackChat(msg.Chat);
        }

        Chat CurrentBotChat = msg.Chat;
        // Extract command from callback data
        string commandWithPrefix = callbackData.Split('@')[0]; // Remove any username part
        string commandstrip = commandWithPrefix.Substring(1); // Remove the leading '/'
        if (callbackData == "/echo/toggle")
        {
            await botCommands.HandleEchoToggle(msg.Chat);
            return;
        }

        switch (commandstrip)
        {
            // Respond when 'help' button is pressed.
            // پاسخ دادن وقتی دکمه 'راهنما' فشرده می‌شود.
            case "help":
                await botCommands.HandleCommand(commandstrip, CurrentBotChat);
                break;

            // Respond on 'start' button pressed
            // پاسخ دادن وقتی دکمه 'شروع' فشرده می‌شود.
            case "start":
                await botCommands.HandleCommand(commandstrip, CurrentBotChat);
                break;
            // Respond on 'echo' button pressed
            // پاسخ دادن وقتی دکمه 'اکو' فشرده می‌شود.
            case "echo":
                await botCommands.HandleCommand(commandstrip, CurrentBotChat);
                break;
            // Respond on 'users' button pressed 
            // پاسخ دادن وقتی دکمه 'کاربران' فشرده می‌شود.
            case "users":
                await botCommands.HandleCommand(commandstrip, CurrentBotChat);
                break;
            // Respond on 'chats' button pressed
            // پاسخ دادن وقتی دکمه 'چت‌ها' فشرده می‌شود.
            case "chats":
                await botCommands.HandleCommand(commandstrip, CurrentBotChat);
                break;
            // Respond on 'stats' button pressed
            // پاسخ دادن وقتی دکمه 'آمار' فشرده می‌شود.
            case "stats":
                await botCommands.HandleCommand(commandstrip, CurrentBotChat);
                break;

            default:
                break;
                // Add more inline button actions as needed here.
                // در صورت نیاز، اقدامات دکمه‌های داخلی بیشتری را در اینجا اضافه کنید.
        }
    }
}
