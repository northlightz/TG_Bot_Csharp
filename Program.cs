namespace Telegramski_Botski;

public static class Program
{
    static async Task Main()
    {
        await Logger.WriteToLogFile("Program Init", "ProgramInit");

        // Load configuration settings from the config file.
        // بارگذاری تنظیمات پیکربندی از فایل پیکربندی.
        await Logger.WriteToLogFile("Attempting to read config file.", "ProgramInit");
        await ConfigManager.ReadConfig();

        // Check if the log file exists; create it if it doesn't.
        // بررسی وجود فایل گزارش؛ در صورت عدم وجود، آن را ایجاد کنید.
        await Logger.WriteToLogFile(
            "Checking if log file exists to create one or not.",
            "ProgramInit"
        );

        // Initialize the database for tracking users, groups, and permissions
        // راه‌اندازی پایگاه داده برای پیگیری کاربران، گروه‌ها و مجوزها
        await Logger.WriteToLogFile("Initializing database...", "ProgramInit");
        await DatabaseManager.InitializeDatabase();

        // Retrieve bot token and proxy settings from the config and initialize the bot.
        // دریافت توکن ربات و تنظیمات پراکسی از پیکربندی و راه‌اندازی ربات.
        if (
            ConfigManager.Config.TryGetValue("TOKEN", out string configVal)
            && ConfigManager.Config.TryGetValue("PROXY", out string proxyStr)
        )
        {
            await BotMain.InitTGBot(configVal, proxyStr: proxyStr);
        }
    }
}
