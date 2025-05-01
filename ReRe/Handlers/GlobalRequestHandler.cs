using System.Text.Json;
using RabbitMQ.Client;
using ReRe.Models;

namespace ReRe.Handlers;

public class TaskCompletionInfo
{
    public Type ResponseType { get; set; }
    public TaskCompletionSource<object> TaskCompletionSource { get; set; }
}

public class GlobalRequestHandler : AsyncDefaultBasicConsumer
{
    public static string BusQueueName;
    public static string ConsumerTag = null!;
    public static readonly IDictionary<string, TaskCompletionInfo> CorrelationIds = new Dictionary<string, TaskCompletionInfo>();

    public GlobalRequestHandler(IChannel channel) : base(channel)
    {
        BusQueueName = "gls_bus_" + Guid.CreateVersion7().ToString();

        channel.QueueDeclareAsync(BusQueueName, exclusive: false, autoDelete: true).GetAwaiter().GetResult();
        channel.ExchangeDeclareAsync(BusQueueName, "fanout", durable: false, autoDelete: true).GetAwaiter().GetResult();
        channel.QueueBindAsync(BusQueueName, BusQueueName, string.Empty).GetAwaiter().GetResult();
        ConsumerTag = channel.BasicConsumeAsync(BusQueueName, false, this).GetAwaiter().GetResult();
    }


    public override async Task HandleBasicDeliverAsync(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IReadOnlyBasicProperties properties,
        ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken = new CancellationToken())
    {
        if (!CorrelationIds.TryGetValue(properties.CorrelationId, out var taskCompletionInfo))
        {
            await base.HandleBasicDeliverAsync(consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body, cancellationToken);
            return;
        }

        var respType = typeof(MessageContext<>).MakeGenericType(taskCompletionInfo.ResponseType);
        var msg = JsonSerializer.Deserialize(body.Span, respType);

        var payload = respType.GetProperty("Payload").GetValue(msg);
        taskCompletionInfo.TaskCompletionSource.SetResult(payload);
        await base.Channel.BasicAckAsync(deliveryTag, false);


        // var consumerReg = ConsumerHandler.Consumers.SingleOrDefault(c => QueueNameHelper.GetQueueName(c.RequestType) == msg?.Header?.MessageType);
        // if (consumerReg is not null)
        // {
        //     var subReqType = typeof(IMessageContext<>).MakeGenericType(consumerReg.RequestType);
        //     var subRespType = typeof(IMessageContext<>).MakeGenericType(consumerReg.ResponseType);
        //
        //     var realMsg = JsonSerializer.Deserialize(body.Span, subReqType);
        //
        //     // var mtInfo = consumerReg.Instance.GetType().GetMethod("Consume", BindingFlags.Instance); //.MakeGenericMethod(consumerReg.RequestType);
        //     // var respTask = (Task)mtInfo.Invoke(consumerReg.Instance, new object[] { realMsg });
        //     // await respTask;
        //     // var resp = respTask.GetType().GetProperty("Result").GetValue(respTask);
        //
        //
        //     //    Task<TRes> Handle(IMessageContext<TReq> message);
        //     var instance = consumerReg.InstanceType.GetMethod("Handle");
        //     var respTask = (Task<object>)instance.Invoke(consumerReg.Instance, new[] { realMsg });
        //
        //     var resp = await respTask;
        //
        //     var respQueue = properties.ReplyTo;
        //     var basicProps = new BasicProperties();
        //     basicProps.Expiration = "30000";
        //
        //     using var memstr = new MemoryStream();
        //     await JsonSerializer.SerializeAsync(memstr, resp, subRespType);
        //     await base.Channel.BasicPublishAsync(respQueue, string.Empty, false, basicProps, memstr.ToArray());
        // }
    }
}