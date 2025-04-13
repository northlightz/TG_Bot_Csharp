using Microsoft.Data.Sqlite;
using Telegram.Bot.Types;

namespace Telegramski_Botski;

/// <summary>
/// Manages database operations for tracking users, groups, and permissions.
/// </summary>
/// <remarks>
/// مدیریت عملیات پایگاه داده برای پیگیری کاربران، گروه‌ها و مجوزها.
/// </remarks>
public static class DatabaseManager
{
    private static readonly string DatabasePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "tg_dev_files",
        "botdata.db"
    );

    private static SqliteConnection GetConnection()
    {
        return new SqliteConnection($"Data Source={DatabasePath}");
    }

    /// <summary>
    /// Initializes the database by creating necessary tables if they don't exist.
    /// </summary>
    /// <remarks>
    /// پایگاه داده را با ایجاد جداول ضروری در صورت عدم وجود، راه‌اندازی می‌کند.
    /// </remarks>
    public static async Task InitializeDatabase()
    {
        // Create directory if it doesn't exist
        // ایجاد دایرکتوری در صورت عدم وجود
        var directory = Path.GetDirectoryName(DatabasePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var connection = GetConnection();
        await connection.OpenAsync();

        // Create Users table
        // ایجاد جدول کاربران
        var createUsersTableCommand = connection.CreateCommand();
        createUsersTableCommand.CommandText = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY,
                Username TEXT NULL,
                FirstName TEXT NULL,
                LastName TEXT NULL,
                IsBot BOOLEAN NOT NULL,
                LanguageCode TEXT NULL,
                FirstSeen TEXT NOT NULL,
                LastSeen TEXT NOT NULL
            )
        ";
        await createUsersTableCommand.ExecuteNonQueryAsync();

        // Create Chats table for groups, channels, etc.
        // ایجاد جدول چت‌ها برای گروه‌ها، کانال‌ها و غیره
        var createChatsTableCommand = connection.CreateCommand();
        createChatsTableCommand.CommandText = @"
            CREATE TABLE IF NOT EXISTS Chats (
                Id INTEGER PRIMARY KEY,
                Title TEXT NULL,
                Type TEXT NOT NULL,
                Username TEXT NULL,
                FirstSeen TEXT NOT NULL,
                LastSeen TEXT NOT NULL
            )
        ";
        await createChatsTableCommand.ExecuteNonQueryAsync();

        // Create Permissions table
        // ایجاد جدول مجوزها
        var createPermissionsTableCommand = connection.CreateCommand();
        createPermissionsTableCommand.CommandText = @"
            CREATE TABLE IF NOT EXISTS Permissions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                ChatId INTEGER NOT NULL,
                IsAdmin BOOLEAN NOT NULL DEFAULT 0,
                CanPostMessages BOOLEAN NULL,
                CanEditMessages BOOLEAN NULL,
                CanDeleteMessages BOOLEAN NULL,
                CanRestrictMembers BOOLEAN NULL,
                CanPromoteMembers BOOLEAN NULL,
                CanChangeInfo BOOLEAN NULL,
                CanInviteUsers BOOLEAN NULL,
                CanPinMessages BOOLEAN NULL,
                LastUpdated TEXT NOT NULL,
                UNIQUE(UserId, ChatId),
                FOREIGN KEY(UserId) REFERENCES Users(Id),
                FOREIGN KEY(ChatId) REFERENCES Chats(Id)
            )
        ";
        await createPermissionsTableCommand.ExecuteNonQueryAsync();

        await Logger.WriteToLogFile("Database initialized successfully", "DatabaseManager");
    }

    /// <summary>
    /// Tracks a user by adding them to the database or updating their information.
    /// </summary>
    /// <remarks>
    /// با افزودن کاربر به پایگاه داده یا به‌روزرسانی اطلاعات آن‌ها، آن‌ها را پیگیری می‌کند.
    /// </remarks>
    public static async Task TrackUser(User user)
    {
        if (user == null) return;

        await using var connection = GetConnection();
        await connection.OpenAsync();

        var currentTime = DateTime.UtcNow.ToString("o");

        // Check if user exists
        // بررسی وجود کاربر
        var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = "SELECT COUNT(*) FROM Users WHERE Id = $userId";
        checkCommand.Parameters.AddWithValue("$userId", user.Id);
        var userExists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

        if (userExists)
        {
            // Update existing user
            // به‌روزرسانی کاربر موجود
            var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = @"
                UPDATE Users SET 
                    Username = $username, 
                    FirstName = $firstName, 
                    LastName = $lastName, 
                    IsBot = $isBot, 
                    LanguageCode = $languageCode, 
                    LastSeen = $lastSeen
                WHERE Id = $userId
            ";
            updateCommand.Parameters.AddWithValue("$username", user.Username ?? (object)DBNull.Value);
            updateCommand.Parameters.AddWithValue("$firstName", user.FirstName ?? (object)DBNull.Value);
            updateCommand.Parameters.AddWithValue("$lastName", user.LastName ?? (object)DBNull.Value);
            updateCommand.Parameters.AddWithValue("$isBot", user.IsBot);
            updateCommand.Parameters.AddWithValue("$languageCode", user.LanguageCode ?? (object)DBNull.Value);
            updateCommand.Parameters.AddWithValue("$lastSeen", currentTime);
            updateCommand.Parameters.AddWithValue("$userId", user.Id);

            await updateCommand.ExecuteNonQueryAsync();
            await Logger.WriteToLogFile($"Updated user {user.Id} ({user.FirstName})", "DatabaseManager");
        }
        else
        {
            // Insert new user
            // درج کاربر جدید
            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"
                INSERT INTO Users (Id, Username, FirstName, LastName, IsBot, LanguageCode, FirstSeen, LastSeen)
                VALUES ($userId, $username, $firstName, $lastName, $isBot, $languageCode, $firstSeen, $lastSeen)
            ";
            insertCommand.Parameters.AddWithValue("$userId", user.Id);
            insertCommand.Parameters.AddWithValue("$username", user.Username ?? (object)DBNull.Value);
            insertCommand.Parameters.AddWithValue("$firstName", user.FirstName ?? (object)DBNull.Value);
            insertCommand.Parameters.AddWithValue("$lastName", user.LastName ?? (object)DBNull.Value);
            insertCommand.Parameters.AddWithValue("$isBot", user.IsBot);
            insertCommand.Parameters.AddWithValue("$languageCode", user.LanguageCode ?? (object)DBNull.Value);
            insertCommand.Parameters.AddWithValue("$firstSeen", currentTime);
            insertCommand.Parameters.AddWithValue("$lastSeen", currentTime);

            await insertCommand.ExecuteNonQueryAsync();
            await Logger.WriteToLogFile($"Added new user {user.Id} ({user.FirstName})", "DatabaseManager");
        }
    }

    /// <summary>
    /// Tracks a chat by adding it to the database or updating its information.
    /// </summary>
    /// <remarks>
    /// با افزودن چت به پایگاه داده یا به‌روزرسانی اطلاعات آن، آن را پیگیری می‌کند.
    /// </remarks>
    public static async Task TrackChat(Chat chat)
    {
        if (chat == null) return;

        await using var connection = GetConnection();
        await connection.OpenAsync();

        var currentTime = DateTime.UtcNow.ToString("o");

        // Check if chat exists
        // بررسی وجود چت
        var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = "SELECT COUNT(*) FROM Chats WHERE Id = $chatId";
        checkCommand.Parameters.AddWithValue("$chatId", chat.Id);
        var chatExists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

        if (chatExists)
        {
            // Update existing chat
            // به‌روزرسانی چت موجود
            var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = @"
                UPDATE Chats SET 
                    Title = $title, 
                    Type = $type, 
                    Username = $username, 
                    LastSeen = $lastSeen
                WHERE Id = $chatId
            ";
            updateCommand.Parameters.AddWithValue("$title", chat.Title ?? (object)DBNull.Value);
            updateCommand.Parameters.AddWithValue("$type", chat.Type.ToString());
            updateCommand.Parameters.AddWithValue("$username", chat.Username ?? (object)DBNull.Value);
            updateCommand.Parameters.AddWithValue("$lastSeen", currentTime);
            updateCommand.Parameters.AddWithValue("$chatId", chat.Id);

            await updateCommand.ExecuteNonQueryAsync();
            await Logger.WriteToLogFile($"Updated chat {chat.Id} ({chat.Title ?? chat.Type.ToString()})", "DatabaseManager");
        }
        else
        {
            // Insert new chat
            // درج چت جدید
            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"
                INSERT INTO Chats (Id, Title, Type, Username, FirstSeen, LastSeen)
                VALUES ($chatId, $title, $type, $username, $firstSeen, $lastSeen)
            ";
            insertCommand.Parameters.AddWithValue("$chatId", chat.Id);
            insertCommand.Parameters.AddWithValue("$title", chat.Title ?? (object)DBNull.Value);
            insertCommand.Parameters.AddWithValue("$type", chat.Type.ToString());
            insertCommand.Parameters.AddWithValue("$username", chat.Username ?? (object)DBNull.Value);
            insertCommand.Parameters.AddWithValue("$firstSeen", currentTime);
            insertCommand.Parameters.AddWithValue("$lastSeen", currentTime);

            await insertCommand.ExecuteNonQueryAsync();
            await Logger.WriteToLogFile($"Added new chat {chat.Id} ({chat.Title ?? chat.Type.ToString()})", "DatabaseManager");
        }
    }

    /// <summary>
    /// Updates permissions for a user in a specific chat.
    /// </summary>
    /// <remarks>
    /// مجوزهای یک کاربر را در یک چت خاص به‌روز می‌کند.
    /// </remarks>
    public static async Task UpdatePermissions(
        long userId, 
        long chatId, 
        bool isAdmin = false,
        bool? canPostMessages = null,
        bool? canEditMessages = null,
        bool? canDeleteMessages = null,
        bool? canRestrictMembers = null,
        bool? canPromoteMembers = null,
        bool? canChangeInfo = null,
        bool? canInviteUsers = null,
        bool? canPinMessages = null)
    {
        await using var connection = GetConnection();
        await connection.OpenAsync();

        var currentTime = DateTime.UtcNow.ToString("o");

        // Check if permissions record exists
        // بررسی وجود رکورد مجوزها
        var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = "SELECT COUNT(*) FROM Permissions WHERE UserId = $userId AND ChatId = $chatId";
        checkCommand.Parameters.AddWithValue("$userId", userId);
        checkCommand.Parameters.AddWithValue("$chatId", chatId);
        var permissionsExist = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

        if (permissionsExist)
        {
            // Update existing permissions
            // به‌روزرسانی مجوزهای موجود
            var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = @"
                UPDATE Permissions SET 
                    IsAdmin = $isAdmin,
                    CanPostMessages = COALESCE($canPostMessages, CanPostMessages),
                    CanEditMessages = COALESCE($canEditMessages, CanEditMessages),
                    CanDeleteMessages = COALESCE($canDeleteMessages, CanDeleteMessages),
                    CanRestrictMembers = COALESCE($canRestrictMembers, CanRestrictMembers),
                    CanPromoteMembers = COALESCE($canPromoteMembers, CanPromoteMembers),
                    CanChangeInfo = COALESCE($canChangeInfo, CanChangeInfo),
                    CanInviteUsers = COALESCE($canInviteUsers, CanInviteUsers),
                    CanPinMessages = COALESCE($canPinMessages, CanPinMessages),
                    LastUpdated = $lastUpdated
                WHERE UserId = $userId AND ChatId = $chatId
            ";
            updateCommand.Parameters.AddWithValue("$isAdmin", isAdmin);
            updateCommand.Parameters.AddWithValue("$canPostMessages", canPostMessages.HasValue ? (object)canPostMessages.Value : DBNull.Value);
            updateCommand.Parameters.AddWithValue("$canEditMessages", canEditMessages.HasValue ? (object)canEditMessages.Value : DBNull.Value);
            updateCommand.Parameters.AddWithValue("$canDeleteMessages", canDeleteMessages.HasValue ? (object)canDeleteMessages.Value : DBNull.Value);
            updateCommand.Parameters.AddWithValue("$canRestrictMembers", canRestrictMembers.HasValue ? (object)canRestrictMembers.Value : DBNull.Value);
            updateCommand.Parameters.AddWithValue("$canPromoteMembers", canPromoteMembers.HasValue ? (object)canPromoteMembers.Value : DBNull.Value);
            updateCommand.Parameters.AddWithValue("$canChangeInfo", canChangeInfo.HasValue ? (object)canChangeInfo.Value : DBNull.Value);
            updateCommand.Parameters.AddWithValue("$canInviteUsers", canInviteUsers.HasValue ? (object)canInviteUsers.Value : DBNull.Value);
            updateCommand.Parameters.AddWithValue("$canPinMessages", canPinMessages.HasValue ? (object)canPinMessages.Value : DBNull.Value);
            updateCommand.Parameters.AddWithValue("$lastUpdated", currentTime);
            updateCommand.Parameters.AddWithValue("$userId", userId);
            updateCommand.Parameters.AddWithValue("$chatId", chatId);

            await updateCommand.ExecuteNonQueryAsync();
            await Logger.WriteToLogFile($"Updated permissions for user {userId} in chat {chatId}", "DatabaseManager");
        }
        else
        {
            // Insert new permissions
            // درج مجوزهای جدید
            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"
                INSERT INTO Permissions (
                    UserId, ChatId, IsAdmin, 
                    CanPostMessages, CanEditMessages, CanDeleteMessages,
                    CanRestrictMembers, CanPromoteMembers, CanChangeInfo,
                    CanInviteUsers, CanPinMessages, LastUpdated
                )
                VALUES (
                    $userId, $chatId, $isAdmin,
                    $canPostMessages, $canEditMessages, $canDeleteMessages,
                    $canRestrictMembers, $canPromoteMembers, $canChangeInfo,
                    $canInviteUsers, $canPinMessages, $lastUpdated
                )
            ";
            insertCommand.Parameters.AddWithValue("$userId", userId);
            insertCommand.Parameters.AddWithValue("$chatId", chatId);
            insertCommand.Parameters.AddWithValue("$isAdmin", isAdmin);
            insertCommand.Parameters.AddWithValue("$canPostMessages", canPostMessages.HasValue ? (object)canPostMessages.Value : DBNull.Value);
            insertCommand.Parameters.AddWithValue("$canEditMessages", canEditMessages.HasValue ? (object)canEditMessages.Value : DBNull.Value);
            insertCommand.Parameters.AddWithValue("$canDeleteMessages", canDeleteMessages.HasValue ? (object)canDeleteMessages.Value : DBNull.Value);
            insertCommand.Parameters.AddWithValue("$canRestrictMembers", canRestrictMembers.HasValue ? (object)canRestrictMembers.Value : DBNull.Value);
            insertCommand.Parameters.AddWithValue("$canPromoteMembers", canPromoteMembers.HasValue ? (object)canPromoteMembers.Value : DBNull.Value);
            insertCommand.Parameters.AddWithValue("$canChangeInfo", canChangeInfo.HasValue ? (object)canChangeInfo.Value : DBNull.Value);
            insertCommand.Parameters.AddWithValue("$canInviteUsers", canInviteUsers.HasValue ? (object)canInviteUsers.Value : DBNull.Value);
            insertCommand.Parameters.AddWithValue("$canPinMessages", canPinMessages.HasValue ? (object)canPinMessages.Value : DBNull.Value);
            insertCommand.Parameters.AddWithValue("$lastUpdated", currentTime);

            await insertCommand.ExecuteNonQueryAsync();
            await Logger.WriteToLogFile($"Added permissions for user {userId} in chat {chatId}", "DatabaseManager");
        }
    }

    /// <summary>
    /// Retrieves a list of all tracked users.
    /// </summary>
    /// <remarks>
    /// لیستی از تمام کاربران ردیابی شده را بازیابی می‌کند.
    /// </remarks>
    public static async Task<List<(long Id, string Username, string FirstName, string LastName)>> GetAllUsers()
    {
        var users = new List<(long Id, string Username, string FirstName, string LastName)>();
        
        await using var connection = GetConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Username, FirstName, LastName FROM Users ORDER BY LastSeen DESC";
        
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var id = reader.GetInt64(0);
            string username = reader.IsDBNull(1) ? null : reader.GetString(1);
            string firstName = reader.IsDBNull(2) ? null : reader.GetString(2);
            string lastName = reader.IsDBNull(3) ? null : reader.GetString(3);
            
            users.Add((id, username, firstName, lastName));
        }

        return users;
    }

    /// <summary>
    /// Retrieves a list of all tracked chats.
    /// </summary>
    /// <remarks>
    /// لیستی از تمام چت‌های ردیابی شده را بازیابی می‌کند.
    /// </remarks>
    public static async Task<List<(long Id, string Title, string Type)>> GetAllChats()
    {
        var chats = new List<(long Id, string Title, string Type)>();
        
        await using var connection = GetConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Title, Type FROM Chats ORDER BY LastSeen DESC";
        
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var id = reader.GetInt64(0);
            string title = reader.IsDBNull(1) ? null : reader.GetString(1);
            string type = reader.GetString(2);
            
            chats.Add((id, title, type));
        }

        return chats;
    }
} 