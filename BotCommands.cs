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
    
    // Dictionaries to store permission state during editing
    // دیکشنری‌ها برای ذخیره وضعیت مجوزها در طول ویرایش
    private static readonly Dictionary<string, bool> _adminPermissions = new();
    private static readonly Dictionary<string, bool> _userPermissions = new();
    
    // Store message IDs for updating UI instead of creating new messages
    // ذخیره شناسه‌های پیام برای به‌روزرسانی رابط کاربری به جای ایجاد پیام‌های جدید
    private static readonly Dictionary<string, int> _activeMessages = new();

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

    // Permissions management command
    // دستور مدیریت مجوزها
    public async Task PermissionsCommand(Chat chat, int? messageId = null)
    {
        // Check if we're in a private chat
        if (chat.Type == ChatType.Private)
        {
            // Show list of chats where the bot is present
            var chats = await BotMain.GetAllChats();
            var groupChats = chats.Where(c => c.Type == "Group" || c.Type == "Supergroup").ToList();

            if (groupChats.Count == 0)
            {
                string text = "I'm not in any groups yet! Add me to a group to manage permissions.";
                
                if (messageId.HasValue)
                {
                    await _botClient.EditMessageText(
                        chatId: chat.Id,
                        messageId: messageId.Value,
                        text: text
                    );
                }
                else
                {
                    await _botClient.SendMessage(
                        chatId: chat.Id,
                        text: text
                    );
                }
                return;
            }

            var buttons = new List<InlineKeyboardButton[]>();
            foreach (var (id, title, _) in groupChats.Take(10))
            {
                var chatName = !string.IsNullOrEmpty(title) ? title : $"Chat {id}";
                buttons.Add(new[] 
                { 
                    InlineKeyboardButton.WithCallbackData(
                        chatName, 
                        $"/permissions/chat/{id}"
                    ) 
                });
            }

            string msgText = "Select a chat to manage permissions:";
            
            if (messageId.HasValue)
            {
                await _botClient.EditMessageText(
                    chatId: chat.Id,
                    messageId: messageId.Value,
                    text: msgText,
                    replyMarkup: new InlineKeyboardMarkup(buttons)
                );
            }
            else
            {
                var message = await _botClient.SendMessage(
                    chatId: chat.Id,
                    text: msgText,
                    replyMarkup: new InlineKeyboardMarkup(buttons)
                );
                
                // Store the message ID for future updates
                _activeMessages[$"permissions:{chat.Id}"] = message.MessageId;
            }
        }
        else
        {
            // Show permissions for the current chat
            await ShowChatPermissions(chat.Id, chat.Id, messageId);
        }
    }

    // Show permissions for a specific chat
    // نمایش مجوزها برای یک چت خاص
    public async Task ShowChatPermissions(long chatId, long targetChatId, int? messageId = null)
    {
        string messageKey = $"permissions:{chatId}:{targetChatId}";
        
        // Check if the bot has permission to change permissions
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

        // Get users with permissions in this chat
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
        
        // Create buttons for each user
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
        
        // Add a "Back" button if we're in a private chat
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

    // Show a user's permissions and provide options to modify them
    // نمایش مجوزهای یک کاربر و ارائه گزینه‌هایی برای تغییر آنها
    public async Task ShowUserPermissions(long chatId, long targetChatId, long userId, int? messageId = null)
    {
        string messageKey = $"permissions:{chatId}:{targetChatId}:{userId}";
        
        // Get the user's info
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
        
        // Get the user's current permissions
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
        
        // Check bot permissions to see what actions are available
        var (canPromote, canRestrict, _) = await BotMain.CheckBotPermissions(targetChatId);
        
        var buttons = new List<InlineKeyboardButton[]>();
        
        if (permissions.IsAdmin)
        {
            // Admin user options
            if (canPromote)
            {
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("Demote to Regular User", $"/permissions/demote/{targetChatId}/{userId}") });
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("Edit Admin Permissions", $"/permissions/edit/admin/{targetChatId}/{userId}") });
            }
        }
        else
        {
            // Regular user options
            if (canPromote)
            {
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("Promote to Admin", $"/permissions/promote/{targetChatId}/{userId}") });
            }
            
            if (canRestrict)
            {
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("Edit User Permissions", $"/permissions/edit/user/{targetChatId}/{userId}") });
            }
        }
        
        // Add back buttons
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

    // Handle promotion of a user to admin
    // مدیریت ارتقای یک کاربر به مدیر
    public async Task PromoteUser(long chatId, long targetChatId, long userId, int? messageId = null)
    {
        string messageKey = $"permissions:{chatId}:{targetChatId}:{userId}:promote";
        
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
        
        string stateKey = $"{targetChatId}:{userId}";
        
        // Create permission selection buttons
        var buttons = new List<InlineKeyboardButton[]>
        {
            new[] { 
                InlineKeyboardButton.WithCallbackData(
                    GetToggleText("changeInfo", stateKey, "Change Info"), 
                    $"/permissions/promote/{targetChatId}/{userId}/changeInfo"
                ),
                InlineKeyboardButton.WithCallbackData(
                    GetToggleText("postMessages", stateKey, "Post Messages"), 
                    $"/permissions/promote/{targetChatId}/{userId}/postMessages"
                )
            },
            new[] { 
                InlineKeyboardButton.WithCallbackData(
                    GetToggleText("editMessages", stateKey, "Edit Messages"), 
                    $"/permissions/promote/{targetChatId}/{userId}/editMessages"
                ),
                InlineKeyboardButton.WithCallbackData(
                    GetToggleText("deleteMessages", stateKey, "Delete Messages"), 
                    $"/permissions/promote/{targetChatId}/{userId}/deleteMessages"
                )
            },
            new[] { 
                InlineKeyboardButton.WithCallbackData(
                    GetToggleText("inviteUsers", stateKey, "Invite Users"), 
                    $"/permissions/promote/{targetChatId}/{userId}/inviteUsers"
                ),
                InlineKeyboardButton.WithCallbackData(
                    GetToggleText("restrictMembers", stateKey, "Restrict Users"), 
                    $"/permissions/promote/{targetChatId}/{userId}/restrictMembers"
                )
            },
            new[] { 
                InlineKeyboardButton.WithCallbackData(
                    GetToggleText("pinMessages", stateKey, "Pin Messages"), 
                    $"/permissions/promote/{targetChatId}/{userId}/pinMessages"
                ),
                InlineKeyboardButton.WithCallbackData(
                    GetToggleText("promoteMembers", stateKey, "Promote Users"), 
                    $"/permissions/promote/{targetChatId}/{userId}/promoteMembers"
                )
            },
            new[] { 
                InlineKeyboardButton.WithCallbackData("✅ Apply Promotion", $"/permissions/promote/{targetChatId}/{userId}/apply")
            },
            new[] { 
                InlineKeyboardButton.WithCallbackData("Back", $"/permissions/user/{targetChatId}/{userId}")
            }
        };
        
        string text = $"Select admin permissions for {displayName}:\n\n" +
                  "Click on permissions to toggle them, then press Apply when done.";
        
        if (messageId.HasValue)
        {
            await _botClient.EditMessageText(
                chatId: chatId,
                messageId: messageId.Value,
                text: text,
                replyMarkup: new InlineKeyboardMarkup(buttons)
            );
        }
        else
        {
            var message = await _botClient.SendMessage(
                chatId: chatId,
                text: text,
                replyMarkup: new InlineKeyboardMarkup(buttons)
            );
            _activeMessages[messageKey] = message.MessageId;
        }
    }

    // Handle restriction of a regular user
    // مدیریت محدودیت یک کاربر عادی
    public async Task RestrictUser(long chatId, long targetChatId, long userId, int? messageId = null)
    {
        string messageKey = $"permissions:{chatId}:{targetChatId}:{userId}:restrict";
        
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
        
        string stateKey = $"{targetChatId}:{userId}";
        
        // Create permission selection buttons
        var buttons = new List<InlineKeyboardButton[]>
        {
            new[] { 
                InlineKeyboardButton.WithCallbackData(
                    GetToggleText("sendMessages", stateKey, "Send Messages", isUser: true), 
                    $"/permissions/restrict/{targetChatId}/{userId}/sendMessages"
                ),
                InlineKeyboardButton.WithCallbackData(
                    GetToggleText("inviteUsers", stateKey, "Invite Users", isUser: true), 
                    $"/permissions/restrict/{targetChatId}/{userId}/inviteUsers"
                )
            },
            new[] { 
                InlineKeyboardButton.WithCallbackData(
                    GetToggleText("pinMessages", stateKey, "Pin Messages", isUser: true), 
                    $"/permissions/restrict/{targetChatId}/{userId}/pinMessages"
                ),
                InlineKeyboardButton.WithCallbackData(
                    GetToggleText("changeInfo", stateKey, "Change Info", isUser: true), 
                    $"/permissions/restrict/{targetChatId}/{userId}/changeInfo"
                )
            },
            new[] { 
                InlineKeyboardButton.WithCallbackData("✅ Apply Restrictions", $"/permissions/restrict/{targetChatId}/{userId}/apply")
            },
            new[] { 
                InlineKeyboardButton.WithCallbackData("Back", $"/permissions/user/{targetChatId}/{userId}")
            }
        };
        
        string text = $"Select permissions for {displayName}:\n\n" +
                  "Click on permissions to toggle them, then press Apply when done.";
        
        if (messageId.HasValue)
        {
            await _botClient.EditMessageText(
                chatId: chatId,
                messageId: messageId.Value,
                text: text,
                replyMarkup: new InlineKeyboardMarkup(buttons)
            );
        }
        else
        {
            var message = await _botClient.SendMessage(
                chatId: chatId,
                text: text,
                replyMarkup: new InlineKeyboardMarkup(buttons)
            );
            _activeMessages[messageKey] = message.MessageId;
        }
    }

    // Handle the demotion of an admin to regular user
    // مدیریت تنزل یک مدیر به کاربر عادی
    public async Task DemoteUser(long chatId, long targetChatId, long userId, int? messageId = null)
    {
        string messageKey = $"permissions:{chatId}:{targetChatId}:{userId}:demote";
        
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
        
        var buttons = new List<InlineKeyboardButton[]>
        {
            new[] { 
                InlineKeyboardButton.WithCallbackData("✅ Confirm Demotion", $"/permissions/demote/{targetChatId}/{userId}/confirm"),
                InlineKeyboardButton.WithCallbackData("❌ Cancel", $"/permissions/user/{targetChatId}/{userId}")
            }
        };
        
        string text = $"Are you sure you want to demote {displayName} to a regular user?\n\n" +
                  "This will remove all their admin permissions.";
        
        if (messageId.HasValue)
        {
            await _botClient.EditMessageText(
                chatId: chatId,
                messageId: messageId.Value,
                text: text,
                replyMarkup: new InlineKeyboardMarkup(buttons)
            );
        }
        else
        {
            var message = await _botClient.SendMessage(
                chatId: chatId,
                text: text,
                replyMarkup: new InlineKeyboardMarkup(buttons)
            );
            _activeMessages[messageKey] = message.MessageId;
        }
    }

    // Helper method to get the text for a toggle button
    // متد کمکی برای دریافت متن دکمه تغییر وضعیت
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
            // Default to false (off)
            dict[permissionKey] = false;
            return $"{label}: ❌";
        }
    }
    
    // Toggle a permission state
    // تغییر وضعیت یک مجوز
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
    
    // Get a permission state
    // دریافت وضعیت یک مجوز
    private static bool GetPermissionState(string key, string permission, bool isUser = false)
    {
        var permissionKey = $"{key}:{permission}";
        var dict = isUser ? _userPermissions : _adminPermissions;
        
        return dict.TryGetValue(permissionKey, out bool enabled) && enabled;
    }
    
    // Clear permissions for a user in a chat
    // پاک کردن مجوزهای یک کاربر در یک چت
    private static void ClearPermissions(string key)
    {
        // Remove all keys that start with this key
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

        await Logger.WriteToLogFile($"Received callback: {callbackData}", "HandleInlineButtonPress");
        
        Chat CurrentBotChat = msg.Chat;
        
        // Handle permissions-specific commands
        if (callbackData.StartsWith("/permissions/") || callbackData == "/permissions")
        {
            await HandlePermissionsCallback(botCommands, callbackData, msg);
            return;
        }
        
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
            // Respond on 'permissions' button pressed
            // پاسخ دادن وقتی دکمه 'مجوزها' فشرده می‌شود.
            case "permissions":
                await botCommands.PermissionsCommand(CurrentBotChat, msg.MessageId);
                break;

            default:
                await Logger.WriteToLogFile($"Unhandled callback command: {commandstrip}", "HandleInlineButtonPress");
                break;
                // Add more inline button actions as needed here.
                // در صورت نیاز، اقدامات دکمه‌های داخلی بیشتری را در اینجا اضافه کنید.
        }
    }

    // Handle permission-related callback queries
    // مدیریت کوئری‌های بازگشتی مربوط به مجوزها
    private static async Task HandlePermissionsCallback(BotCommands commands, string callbackData, Message msg)
    {
        await Logger.WriteToLogFile($"Processing permissions callback: '{callbackData}'", "BotCommands");
        
        var parts = callbackData.Split('/');
        
        if (parts.Length < 3)
        {
            // This handles the base "/permissions" command
            if (callbackData == "/permissions")
            {
                await Logger.WriteToLogFile("Handling base /permissions callback", "BotCommands");
                // For plain "/permissions" callback, show the permission selection screen
                await commands.PermissionsCommand(msg.Chat, msg.MessageId);
                return;
            }
            return;
        }
            
        string action = parts[2];
        int? messageId = msg.MessageId;
        
        await Logger.WriteToLogFile($"Permissions action: {action}", "BotCommands");
        
        switch (action)
        {
            case "chat":
                if (parts.Length >= 4 && long.TryParse(parts[3], out long chatId))
                {
                    await commands.ShowChatPermissions(msg.Chat.Id, chatId, messageId);
                }
                break;
                
            case "user":
                if (parts.Length >= 5 && long.TryParse(parts[3], out long userChatId) && long.TryParse(parts[4], out long userId))
                {
                    await commands.ShowUserPermissions(msg.Chat.Id, userChatId, userId, messageId);
                }
                break;
                
            case "promote":
                if (parts.Length >= 5 && long.TryParse(parts[3], out long promChatId) && long.TryParse(parts[4], out long promUserId))
                {
                    string stateKey = $"{promChatId}:{promUserId}";
                    
                    if (parts.Length >= 6)
                    {
                        // Handle specific permission toggle or apply promotion
                        string permAction = parts[5];
                        
                        if (permAction == "apply")
                        {
                            // Get permissions from stored state and apply
                            bool success = await BotMain.PromoteUser(
                                promChatId, 
                                promUserId,
                                GetPermissionState(stateKey, "changeInfo"),
                                GetPermissionState(stateKey, "postMessages"),
                                GetPermissionState(stateKey, "editMessages"),
                                GetPermissionState(stateKey, "deleteMessages"),
                                GetPermissionState(stateKey, "inviteUsers"),
                                GetPermissionState(stateKey, "restrictMembers"),
                                GetPermissionState(stateKey, "pinMessages"),
                                GetPermissionState(stateKey, "promoteMembers")
                            );
                            
                            // Clear permissions state after application
                            ClearPermissions(stateKey);
                            
                            string resultMessage = success 
                                ? "User was successfully promoted to admin." 
                                : "Failed to promote user. Make sure I have the right permissions.";
                                
                            await commands._botClient.EditMessageText(
                                chatId: msg.Chat.Id,
                                messageId: messageId.Value,
                                text: resultMessage
                            );
                            
                            // Small delay before showing user info
                            await Task.Delay(1000);
                            
                            // Go back to user info
                            await commands.ShowUserPermissions(msg.Chat.Id, promChatId, promUserId, messageId);
                        }
                        else
                        {
                            // Toggle a specific permission and show the promote screen again
                            TogglePermission(stateKey, permAction);
                            await commands.PromoteUser(msg.Chat.Id, promChatId, promUserId, messageId);
                        }
                    }
                    else
                    {
                        // Show promotion screen
                        await commands.PromoteUser(msg.Chat.Id, promChatId, promUserId, messageId);
                    }
                }
                break;
                
            case "demote":
                if (parts.Length >= 5 && long.TryParse(parts[3], out long demChatId) && long.TryParse(parts[4], out long demUserId))
                {
                    if (parts.Length >= 6 && parts[5] == "confirm")
                    {
                        // Confirm demotion
                        bool success = await BotMain.DemoteUser(demChatId, demUserId);
                        
                        string resultMessage = success 
                            ? "User was successfully demoted to regular user." 
                            : "Failed to demote user. Make sure I have the right permissions.";
                            
                        await commands._botClient.EditMessageText(
                            chatId: msg.Chat.Id,
                            messageId: messageId.Value,
                            text: resultMessage
                        );
                        
                        // Small delay before showing user info
                        await Task.Delay(1000);
                        
                        // Go back to user info
                        await commands.ShowUserPermissions(msg.Chat.Id, demChatId, demUserId, messageId);
                    }
                    else
                    {
                        // Show demotion confirmation
                        await commands.DemoteUser(msg.Chat.Id, demChatId, demUserId, messageId);
                    }
                }
                break;
                
            case "restrict":
                if (parts.Length >= 5 && long.TryParse(parts[3], out long resChatId) && long.TryParse(parts[4], out long resUserId))
                {
                    string stateKey = $"{resChatId}:{resUserId}";
                    
                    if (parts.Length >= 6)
                    {
                        // Handle specific permission toggle or apply restrictions
                        string permAction = parts[5];
                        
                        if (permAction == "apply")
                        {
                            // Get permissions from stored state and apply
                            bool success = await BotMain.RestrictUser(
                                resChatId, 
                                resUserId,
                                GetPermissionState(stateKey, "sendMessages", true),
                                GetPermissionState(stateKey, "inviteUsers", true),
                                GetPermissionState(stateKey, "pinMessages", true),
                                GetPermissionState(stateKey, "changeInfo", true)
                            );
                            
                            // Clear permissions state after application
                            ClearPermissions(stateKey);
                            
                            string resultMessage = success 
                                ? "User restrictions were successfully updated." 
                                : "Failed to update user restrictions. Make sure I have the right permissions.";
                                
                            await commands._botClient.EditMessageText(
                                chatId: msg.Chat.Id,
                                messageId: messageId.Value,
                                text: resultMessage
                            );
                            
                            // Small delay before showing user info
                            await Task.Delay(1000);
                            
                            // Go back to user info
                            await commands.ShowUserPermissions(msg.Chat.Id, resChatId, resUserId, messageId);
                        }
                        else
                        {
                            // Toggle a specific permission and show the restrict screen again
                            TogglePermission(stateKey, permAction, true);
                            await commands.RestrictUser(msg.Chat.Id, resChatId, resUserId, messageId);
                        }
                    }
                    else
                    {
                        // Show restriction screen
                        await commands.RestrictUser(msg.Chat.Id, resChatId, resUserId, messageId);
                    }
                }
                break;
                
            case "edit":
                if (parts.Length >= 6 && 
                    long.TryParse(parts[4], out long editChatId) && 
                    long.TryParse(parts[5], out long editUserId))
                {
                    string editType = parts[3]; // "admin" or "user"
                    
                    if (editType == "admin")
                    {
                        // Edit admin permissions
                        await commands.PromoteUser(msg.Chat.Id, editChatId, editUserId, messageId);
                    }
                    else if (editType == "user")
                    {
                        // Edit regular user permissions
                        await commands.RestrictUser(msg.Chat.Id, editChatId, editUserId, messageId);
                    }
                }
                break;
        }
    }

    // Add a helper method to get message ID
    private static int? GetStoredMessageId(string key, int? fallbackId = null)
    {
        if (_activeMessages.TryGetValue(key, out int storedId))
        {
            return storedId;
        }
        
        return fallbackId;
    }
}
