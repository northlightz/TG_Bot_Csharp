using System.Net;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegramski_Botski;

public static class BotMain
{
    private static BotCommands botCommands;

    public static async Task InitTGBot(string API_KEY, string proxyStr)
    {
        // Initialize GroupManager
        await GroupManager.Initialize();

        WebProxy BotProxy = new(proxyStr);

        // Set up HTTP client with proxy settings for bot connection.
        using var httpHandler = new SocketsHttpHandler { Proxy = BotProxy, UseProxy = true };
        using var BotHTTPClient = new HttpClient(httpHandler);

        using var BotDisconnector = new CancellationTokenSource();

        // Initialize Telegram Bot Client with API key and HTTP client.
        TelegramBotClient BotCNC = new(
            API_KEY,
            httpClient: BotHTTPClient,
            cancellationToken: BotDisconnector.Token
        );

        // Setup event handlers for bot updates and errors.
        BotCNC.OnError += OnError;
        BotCNC.OnMessage += OnMessage;
        BotCNC.OnUpdate += OnUpdate;
        BotCNC.OnMyChatMember += OnMyChatMember;

        var BotGet = await BotCNC.GetMe();

        botCommands = new BotCommands(BotCNC);

        await Logger.WriteToLogFile(
            $"Connected. ID : {BotGet.Id} ProfileName : {BotGet.FirstName}",
            "BotConnection"
        );

        // Notify user that bot is running and how to terminate it.
        await Logger.WriteToLogFile(
            $"@{BotGet.Username} is now running... Press Enter to terminate",
            "BotConnection"
        );

        await Task.Run(() => Console.ReadLine());

        await BotDisconnector.CancelAsync();

        async Task OnError(Exception exception, HandleErrorSource source)
        {
            // Log errors during polling or message handling.
            await Logger.WriteToLogFile(exception.Message, "BotConnection");
        }

        async Task OnMessage(Message msg, UpdateType type)
        {
            // Log received messages and pass them to command handler.
            await Logger.WriteToLogFile(
                $"Received Message \"{msg.Text}\" of type \"{type}\"",
                "OnMessage"
            );
            await botCommands!.HandleCommands(msg);
        }

        async Task OnUpdate(Update update)
        {
            // Handle callback queries and inline button presses.
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
        }

        async Task OnMyChatMember(ChatMemberUpdated chatMemberUpdated)
        {
            if (chatMemberUpdated.NewChatMember.Status == ChatMemberStatus.Administrator ||
                chatMemberUpdated.NewChatMember.Status == ChatMemberStatus.Member)
            {
                await GroupManager.AddGroup(chatMemberUpdated.Chat);
            }
            else if (chatMemberUpdated.NewChatMember.Status == ChatMemberStatus.Left ||
                     chatMemberUpdated.NewChatMember.Status == ChatMemberStatus.Kicked)
            {
                await GroupManager.RemoveGroup(chatMemberUpdated.Chat.Id);
            }
        }
    }
}
