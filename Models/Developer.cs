namespace Telegramski_Botski.Models;

public class Developer
{
    public string Id { get; set; }
    public string FullName { get; set; }
    public string Username { get; set; }
    public string Bio { get; set; }
    public List<string> Skills { get; set; }
    public Dictionary<string, string> Links { get; set; }
    public string ProfilePhoto { get; set; }
}
