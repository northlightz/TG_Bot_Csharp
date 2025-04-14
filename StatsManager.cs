using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Text;

namespace Telegramski_Botski;

public class StatsManager
{
    private readonly TelegramBotClient _botClient;

    public StatsManager(TelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public async Task ShowStats(Chat chat)
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

        sb.AppendLine("**Chat types:**");
        foreach (var (type, count) in chatTypes)
        {
            sb.AppendLine($"- {type}s: **{count}**");
        }

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
} 