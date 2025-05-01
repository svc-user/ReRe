using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ReRe.Helpers;
using ReRe.Interfaces;
using ReRe.Models;

namespace ReRe.Services;

internal class ConsumerHandler
{
    private readonly ConnectionMultiplexer _connectionMultiplexer;
    private List<string> _consumerTags = new List<string>();
    private static List<Type> _registeredConsumers = new List<Type>();

    public static void RegisterConsumers(IServiceCollection serviceCollection, params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var types = assembly
                .ExportedTypes
                .Where(t =>
                    t.GetInterfaces()
                        .Any(i =>
                            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
                        )
                );

            foreach (var type in types)
            {
                _registeredConsumers.Add(type);
                serviceCollection.AddSingleton(type);
            }
        }
    }

    public ConsumerHandler(ConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task StartConsumers(IServiceProvider serviceProvider)
    {
        foreach (var registeredConsumer in _registeredConsumers)
        {
            await StartSingleConsumer(registeredConsumer, serviceProvider);
        }
    }

    private async Task StartSingleConsumer(Type type, IServiceProvider serviceProvider)
    {
        var genArgs = type
            .GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
            .SelectMany(i => i.GetGenericArguments())
            .ToList();

        var t = genArgs[0];
        var r1 = genArgs[1];
        var msgType = typeof(MessageContext<>).MakeGenericType(t);

        var inst = serviceProvider.GetRequiredService(type);

        var consumer = new AsyncEventingBasicConsumer(_connectionMultiplexer.Channel);
        consumer.ReceivedAsync += async (_, evt) =>
        {
            var msg = JsonSerializer.Deserialize(evt.Body.ToArray(), msgType);
            var resTask = (dynamic)type.GetMethod("Handle").Invoke(inst, new[] { msg });
            var res = await resTask as object;

            await _connectionMultiplexer.Channel.BasicAckAsync(evt.DeliveryTag, false);

            var respQueue = evt.BasicProperties.ReplyTo;
            var respMsg = new MessageContext<object>();
            respMsg.Header = new();
            respMsg.Payload = res;

            var basicProps = new BasicProperties();
            basicProps.Expiration = "30000";
            basicProps.CorrelationId = evt.BasicProperties.CorrelationId;

            using MemoryStream jsonStream = new MemoryStream();
            await JsonSerializer.SerializeAsync(jsonStream, respMsg);

            await _connectionMultiplexer.Channel.BasicPublishAsync(respQueue, string.Empty, false, basicProps, jsonStream.ToArray());
        };

        var queueName = QueueHelper.GetQueueName(t);
        await QueueHelper.EnsureQueueExists(queueName, _connectionMultiplexer.Channel);
        var consumerTag = await _connectionMultiplexer.Channel.BasicConsumeAsync(queueName, false, consumer);
        _consumerTags.Add(consumerTag);
    }
}