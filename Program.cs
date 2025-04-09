namespace Telegramski_Botski;

public static class Program
{
    static async Task Main()
    {
        await Logger.WriteToLogFile("Program Init");

        // Load configuration settings from the config file.
        await Logger.WriteToLogFile("Attempting to read config file.");
        await ConfigManager.ReadConfig();

        // Check if the log file exists; create it if it doesn't.
        await Logger.WriteToLogFile("Checking if log file exists to create one or not.");

        // Retrieve bot token and proxy settings from the config and initialize the bot.
        if (
            ConfigManager.Config.TryGetValue("TOKEN", out string configVal)
            && ConfigManager.Config.TryGetValue("PROXY", out string proxyStr)
        )
        {
            await BotMain.InitTGBot(configVal, proxyStr: proxyStr);
        }
    }
}
