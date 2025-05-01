namespace ReRe.Models;

public class MessageHeader
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string MessageType { get; set; }
}