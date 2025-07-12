
namespace AntiSpamBot.Models;
public class MessageInfo
{
  public DateTime Timestamp { get; set; }
  public bool HasAttachments { get; set; }
  public int AttachmentCount { get; set; }
}