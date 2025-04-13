using System.Threading.Channels;

namespace Telegramski_Botski;

public static class Logger
{
    // Unbounded channel for queuing log messages asynchronously.
    // کانال نامحدود برای صف‌بندی پیام‌های گزارش به صورت غیرهمزمان.
    private static readonly Channel<string> _logChannel = Channel.CreateUnbounded<string>();

    // Semaphore to ensure only one thread writes to the log file at a time.
    // سمافور برای اطمینان از اینکه فقط یک نخ در یک زمان به فایل گزارش می‌نویسد.
    private static readonly SemaphoreSlim _logSemaphore = new(1, 1);

    // Asynchronously writes a log message to a file, ensuring thread safety.
    // به صورت غیرهمزمان یک پیام گزارش را به فایل می‌نویسد، با اطمینان از ایمنی نخ.
    public static async Task WriteToLogFile(string logStr, string logOrg = "UnknownSrc")
    {
        // Append log messages to the log file and print them to the console.
        // پیام‌های گزارش را به فایل گزارش اضافه کنید و آن‌ها را در کنسول چاپ کنید.
        Console.WriteLine($"On :: [{DateTime.Now}] || From <{logOrg}>,  Message :: {logStr}");
        string entry = $"[{DateTime.Now}] || FROM <{logOrg}> >> MESSAGE >> {logStr}";
        // Batch processing every 100ms or 50 messages.
        // Basically to be safe we wait till it determines we are done writing to the file
        // (actually writing to the stream as i understand it because we append later)
        // (look i really tried understanding. this shi too complicated for my ass)
        // پردازش دسته‌ای هر ۱۰۰ میلی‌ثانیه یا ۵۰ پیام.
        // اساساً برای اطمینان منتظر می‌مانیم تا تعیین شود که نوشتن در فایل به پایان رسیده است
        // (در واقع نوشتن در جریان همانطور که من متوجه شدم زیرا ما بعداً اضافه می‌کنیم)
        // (ببین واقعاً سعی کردم بفهمم. این موضوع برای من خیلی پیچیده است)
        await _logChannel.Writer.WriteAsync(entry);

        await _logSemaphore.WaitAsync();
        try
        {
            await using StreamWriter writer = new(Constants.LogFilePath, true);
            // we append them here, opening a writer session and then writing line by line.
            // ما آن‌ها را اینجا اضافه می‌کنیم، یک جلسه نویسنده را باز می‌کنیم و سپس خط به خط می‌نویسیم.
            await writer.WriteLineAsync(entry);
            return;
        }
        finally
        {
            _logSemaphore.Release();
        }
    }

    public static async Task CreateLogFileIfNotExist()
    {
        if (File.Exists(Constants.LogFilePath))
        {
            // Return if the log file already exists.
            // اگر فایل گزارش از قبل وجود دارد، برگردید.
            await WriteToLogFile("Log file already exists, appending to it.");
        }
        else
        {
            // Create a new log file if it doesn't exist.
            // اگر فایل گزارش وجود ندارد، یک فایل جدید ایجاد کنید.
            using (File.Create(Constants.LogFilePath))
                await WriteToLogFile("Log file created. Switching to log file.");
        }
    }
}
