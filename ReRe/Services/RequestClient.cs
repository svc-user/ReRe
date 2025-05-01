using System.Text.Json;
using RabbitMQ.Client;
using ReRe.Handlers;
using ReRe.Helpers;
using ReRe.Interfaces;
using ReRe.Models;

namespace ReRe.Services;

public class RequestClient<TReq> : IRequestClient<TReq>
{
    private readonly ConnectionFactory _connectionFactory = new();
    private readonly RabidOptions _options = new();

    private IChannel _channel = null!;

    private GlobalRequestHandler _globalRequestHandler = null!;

    public RequestClient(ConnectionMultiplexer multiplexer) : this(multiplexer.Channel){}
    
    public RequestClient(IChannel channel) => RequestClientAsync(channel).GetAwaiter().GetResult();

    private async Task RequestClientAsync(IChannel channel)
    {
        _channel = channel;

        var sendQueueName = QueueHelper.GetQueueName<TReq>();
        await QueueHelper.EnsureQueueExists(sendQueueName, _channel);
    }
    


    public async Task<TRes> GetResponse<TRes>(TReq request, MessageHeader? header = default) where TRes : class
    {
        var requestQueue = QueueHelper.GetQueueName(typeof(TReq));

        var fullMsg = new MessageContext<TReq>()
        {
            Header = header ?? new(),
            Payload = request,
        };

        fullMsg.Header.MessageType = QueueHelper.GetQueueName<TReq>();

        using MemoryStream jsonStream = new MemoryStream();
        await JsonSerializer.SerializeAsync(jsonStream, fullMsg);

        var basicProps = new BasicProperties();
        basicProps.Expiration = "30000";
        basicProps.ReplyTo = GlobalRequestHandler.BusQueueName;
        basicProps.CorrelationId = Guid.CreateVersion7().ToString();

        var respType = typeof(TRes);
        var completionSource = new TaskCompletionSource<object>();
        var tci = new TaskCompletionInfo()
        {
            ResponseType = respType,
            TaskCompletionSource = completionSource,
        };
        GlobalRequestHandler.CorrelationIds.Add(basicProps.CorrelationId, tci); // ensure response will be read
        await _channel.BasicPublishAsync(requestQueue, String.Empty, false, basicProps, jsonStream.ToArray());

        var respObj = await completionSource.Task;
        var resp = respObj as TRes;
        return resp;
    }
}