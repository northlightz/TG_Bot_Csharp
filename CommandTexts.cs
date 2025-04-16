using Telegram.Bot.Types;

namespace Telegramski_Botski;

public static class CommandTexts
{
    public static async Task<string> GetLocalizedCommandText(string key, long chatId)
    {
        var language = await DatabaseManager.GetChatLanguage(chatId);
        return language == "fa" ? GetPersianCommandText(key) : GetEnglishCommandText(key);
    }

    private static string GetPersianCommandText(string key)
    {
        return key switch
        {
            // Command descriptions
            "command_start" => "نمایش پیام خوش‌آمدگویی",
            "command_help" => "نمایش اطلاعات راهنما",
            "command_echo" => "تغییر وضعیت اکو پیام",
            "command_language" => "تغییر بین زبان انگلیسی و فارسی",
            "command_users" => "لیست تمام کاربران ردیابی شده",
            "command_chats" => "لیست تمام چت‌های ردیابی شده",
            "command_stats" => "نمایش آمار ربات",
            "command_permissions" => "مشاهده و مدیریت مجوزها",

            // Other texts
            "start_welcome" => "سلام و خوش آمدید به این ربات! برای دیدن اطلاعات بیشتر /help را بزنید.",
            "help_available" => "دستورات موجود:",
            "help_show" => "نمایش راهنما",
            "echo_on" => "اکو روشن است",
            "echo_off" => "اکو خاموش است",
            "echo_toggle_on" => "روشن کردن اکو",
            "echo_toggle_off" => "خاموش کردن اکو",
            "users_no_tracked" => "هنوز هیچ کاربری ردیابی نشده است.",
            "users_tracked" => "**کاربران ردیابی شده:**",
            "users_more" => "...و {0} کاربر دیگر.",
            "chats_no_tracked" => "هنوز هیچ چتی ردیابی نشده است.",
            "chats_tracked" => "**چت‌های ردیابی شده:**",
            "chats_more" => "...و {0} چت دیگر.",
            "permissions_management" => "**مدیریت مجوزها**",
            "permissions_no_access" => "⚠️ من اجازه مدیریت کاربران در این چت را ندارم.",
            "permissions_bot_status" => "وضعیت من: {0}",
            "permissions_need_admin" => "برای مدیریت مجوزها، من باید مدیر با مجوزهای مناسب باشم.",
            "permissions_for_user" => "**مجوزهای {0}**",
            "permissions_admin" => "مدیر: {0}",
            "permissions_can_post" => "ارسال پیام: {0}",
            "permissions_can_edit" => "ویرایش پیام: {0}",
            "permissions_can_delete" => "حذف پیام: {0}",
            "permissions_can_restrict" => "محدود کردن کاربران: {0}",
            "permissions_can_promote" => "ارتقای کاربران: {0}",
            "permissions_can_change_info" => "تغییر اطلاعات: {0}",
            "permissions_can_invite" => "دعوت کاربران: {0}",
            "permissions_can_pin" => "سنجاق کردن پیام: {0}",
            "permissions_promote" => "ارتقا به مدیر",
            "permissions_demote" => "تنزل به کاربر عادی",
            "permissions_edit_user" => "ویرایش مجوزهای کاربر",
            "permissions_back_users" => "بازگشت به لیست کاربران",
            "permissions_back_chats" => "بازگشت به لیست چت‌ها",
            "permissions_user_not_found" => "کاربر یافت نشد.",
            "permissions_promote_success" => "کاربر با موفقیت به مدیر ارتقا یافت.",
            "permissions_promote_failed" => "ارتقای کاربر ناموفق بود. مطمئن شوید که من مجوزهای لازم را دارم.",
            "permissions_demote_success" => "کاربر با موفقیت به کاربر عادی تنزل یافت.",
            "permissions_demote_failed" => "تنزل کاربر ناموفق بود. مطمئن شوید که من مجوزهای لازم را دارم.",
            "permissions_restrict_success" => "محدودیت‌های کاربر با موفقیت به‌روزرسانی شد.",
            "permissions_restrict_failed" => "به‌روزرسانی محدودیت‌های کاربر ناموفق بود. مطمئن شوید که من مجوزهای لازم را دارم.",
            "permissions_users_in_chat" => "**کاربران در این چت:**",
            "permissions_no_users" => "هیچ کاربری با مجوز در این چت ردیابی نشده است.",
            _ => key
        };
    }

    private static string GetEnglishCommandText(string key)
    {
        return key switch
        {
            // Command descriptions
            "command_start" => "Show welcome message",
            "command_help" => "Display help information",
            "command_echo" => "Toggle message echoing",
            "command_language" => "Toggle between English and Persian",
            "command_users" => "List all tracked users",
            "command_chats" => "List all tracked chats",
            "command_stats" => "Show bot statistics",
            "command_permissions" => "View and manage user permissions",

            // Other texts
            "start_welcome" => "Hello and welcome to this bot! To see more information do /help.",
            "help_available" => "Available commands:",
            "help_show" => "Show Help",
            "echo_on" => "Echo is ON",
            "echo_off" => "Echo is OFF",
            "echo_toggle_on" => "Toggle Echo On",
            "echo_toggle_off" => "Toggle Echo Off",
            "users_no_tracked" => "No users have been tracked yet.",
            "users_tracked" => "**Tracked Users:**",
            "users_more" => "...and {0} more users.",
            "chats_no_tracked" => "No chats have been tracked yet.",
            "chats_tracked" => "**Tracked Chats:**",
            "chats_more" => "...and {0} more chats.",
            "permissions_management" => "**Permissions Management**",
            "permissions_no_access" => "⚠️ I don't have permission to manage users in this chat.",
            "permissions_bot_status" => "My status: {0}",
            "permissions_need_admin" => "To manage permissions, I need to be an administrator with appropriate permissions.",
            "permissions_for_user" => "**Permissions for {0}**",
            "permissions_admin" => "Admin: {0}",
            "permissions_can_post" => "Can Post Messages: {0}",
            "permissions_can_edit" => "Can Edit Messages: {0}",
            "permissions_can_delete" => "Can Delete Messages: {0}",
            "permissions_can_restrict" => "Can Restrict Members: {0}",
            "permissions_can_promote" => "Can Promote Members: {0}",
            "permissions_can_change_info" => "Can Change Info: {0}",
            "permissions_can_invite" => "Can Invite Users: {0}",
            "permissions_can_pin" => "Can Pin Messages: {0}",
            "permissions_promote" => "Promote to Admin",
            "permissions_demote" => "Demote to Regular User",
            "permissions_edit_user" => "Edit User Permissions",
            "permissions_back_users" => "Back to User List",
            "permissions_back_chats" => "Back to Chat List",
            "permissions_user_not_found" => "User not found.",
            "permissions_promote_success" => "User was successfully promoted to admin.",
            "permissions_promote_failed" => "Failed to promote user. Make sure I have the right permissions.",
            "permissions_demote_success" => "User was successfully demoted to regular user.",
            "permissions_demote_failed" => "Failed to demote user. Make sure I have the right permissions.",
            "permissions_restrict_success" => "User restrictions were successfully updated.",
            "permissions_restrict_failed" => "Failed to update user restrictions. Make sure I have the right permissions.",
            "permissions_users_in_chat" => "**Users in this chat:**",
            "permissions_no_users" => "No tracked users with permissions in this chat.",
            _ => key
        };
    }
} 