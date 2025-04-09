using System.Threading.Channels;

namespace Telegramski_Botski;

public static class Logger
{
    // Unbounded channel for queuing log messages asynchronously.
    private static readonly Channel<string> _logChannel = Channel.CreateUnbounded<string>();

    // Semaphore to ensure only one thread writes to the log file at a time.
    private static readonly SemaphoreSlim _logSemaphore = new(1, 1);

    // Asynchronously writes a log message to a file, ensuring thread safety.
    public static async Task WriteToLogFile(string logStr, string logOrg = "UnknownSrc")
    {
        // Append log messages to the log file and print them to the console.
        Console.WriteLine($"On :: [{DateTime.Now}] || From <{logOrg}>,  Message :: {logStr}");
        string entry = $"[{DateTime.Now}] || FROM <{logOrg}> >> MESSAGE >> {logStr}";
        // Batch processing every 100ms or 50 messages.
        // Basically to be safe we wait till it determines we are done writing to the file
        // (actually writing to the stream as i understand it because we append later)
        // (look i really tried understanding. this shi too complicated for my ass)
        await _logChannel.Writer.WriteAsync(entry);

        await _logSemaphore.WaitAsync();
        try
        {
            await using StreamWriter writer = new(Constants.LogFilePath, true);
            // we append them here, opening a writer session and then writing line by line.
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
            await WriteToLogFile("Log file already exists, appending to it.");
        }
        else
        {
            // Create a new log file if it doesn't exist.
            using (File.Create(Constants.LogFilePath))
                await WriteToLogFile("Log file created. Switching to log file.");
        }
    }
}
