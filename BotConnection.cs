using System.Net;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegramski_Botski;

public static class BotMain
{
    private static BotCommands botCommands;
    private static TelegramBotClient botClient;

    public static async Task InitTGBot(string API_KEY, string proxyStr)
    {
        WebProxy BotProxy = new(proxyStr);

        // Set up HTTP client with proxy settings for bot connection.
        // راه‌اندازی کلاینت HTTP با تنظیمات پراکسی برای اتصال ربات.
        using var httpHandler = new SocketsHttpHandler { Proxy = BotProxy, UseProxy = true };
        using var BotHTTPClient = new HttpClient(httpHandler);

        using var BotDisconnector = new CancellationTokenSource();

        // Initialize Telegram Bot Client with API key and HTTP client.
        // راه‌اندازی کلاینت ربات تلگرام با کلید API و کلاینت HTTP.
        TelegramBotClient BotCNC = new(
            API_KEY,
            httpClient: BotHTTPClient,
            cancellationToken: BotDisconnector.Token
        );

        botClient = BotCNC;

        // Setup event handlers for bot updates and errors.
        // راه‌اندازی مدیریت‌کننده‌های رویداد برای به‌روزرسانی‌ها و خطاهای ربات.
        BotCNC.OnError += OnError;
        BotCNC.OnMessage += OnMessage;
        BotCNC.OnUpdate += OnUpdate;

        var BotGet = await BotCNC.GetMe();

        botCommands = new BotCommands(BotCNC);

        await Logger.WriteToLogFile(
            $"Connected. ID : {BotGet.Id} ProfileName : {BotGet.FirstName}",
            "BotConnection"
        );

        // Notify user that bot is running and how to terminate it.
        // به کاربر اطلاع دهید که ربات در حال اجرا است و چگونه آن را متوقف کند.
        await Logger.WriteToLogFile(
            $"@{BotGet.Username} is now running... Press Enter to terminate",
            "BotConnection"
        );

        await Task.Run(() => Console.ReadLine());

        await BotDisconnector.CancelAsync();

        async Task OnError(Exception exception, HandleErrorSource source)
        {
            // Log errors during polling or message handling.
            // ثبت خطاها در حین نظرسنجی یا مدیریت پیام.
            await Logger.WriteToLogFile(exception.Message, "BotConnection");
        }

        async Task OnMessage(Message msg, UpdateType type)
        {
            // Log received messages and pass them to command handler.
            // ثبت پیام‌های دریافتی و ارسال آن‌ها به مدیریت‌کننده دستور.
            await Logger.WriteToLogFile(
                $"Received Message \"{msg.Text}\" of type \"{type}\"",
                "OnMessage"
            );
            await botCommands!.HandleCommands(msg);
        }

        async Task OnUpdate(Update update)
        {
            // Handle callback queries and inline button presses.
            // مدیریت درخواست‌های بازگشتی و فشار دکمه‌های داخلی.
            if (
                update is { CallbackQuery: { } query }
                && query != null
                && query.Data != null
                && query.Message != null
                && botCommands != null
            )
            {
                await BotCNC.AnswerCallbackQuery(query.Id, $"You picked {query.Data}");
                await BotCommands.HandleInlineButtonPress(botCommands, query.Data, query.Message);
            }
            // Track chat member updates for permissions
            // پیگیری به‌روزرسانی‌های اعضای چت برای مجوزها
            else if (update.MyChatMember != null)
            {
                await TrackChatMemberUpdate(update.MyChatMember);
            }
            else if (update.ChatMember != null)
            {
                await TrackChatMemberUpdate(update.ChatMember);
            }
        }
    }

    /// <summary>
    /// Tracks updates to chat member permissions.
    /// </summary>
    /// <remarks>
    /// تغییرات مجوزهای اعضای چت را پیگیری می‌کند.
    /// </remarks>
    private static async Task TrackChatMemberUpdate(ChatMemberUpdated memberUpdate)
    {
        // Track the chat where the update occurred
        // پیگیری چتی که به‌روزرسانی در آن رخ داده است
        if (memberUpdate.Chat != null)
        {
            await DatabaseManager.TrackChat(memberUpdate.Chat);
        }

        // Track the user who was updated
        // پیگیری کاربری که به‌روزرسانی شده است
        if (memberUpdate.NewChatMember.User != null)
        {
            await DatabaseManager.TrackUser(memberUpdate.NewChatMember.User);
        }

        // If the user is removed, don't track permissions
        // اگر کاربر حذف شده است، مجوزها را پیگیری نکنید
        if (memberUpdate.NewChatMember.Status == ChatMemberStatus.Left ||
            memberUpdate.NewChatMember.Status == ChatMemberStatus.Kicked)
        {
            return;
        }

        // Extract and save permissions based on member type
        // استخراج و ذخیره مجوزها بر اساس نوع عضو
        bool isAdmin = false;
        bool? canPostMessages = null;
        bool? canEditMessages = null;
        bool? canDeleteMessages = null;
        bool? canRestrictMembers = null;
        bool? canPromoteMembers = null;
        bool? canChangeInfo = null;
        bool? canInviteUsers = null;
        bool? canPinMessages = null;

        switch (memberUpdate.NewChatMember)
        {
            case ChatMemberAdministrator admin:
                isAdmin = true;
                canPostMessages = admin.CanPostMessages;
                canEditMessages = admin.CanEditMessages;
                canDeleteMessages = admin.CanDeleteMessages;
                canRestrictMembers = admin.CanRestrictMembers;
                canPromoteMembers = admin.CanPromoteMembers;
                canChangeInfo = admin.CanChangeInfo;
                canInviteUsers = admin.CanInviteUsers;
                canPinMessages = admin.CanPinMessages;
                break;

            case ChatMemberOwner owner:
                isAdmin = true;
                canPostMessages = true;
                canEditMessages = true;
                canDeleteMessages = true;
                canRestrictMembers = true;
                canPromoteMembers = true;
                canChangeInfo = true;
                canInviteUsers = true;
                canPinMessages = true;
                break;
                
            case ChatMemberRestricted restricted:
                canPostMessages = restricted.CanSendMessages;
                canPinMessages = restricted.CanPinMessages;
                canChangeInfo = restricted.CanChangeInfo;
                canInviteUsers = restricted.CanInviteUsers;
                break;
        }

        await DatabaseManager.UpdatePermissions(
            memberUpdate.NewChatMember.User.Id,
            memberUpdate.Chat.Id,
            isAdmin,
            canPostMessages,
            canEditMessages,
            canDeleteMessages,
            canRestrictMembers,
            canPromoteMembers,
            canChangeInfo,
            canInviteUsers,
            canPinMessages
        );
    }

    /// <summary>
    /// Retrieves a list of all tracked users.
    /// </summary>
    /// <remarks>
    /// لیستی از تمام کاربران ردیابی شده را بازیابی می‌کند.
    /// </remarks>
    public static async Task<List<(long Id, string Username, string FirstName, string LastName)>> GetAllUsers()
    {
        return await DatabaseManager.GetAllUsers();
    }

    /// <summary>
    /// Retrieves a list of all tracked chats.
    /// </summary>
    /// <remarks>
    /// لیستی از تمام چت‌های ردیابی شده را بازیابی می‌کند.
    /// </remarks>
    public static async Task<List<(long Id, string Title, string Type)>> GetAllChats()
    {
        return await DatabaseManager.GetAllChats();
    }
}
