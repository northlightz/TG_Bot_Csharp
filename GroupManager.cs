using System.Text.Json;
using Telegram.Bot.Types;

namespace Telegramski_Botski;

public class GroupInfo
{
    public long ChatId { get; set; }
    public string Title { get; set; }
    public ChatType Type { get; set; }
    public DateTime JoinDate { get; set; }
}

public static class GroupManager
{
    private static readonly string GroupsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "tg_dev_files",
        "groups.json"
    );

    private static List<GroupInfo> _groups = new();

    public static async Task Initialize()
    {
        if (File.Exists(GroupsFilePath))
        {
            string json = await File.ReadAllTextAsync(GroupsFilePath);
            _groups = JsonSerializer.Deserialize<List<GroupInfo>>(json) ?? new List<GroupInfo>();
        }
        else
        {
            Directory.CreateDirectory(Path.GetDirectoryName(GroupsFilePath)!);
            await SaveGroups();
        }
    }

    public static async Task AddGroup(Chat chat)
    {
        if (chat.Type == ChatType.Private) return;

        var groupInfo = new GroupInfo
        {
            ChatId = chat.Id,
            Title = chat.Title ?? "Unknown",
            Type = chat.Type,
            JoinDate = DateTime.UtcNow
        };

        if (!_groups.Any(g => g.ChatId == chat.Id))
        {
            _groups.Add(groupInfo);
            await SaveGroups();
            await Logger.WriteToLogFile($"Bot joined new {chat.Type}: {chat.Title}", "GroupManager");
        }
    }

    public static async Task RemoveGroup(long chatId)
    {
        var group = _groups.FirstOrDefault(g => g.ChatId == chatId);
        if (group != null)
        {
            _groups.Remove(group);
            await SaveGroups();
            await Logger.WriteToLogFile($"Bot left group: {group.Title}", "GroupManager");
        }
    }

    public static List<GroupInfo> GetAllGroups() => _groups;

    private static async Task SaveGroups()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(_groups, options);
        await File.WriteAllTextAsync(GroupsFilePath, json);
    }
} 