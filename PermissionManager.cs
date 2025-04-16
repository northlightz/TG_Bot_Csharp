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
        sb.AppendLine(await CommandTexts.GetLocalizedCommandText("permissions_management", chatId));
        sb.AppendLine();
        
        if (!canPromote && !canRestrict)
        {
            sb.AppendLine(await CommandTexts.GetLocalizedCommandText("permissions_no_access", chatId));
            sb.AppendLine(string.Format(await CommandTexts.GetLocalizedCommandText("permissions_bot_status", chatId), botStatus));
            sb.AppendLine();
            sb.AppendLine(await CommandTexts.GetLocalizedCommandText("permissions_need_admin", chatId));
            
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
            sb.AppendLine("✅ " + await CommandTexts.GetLocalizedCommandText("permissions_can_promote", chatId));
        else
            sb.AppendLine("❌ " + await CommandTexts.GetLocalizedCommandText("permissions_can_promote", chatId));
            
        if (canRestrict)
            sb.AppendLine("✅ " + await CommandTexts.GetLocalizedCommandText("permissions_can_restrict", chatId));
        else
            sb.AppendLine("❌ " + await CommandTexts.GetLocalizedCommandText("permissions_can_restrict", chatId));
        
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
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(await CommandTexts.GetLocalizedCommandText("permissions_back_chats", chatId), "/permissions") });
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
            string errorText = await CommandTexts.GetLocalizedCommandText("permissions_user_not_found", chatId);
            
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
        sb.AppendLine(string.Format(await CommandTexts.GetLocalizedCommandText("permissions_for_user", chatId), displayName));
        sb.AppendLine();
        
        sb.AppendLine(string.Format(await CommandTexts.GetLocalizedCommandText("permissions_admin", chatId), (permissions.IsAdmin ? "✅" : "❌")));
        
        if (permissions.CanPostMessages.HasValue)
            sb.AppendLine(string.Format(await CommandTexts.GetLocalizedCommandText("permissions_can_post", chatId), (permissions.CanPostMessages.Value ? "✅" : "❌")));
            
        if (permissions.CanEditMessages.HasValue)
            sb.AppendLine(string.Format(await CommandTexts.GetLocalizedCommandText("permissions_can_edit", chatId), (permissions.CanEditMessages.Value ? "✅" : "❌")));
            
        if (permissions.CanDeleteMessages.HasValue)
            sb.AppendLine(string.Format(await CommandTexts.GetLocalizedCommandText("permissions_can_delete", chatId), (permissions.CanDeleteMessages.Value ? "✅" : "❌")));
            
        if (permissions.CanRestrictMembers.HasValue)
            sb.AppendLine(string.Format(await CommandTexts.GetLocalizedCommandText("permissions_can_restrict", chatId), (permissions.CanRestrictMembers.Value ? "✅" : "❌")));
            
        if (permissions.CanPromoteMembers.HasValue)
            sb.AppendLine(string.Format(await CommandTexts.GetLocalizedCommandText("permissions_can_promote", chatId), (permissions.CanPromoteMembers.Value ? "✅" : "❌")));
            
        if (permissions.CanChangeInfo.HasValue)
            sb.AppendLine(string.Format(await CommandTexts.GetLocalizedCommandText("permissions_can_change_info", chatId), (permissions.CanChangeInfo.Value ? "✅" : "❌")));
            
        if (permissions.CanInviteUsers.HasValue)
            sb.AppendLine(string.Format(await CommandTexts.GetLocalizedCommandText("permissions_can_invite", chatId), (permissions.CanInviteUsers.Value ? "✅" : "❌")));
            
        if (permissions.CanPinMessages.HasValue)
            sb.AppendLine(string.Format(await CommandTexts.GetLocalizedCommandText("permissions_can_pin", chatId), (permissions.CanPinMessages.Value ? "✅" : "❌")));
        
        var (canPromote, canRestrict, _) = await BotMain.CheckBotPermissions(targetChatId);
        
        var buttons = new List<InlineKeyboardButton[]>();
        
        if (permissions.IsAdmin)
        {
            if (canPromote)
            {
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(await CommandTexts.GetLocalizedCommandText("permissions_demote", chatId), $"/permissions/demote/{targetChatId}/{userId}") });
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(await CommandTexts.GetLocalizedCommandText("permissions_edit_user", chatId), $"/permissions/edit/admin/{targetChatId}/{userId}") });
            }
        }
        else
        {
            if (canPromote)
            {
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(await CommandTexts.GetLocalizedCommandText("permissions_promote", chatId), $"/permissions/promote/{targetChatId}/{userId}") });
            }
            
            if (canRestrict)
            {
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(await CommandTexts.GetLocalizedCommandText("permissions_edit_user", chatId), $"/permissions/edit/user/{targetChatId}/{userId}") });
            }
        }
        
        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(await CommandTexts.GetLocalizedCommandText("permissions_back_users", chatId), $"/permissions/chat/{targetChatId}") });
        
        if (chatId != targetChatId)
        {
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(await CommandTexts.GetLocalizedCommandText("permissions_back_chats", chatId), "/permissions") });
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