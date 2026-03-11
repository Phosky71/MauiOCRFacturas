using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;

namespace MauiOCRFacturas.Services;

public class ServiceBusService : IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;

    public ServiceBusService(IConfiguration configuration)
    {
        var connectionString = configuration["AzureServiceBus:ConnectionString"]!;
        var cola = configuration["AzureServiceBus:Cola"]!;
        _client = new ServiceBusClient(connectionString);
        _sender = _client.CreateSender(cola);
    }

    public async Task EnviarMensajeAsync(string mensaje)
    {
        await _sender.SendMessageAsync(new ServiceBusMessage(mensaje));
    }

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
    }
}