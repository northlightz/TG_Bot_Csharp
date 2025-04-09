using System.Text.RegularExpressions;

namespace Telegramski_Botski;

public static class Constants
{
    // Path to the configuration file where bot settings are stored.
    public static string ConfigFilePath { get; set; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "tg_dev_files",
            "botconfig.conf"
        );

    // Path to the log file for storing bot logs.
    public static string LogFilePath { get; set; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "tg_dev_files",
            "botlog.log"
        );
}

public static partial class ConfigManager
{
    // Dictionary to store key-value pairs from the configuration file.
    public static Dictionary<string, string> Config { get; private set; } = [];
    private static readonly Regex _configRegex = MyRegex();

    public static async Task ReadConfig()
    {
        // Read the configuration file line by line.
        using StreamReader fileReader = new(Constants.ConfigFilePath);
        string fileText = await fileReader.ReadToEndAsync();
        string[] fileLines = fileText.Split(Environment.NewLine, StringSplitOptions.None);

        foreach (string fileLine in fileLines)
        {
            string trimmedFileLine = fileLine.Trim();
            // Match lines in the format "key" : "value".
            Match match = _configRegex.Match(trimmedFileLine);
            if (match.Success)
            {
                Config[match.Groups[1].Value] = match.Groups[2].Value;
            }
        }
        return;
    }

    [GeneratedRegex(
        @"^""([^""]+)""\s*[:=]\s*""([^""]+)""$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant
    )]
    private static partial Regex MyRegex();
}
