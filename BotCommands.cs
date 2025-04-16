using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegramski_Botski;

// Main class for handling bot commands
// کلاس اصلی برای مدیریت دستورات ربات
public class BotCommands
{
    private readonly TelegramBotClient _botClient;
    private readonly string _botUsername;
    private readonly EchoManager _echoManager;
    private readonly UserManager _userManager;
    private readonly StatsManager _statsManager;
    private readonly PermissionManager _permissionManager;
    private readonly LanguageManager _languageManager;
    private static readonly Dictionary<string, bool> _adminPermissions = new();
    private static readonly Dictionary<string, bool> _userPermissions = new();

    public BotCommands(TelegramBotClient botClient, string botUsername)
    {
        _botClient = botClient;
        _botUsername = botUsername;
        _echoManager = new EchoManager(botClient);
        _userManager = new UserManager(botClient);
        _statsManager = new StatsManager(botClient);
        _permissionManager = new PermissionManager(botClient);
        _languageManager = new LanguageManager(botClient);
    }

    // Main method to handle incoming messages and commands
    // متد اصلی برای مدیریت پیام‌های ورودی و دستورات
    public async Task HandleCommands(Message msg)
    {
        // Track user and chat information
        // پیگیری اطلاعات کاربر و چت
        if (msg.From != null)
        {
            await DatabaseManager.TrackUser(msg.From);
            await DatabaseManager.TrackChat(msg.Chat);
        }

        string MessageText = msg.Text;
        Chat CurrentBotChat = msg.Chat;

        await _echoManager.HandleEcho(msg);

        // Check if the message starts with a command prefix ('/') and handle accordingly
        // بررسی اینکه آیا پیام با پیشوند دستور ('/') شروع می‌شود و بر اساس آن اقدام کنید
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
                // تقسیم در @ برای دریافت دستور و نام کاربری
                string[] parts = fullCommand.Split('@');
                string botcommand = parts[0];
                string targetUsername = parts[1];

                // Only process if the command is for this bot or in a private chat
                // فقط در صورتی پردازش کنید که دستور برای این ربات باشد یا در چت خصوصی باشد
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

    // Handle individual commands
    // مدیریت دستورات فردی
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

    // Send welcome message with an inline button for help
    // ارسال پیام خوش‌آمدگویی با یک دکمه داخلی برای راهنمایی
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

    // Provide help information about available commands and functionality
    // ارائه اطلاعات راهنما درباره دستورات و قابلیت‌های موجود
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

    // Provide echo functionality, i.e. just send the same messages it gets back to you
    // ارائه قابلیت اکو، یعنی فقط همان پیام‌هایی را که دریافت می‌کند به شما بازگرداند
    public async Task EchoCommand(Chat chat)
    {
        await _echoManager.ShowEchoStatus(chat);
    }

    // Add this new method to handle toggle requests
    // این متد جدید را برای مدیریت درخواست‌های تغییر وضعیت اضافه کنید
    public async Task HandleEchoToggle(Chat chat)
    {
        await _echoManager.ToggleEcho(chat);
    }

    // Display a list of all tracked users
    // نمایش لیستی از تمام کاربران ردیابی شده
    public async Task ListUsersCommand(Chat chat)
    {
        await _userManager.ListUsers(chat);
    }

    // Display a list of all tracked chats
    // نمایش لیستی از تمام چت‌های ردیابی شده
    public async Task ListChatsCommand(Chat chat)
    {
        await _userManager.ListChats(chat);
    }

    // Display statistics about tracked data
    // نمایش آمار در مورد داده‌های ردیابی شده
    public async Task StatsCommand(Chat chat)
    {
        await _statsManager.ShowStats(chat);
    }

    // Permissions management command
    // دستور مدیریت مجوزها
    public async Task PermissionsCommand(Chat chat, int? messageId = null)
    {
        // Check if we're in a private chat
        // بررسی اینکه آیا در چت خصوصی هستیم
        if (chat.Type == ChatType.Private)
        {
            // Show list of chats where the bot is present
            // نمایش لیست چت‌هایی که ربات در آن حضور دارد
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
            }
        }
        else
        {
            // Show permissions for the current chat
            // نمایش مجوزها برای چت فعلی
            await _permissionManager.ShowChatPermissions(chat.Id, chat.Id, messageId);
        }
    }

    public async Task LanguageCommand(Chat chat)
    {
        await _languageManager.ToggleLanguage(chat);
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
        // مدیریت دستورات خاص مجوزها
        if (callbackData.StartsWith("/permissions/") || callbackData == "/permissions")
        {
            await HandlePermissionsCallback(botCommands, callbackData, msg);
            return;
        }
        
        // Extract command from callback data
        // استخراج دستور از داده‌های بازگشتی
        string commandWithPrefix = callbackData.Split('@')[0]; // Remove any username part
        string commandstrip = commandWithPrefix.Substring(1); // Remove the leading '/'
        if (callbackData == "/echo/toggle")
        {
            await botCommands.HandleEchoToggle(msg.Chat);
            return;
        }

        switch (commandstrip)
        {
            // Respond when 'help' button is pressed
            // پاسخ دادن وقتی دکمه 'راهنما' فشرده می‌شود
            case "help":
                await botCommands.HandleCommand(commandstrip, CurrentBotChat);
                break;

            // Respond on 'start' button pressed
            // پاسخ دادن وقتی دکمه 'شروع' فشرده می‌شود
            case "start":
                await botCommands.HandleCommand(commandstrip, CurrentBotChat);
                break;
            // Respond on 'echo' button pressed
            // پاسخ دادن وقتی دکمه 'اکو' فشرده می‌شود
            case "echo":
                await botCommands.HandleCommand(commandstrip, CurrentBotChat);
                break;
            // Respond on 'users' button pressed 
            // پاسخ دادن وقتی دکمه 'کاربران' فشرده می‌شود
            case "users":
                await botCommands.HandleCommand(commandstrip, CurrentBotChat);
                break;
            // Respond on 'chats' button pressed
            // پاسخ دادن وقتی دکمه 'چت‌ها' فشرده می‌شود
            case "chats":
                await botCommands.HandleCommand(commandstrip, CurrentBotChat);
                break;
            // Respond on 'stats' button pressed
            // پاسخ دادن وقتی دکمه 'آمار' فشرده می‌شود
            case "stats":
                await botCommands.HandleCommand(commandstrip, CurrentBotChat);
                break;
            // Respond on 'permissions' button pressed
            // پاسخ دادن وقتی دکمه 'مجوزها' فشرده می‌شود
            case "permissions":
                await botCommands.PermissionsCommand(CurrentBotChat, msg.MessageId);
                break;
            case "/language/toggle":
                await botCommands.LanguageCommand(msg.Chat);
                break;

            default:
                await Logger.WriteToLogFile($"Unhandled callback command: {commandstrip}", "HandleInlineButtonPress");
                break;
                // Add more inline button actions as needed here
                // در صورت نیاز، اقدامات دکمه‌های داخلی بیشتری را در اینجا اضافه کنید
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
            // این بخش دستور پایه "/permissions" را مدیریت می‌کند
            if (callbackData == "/permissions")
            {
                await Logger.WriteToLogFile("Handling base /permissions callback", "BotCommands");
                // For plain "/permissions" callback, show the permission selection screen
                // برای بازگشت ساده "/permissions"، صفحه انتخاب مجوز را نمایش دهید
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
                    await commands._permissionManager.ShowChatPermissions(msg.Chat.Id, chatId, messageId);
                }
                break;
                
            case "user":
                if (parts.Length >= 5 && long.TryParse(parts[3], out long userChatId) && long.TryParse(parts[4], out long userId))
                {
                    await commands._permissionManager.ShowUserPermissions(msg.Chat.Id, userChatId, userId, messageId);
                }
                break;
                
            case "promote":
                if (parts.Length >= 5 && long.TryParse(parts[3], out long promChatId) && long.TryParse(parts[4], out long promUserId))
                {
                    string stateKey = $"{promChatId}:{promUserId}";
                    
                    if (parts.Length >= 6)
                    {
                        // Handle specific permission toggle or apply promotion
                        // مدیریت تغییر وضعیت مجوز خاص یا اعمال ارتقا
                        string permAction = parts[5];
                        
                        if (permAction == "apply")
                        {
                            // Get permissions from stored state and apply
                            // دریافت مجوزها از وضعیت ذخیره شده و اعمال آنها
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
                            // پاک کردن وضعیت مجوزها پس از اعمال
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
                            // تاخیر کوچک قبل از نمایش اطلاعات کاربر
                            await Task.Delay(1000);
                            
                            // Go back to user info
                            // بازگشت به اطلاعات کاربر
                            await commands._permissionManager.ShowUserPermissions(msg.Chat.Id, promChatId, promUserId, messageId);
                        }
                        else
                        {
                            // Toggle a specific permission and show the promote screen again
                            // تغییر وضعیت یک مجوز خاص و نمایش مجدد صفحه ارتقا
                            TogglePermission(stateKey, permAction);
                            await commands._permissionManager.ShowUserPermissions(msg.Chat.Id, promChatId, promUserId, messageId);
                        }
                    }
                    else
                    {
                        // Show promotion screen
                        // نمایش صفحه ارتقا
                        await commands._permissionManager.ShowUserPermissions(msg.Chat.Id, promChatId, promUserId, messageId);
                    }
                }
                break;
                
            case "demote":
                if (parts.Length >= 5 && long.TryParse(parts[3], out long demChatId) && long.TryParse(parts[4], out long demUserId))
                {
                    if (parts.Length >= 6 && parts[5] == "confirm")
                    {
                        // Confirm demotion
                        // تایید تنزل
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
                        // تاخیر کوچک قبل از نمایش اطلاعات کاربر
                        await Task.Delay(1000);
                        
                        // Go back to user info
                        // بازگشت به اطلاعات کاربر
                        await commands._permissionManager.ShowUserPermissions(msg.Chat.Id, demChatId, demUserId, messageId);
                    }
                    else
                    {
                        // Show demotion confirmation
                        // نمایش تایید تنزل
                        await commands._permissionManager.ShowUserPermissions(msg.Chat.Id, demChatId, demUserId, messageId);
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
                        // مدیریت تغییر وضعیت مجوز خاص یا اعمال محدودیت‌ها
                        string permAction = parts[5];
                        
                        if (permAction == "apply")
                        {
                            // Get permissions from stored state and apply
                            // دریافت مجوزها از وضعیت ذخیره شده و اعمال آنها
                            bool success = await BotMain.RestrictUser(
                                resChatId, 
                                resUserId,
                                GetPermissionState(stateKey, "sendMessages", true),
                                GetPermissionState(stateKey, "inviteUsers", true),
                                GetPermissionState(stateKey, "pinMessages", true),
                                GetPermissionState(stateKey, "changeInfo", true)
                            );
                            
                            // Clear permissions state after application
                            // پاک کردن وضعیت مجوزها پس از اعمال
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
                            // تاخیر کوچک قبل از نمایش اطلاعات کاربر
                            await Task.Delay(1000);
                            
                            // Go back to user info
                            // بازگشت به اطلاعات کاربر
                            await commands._permissionManager.ShowUserPermissions(msg.Chat.Id, resChatId, resUserId, messageId);
                        }
                        else
                        {
                            // Toggle a specific permission and show the restrict screen again
                            // تغییر وضعیت یک مجوز خاص و نمایش مجدد صفحه محدودیت
                            TogglePermission(stateKey, permAction, true);
                            await commands._permissionManager.ShowUserPermissions(msg.Chat.Id, resChatId, resUserId, messageId);
                        }
                    }
                    else
                    {
                        // Show restriction screen
                        // نمایش صفحه محدودیت
                        await commands._permissionManager.ShowUserPermissions(msg.Chat.Id, resChatId, resUserId, messageId);
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
                        // ویرایش مجوزهای مدیر
                        await commands._permissionManager.ShowUserPermissions(msg.Chat.Id, editChatId, editUserId, messageId);
                    }
                    else if (editType == "user")
                    {
                        // Edit regular user permissions
                        // ویرایش مجوزهای کاربر عادی
                        await commands._permissionManager.ShowUserPermissions(msg.Chat.Id, editChatId, editUserId, messageId);
                    }
                }
                break;
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
        // حذف تمام کلیدهایی که با این کلید شروع می‌شوند
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
