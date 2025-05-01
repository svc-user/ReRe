using System.Reflection;
using RabbitMQ.Client;
using ReRe.Handlers;
using ReRe.Interfaces;
using ReRe.Models;

namespace ReRe.Services;

public class ConnectionMultiplexer
{
    private readonly ConnectionFactory _connectionFactory = new();
    private readonly RabidOptions _options = new();

    private IConnection _connection = null!;
    public IChannel Channel { get; private set; }
    private GlobalRequestHandler _globalRequestHandler = null!;

    public ConnectionMultiplexer(Action<ConnectionFactory, RabidOptions> setup) => ConnectionMultiplexerAsync(setup).GetAwaiter().GetResult();

    private async Task ConnectionMultiplexerAsync(Action<ConnectionFactory, RabidOptions> setup, params Assembly[] assemblies)
    {
        setup(_connectionFactory, _options);

        _connection = await _connectionFactory.CreateConnectionAsync();
        Channel = await _connection.CreateChannelAsync();
        _globalRequestHandler = new GlobalRequestHandler(Channel);

        _connection.ConnectionShutdownAsync += async (sender, evt) =>
        {
            _connection = await _connectionFactory.CreateConnectionAsync();
            Channel = await _connection.CreateChannelAsync();

            _globalRequestHandler ??= null!;
            _globalRequestHandler = new GlobalRequestHandler(Channel);
        };
    }
}