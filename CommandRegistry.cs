using Telegram.Bot.Types;

namespace Telegramski_Botski;

public static class CommandRegistry
{
    public static readonly Dictionary<string, Func<BotCommands, Chat, Task>> Commands = new()
    {
        { "start", (bc, chat) => bc.StartCommand(chat) },
        { "help", (bc, chat) => bc.HelpCommand(chat) },
        { "echo", (bc, chat) => bc.EchoCommand(chat) },
    };

    public static string GetHelpText() =>
        string.Join("\n", Commands.Keys.Select(c => $"/{c} - {GetCommandDescription(c)}"));

    private static string GetCommandDescription(string command) =>
        command switch
        {
            "start" => "Show welcome message",
            "help" => "Display help information",
            "echo" => "Toggle message echoing",
            _ => "No description available",
        };
}
