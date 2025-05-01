namespace ReRe.Models;

public class MessageContext<T> 
{
    public  MessageHeader Header { get; set; }
    public T? Payload { get; set; }
}