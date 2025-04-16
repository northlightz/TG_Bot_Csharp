using Telegram.Bot.Types;

namespace Telegramski_Botski;

public static class CommandRegistry
{
    // Dictionary of commands and their handlers
    // دیکشنری دستورات و اجراکننده‌های آن‌ها
    public static readonly Dictionary<string, Func<BotCommands, Chat, Task>> Commands = new()
    {
        { "start", (bc, chat) => bc.StartCommand(chat) },
        { "help", (bc, chat) => bc.HelpCommand(chat) },
        { "echo", (bc, chat) => bc.EchoCommand(chat) },
        { "language", (bc, chat) => bc.LanguageCommand(chat) },
        { "users", (bc, chat) => bc.ListUsersCommand(chat) },
        { "chats", (bc, chat) => bc.ListChatsCommand(chat) },
        { "stats", (bc, chat) => bc.StatsCommand(chat) },
        { "permissions", (bc, chat) => bc.PermissionsCommand(chat) },
    };

    // Gets help text for all commands
    // متن راهنما برای تمام دستورات را دریافت می‌کند
    public static string GetHelpText() =>
        string.Join("\n", Commands.Keys.Select(c => $"/{c} - {GetCommandDescription(c)}"));

    // Gets description for a specific command
    // توضیحات برای یک دستور خاص را دریافت می‌کند
    private static string GetCommandDescription(string command) =>
        command switch
        {
            "start" => "Show welcome message",
            "help" => "Display help information",
            "echo" => "Toggle message echoing",
            "language" => "Toggle between English and Persian",
            "users" => "List all tracked users",
            "chats" => "List all tracked chats",
            "stats" => "Show bot statistics",
            "permissions" => "View and manage user permissions",
            _ => "No description available",
        };
}
