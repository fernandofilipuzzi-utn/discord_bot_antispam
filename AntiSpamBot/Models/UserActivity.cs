
namespace AntiSpamBot.Models;
public class UserActivity
{
    public List<MessageInfo> RecentMessages { get; set; } = new List<MessageInfo>();
    public int ViolationCount { get; set; } = 0;
}