using RabbitMQ.Client;

namespace ReRe.Helpers;

public class QueueHelper
{
    public static string GetQueueName<T>()
    {
        return GetQueueName(typeof(T));
    }

    public static string GetQueueName(Type type)
    {
        var typeQueue = NormalizeQueueName($"{type.FullName}");
        return typeQueue;
    }

    private static string NormalizeQueueName(string name)
    {
        return name;
    }

    private static readonly HashSet<string> _existingQueues = new HashSet<string>();

    public static async Task EnsureQueueExists(string queue, IChannel channel)
    {
        if (_existingQueues.Contains(queue)) return;

        await channel.QueueDeclareAsync(queue, exclusive: false, autoDelete: false, durable: true);
        await channel.ExchangeDeclareAsync(queue, "fanout", durable: true, autoDelete: false);
        await channel.QueueBindAsync(queue, queue, string.Empty);

        _existingQueues.Add(queue);
    }
}