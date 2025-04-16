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
    public static async Task<string> GetHelpText(long chatId)
    {
        var commandDescriptions = new List<string>();
        foreach (var command in Commands.Keys)
        {
            var description = await GetCommandDescription(command, chatId);
            commandDescriptions.Add($"/{command} - {description}");
        }
        return string.Join("\n", commandDescriptions);
    }

    // Gets description for a specific command
    // توضیحات برای یک دستور خاص را دریافت می‌کند
    private static async Task<string> GetCommandDescription(string command, long chatId)
    {
        var key = $"command_{command}";
        return await CommandTexts.GetLocalizedCommandText(key, chatId);
    }
}
