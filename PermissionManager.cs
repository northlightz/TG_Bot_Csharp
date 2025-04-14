using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using System.Text;
using Telegram.Bot.Types.Enums;

namespace Telegramski_Botski;

public class PermissionManager
{
    private readonly TelegramBotClient _botClient;
    private static readonly Dictionary<string, bool> _adminPermissions = new();
    private static readonly Dictionary<string, bool> _userPermissions = new();
    private static readonly Dictionary<string, int> _activeMessages = new();

    public PermissionManager(TelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public async Task ShowChatPermissions(long chatId, long targetChatId, int? messageId = null)
    {
        string messageKey = $"permissions:{chatId}:{targetChatId}";
        
        var (canPromote, canRestrict, botStatus) = await BotMain.CheckBotPermissions(targetChatId);
        
        var sb = new StringBuilder();
        sb.AppendLine($"**Permissions Management**");
        sb.AppendLine();
        
        if (!canPromote && !canRestrict)
        {
            sb.AppendLine($"⚠️ I don't have permission to manage users in this chat.");
            sb.AppendLine($"My status: {botStatus}");
            sb.AppendLine();
            sb.AppendLine("To manage permissions, I need to be an administrator with appropriate permissions.");
            
            if (messageId.HasValue)
            {
                await _botClient.EditMessageText(
                    chatId: chatId,
                    messageId: messageId.Value,
                    text: sb.ToString(),
                    parseMode: ParseMode.Markdown
                );
            }
            else
            {
                var message = await _botClient.SendMessage(
                    chatId: chatId,
                    text: sb.ToString(),
                    parseMode: ParseMode.Markdown
                );
                _activeMessages[messageKey] = message.MessageId;
            }
            return;
        }

        var users = await DatabaseManager.GetUsersWithPermissionsInChat(targetChatId);
        
        if (canPromote)
            sb.AppendLine("✅ I can promote/demote users");
        else
            sb.AppendLine("❌ I cannot promote/demote users");
            
        if (canRestrict)
            sb.AppendLine("✅ I can restrict users");
        else
            sb.AppendLine("❌ I cannot restrict users");
        
        sb.AppendLine();
        sb.AppendLine("**Users in this chat:**");
        
        if (users.Count == 0)
        {
            sb.AppendLine("No tracked users with permissions in this chat.");
        }
        
        var buttons = new List<InlineKeyboardButton[]>();
        
        if (users.Count > 0)
        {
            foreach (var (userId, username, firstName, isAdmin) in users.Take(10))
            {
                string displayName = !string.IsNullOrEmpty(firstName) ? firstName : username ?? $"User {userId}";
                string statusEmoji = isAdmin ? "👑 " : "👤 ";
                
                buttons.Add(new[] 
                { 
                    InlineKeyboardButton.WithCallbackData(
                        $"{statusEmoji}{displayName}", 
                        $"/permissions/user/{targetChatId}/{userId}"
                    ) 
                });
            }
        }
        
        if (chatId != targetChatId)
        {
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("Back to chat list", "/permissions") });
        }

        if (messageId.HasValue)
        {
            await _botClient.EditMessageText(
                chatId: chatId,
                messageId: messageId.Value,
                text: sb.ToString(),
                parseMode: ParseMode.Markdown,
                replyMarkup: new InlineKeyboardMarkup(buttons)
            );
        }
        else
        {
            var message = await _botClient.SendMessage(
                chatId: chatId,
                text: sb.ToString(),
                parseMode: ParseMode.Markdown,
                replyMarkup: new InlineKeyboardMarkup(buttons)
            );
            _activeMessages[messageKey] = message.MessageId;
        }
    }

    public async Task ShowUserPermissions(long chatId, long targetChatId, long userId, int? messageId = null)
    {
        string messageKey = $"permissions:{chatId}:{targetChatId}:{userId}";
        
        var userInfo = await BotMain.GetAllUsers();
        var user = userInfo.FirstOrDefault(u => u.Id == userId);
        
        if (user == default)
        {
            string errorText = "User not found.";
            
            if (messageId.HasValue)
            {
                await _botClient.EditMessageText(
                    chatId: chatId,
                    messageId: messageId.Value,
                    text: errorText
                );
            }
            else
            {
                var message = await _botClient.SendMessage(
                    chatId: chatId,
                    text: errorText
                );
                _activeMessages[messageKey] = message.MessageId;
            }
            return;
        }
        
        string displayName = !string.IsNullOrEmpty(user.FirstName) 
            ? $"{user.FirstName} {user.LastName ?? ""}".Trim() 
            : user.Username ?? $"User {userId}";
        
        var permissions = await DatabaseManager.GetUserPermissions(userId, targetChatId);
        
        var sb = new StringBuilder();
        sb.AppendLine($"**Permissions for {displayName}**");
        sb.AppendLine();
        
        sb.AppendLine($"Admin: {(permissions.IsAdmin ? "✅" : "❌")}");
        
        if (permissions.CanPostMessages.HasValue)
            sb.AppendLine($"Can post messages: {(permissions.CanPostMessages.Value ? "✅" : "❌")}");
            
        if (permissions.CanEditMessages.HasValue)
            sb.AppendLine($"Can edit messages: {(permissions.CanEditMessages.Value ? "✅" : "❌")}");
            
        if (permissions.CanDeleteMessages.HasValue)
            sb.AppendLine($"Can delete messages: {(permissions.CanDeleteMessages.Value ? "✅" : "❌")}");
            
        if (permissions.CanRestrictMembers.HasValue)
            sb.AppendLine($"Can restrict members: {(permissions.CanRestrictMembers.Value ? "✅" : "❌")}");
            
        if (permissions.CanPromoteMembers.HasValue)
            sb.AppendLine($"Can promote members: {(permissions.CanPromoteMembers.Value ? "✅" : "❌")}");
            
        if (permissions.CanChangeInfo.HasValue)
            sb.AppendLine($"Can change info: {(permissions.CanChangeInfo.Value ? "✅" : "❌")}");
            
        if (permissions.CanInviteUsers.HasValue)
            sb.AppendLine($"Can invite users: {(permissions.CanInviteUsers.Value ? "✅" : "❌")}");
            
        if (permissions.CanPinMessages.HasValue)
            sb.AppendLine($"Can pin messages: {(permissions.CanPinMessages.Value ? "✅" : "❌")}");
        
        var (canPromote, canRestrict, _) = await BotMain.CheckBotPermissions(targetChatId);
        
        var buttons = new List<InlineKeyboardButton[]>();
        
        if (permissions.IsAdmin)
        {
            if (canPromote)
            {
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("Demote to Regular User", $"/permissions/demote/{targetChatId}/{userId}") });
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("Edit Admin Permissions", $"/permissions/edit/admin/{targetChatId}/{userId}") });
            }
        }
        else
        {
            if (canPromote)
            {
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("Promote to Admin", $"/permissions/promote/{targetChatId}/{userId}") });
            }
            
            if (canRestrict)
            {
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("Edit User Permissions", $"/permissions/edit/user/{targetChatId}/{userId}") });
            }
        }
        
        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("Back to User List", $"/permissions/chat/{targetChatId}") });
        
        if (chatId != targetChatId)
        {
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("Back to Chat List", "/permissions") });
        }
        
        if (messageId.HasValue)
        {
            await _botClient.EditMessageText(
                chatId: chatId,
                messageId: messageId.Value,
                text: sb.ToString(),
                parseMode: ParseMode.Markdown,
                replyMarkup: new InlineKeyboardMarkup(buttons)
            );
        }
        else
        {
            var message = await _botClient.SendMessage(
                chatId: chatId,
                text: sb.ToString(),
                parseMode: ParseMode.Markdown,
                replyMarkup: new InlineKeyboardMarkup(buttons)
            );
            _activeMessages[messageKey] = message.MessageId;
        }
    }

    private string GetToggleText(string permission, string key, string label, bool isUser = false)
    {
        var permissionKey = $"{key}:{permission}";
        var dict = isUser ? _userPermissions : _adminPermissions;
        
        if (dict.TryGetValue(permissionKey, out bool enabled))
        {
            return $"{label}: {(enabled ? "✅" : "❌")}";
        }
        else
        {
            dict[permissionKey] = false;
            return $"{label}: ❌";
        }
    }
    
    private static void TogglePermission(string key, string permission, bool isUser = false)
    {
        var permissionKey = $"{key}:{permission}";
        var dict = isUser ? _userPermissions : _adminPermissions;
        
        if (dict.ContainsKey(permissionKey))
        {
            dict[permissionKey] = !dict[permissionKey];
        }
        else
        {
            dict[permissionKey] = true;
        }
    }
    
    private static bool GetPermissionState(string key, string permission, bool isUser = false)
    {
        var permissionKey = $"{key}:{permission}";
        var dict = isUser ? _userPermissions : _adminPermissions;
        
        return dict.TryGetValue(permissionKey, out bool enabled) && enabled;
    }
    
    private static void ClearPermissions(string key)
    {
        var adminKeys = _adminPermissions.Keys
            .Where(k => k.StartsWith(key))
            .ToList();
            
        var userKeys = _userPermissions.Keys
            .Where(k => k.StartsWith(key))
            .ToList();
            
        foreach (var k in adminKeys)
        {
            _adminPermissions.Remove(k);
        }
        
        foreach (var k in userKeys)
        {
            _userPermissions.Remove(k);
        }
    }
} 