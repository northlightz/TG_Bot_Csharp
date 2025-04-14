using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Text;

namespace Telegramski_Botski;

public class UserManager
{
    private readonly TelegramBotClient _botClient;

    public UserManager(TelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public async Task ListUsers(Chat chat)
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

    public async Task ListChats(Chat chat)
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

        var chatsByType = chats.GroupBy(c => c.Type);

        foreach (var group in chatsByType)
        {
            sb.AppendLine($"**{group.Key}s:**");

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
} 