using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Diagnostics;

//Cambia el namespace a App2_Chat.Services en la App2
namespace App2_Chat.Services
{
    public class RabbitService
    {
        private IConnection _connection;
        private IChannel _channel;
        private string _queuePublish;
        private string _queueConsume;

        public event Action<string> OnMessageReceived;

        public async Task InitializeAsync(string hostName, string queuePublish, string queueConsume)
        {
            _queuePublish = queuePublish;
            _queueConsume = queueConsume;

            try
            {
                var factory = new ConnectionFactory { HostName = hostName };
                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                // Declarar ambas colas para asegurar existencia
                await _channel.QueueDeclareAsync(queue: _queuePublish, durable: false, exclusive: false, autoDelete: false, arguments: null);
                await _channel.QueueDeclareAsync(queue: _queueConsume, durable: false, exclusive: false, autoDelete: false, arguments: null);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += Consumer_ReceivedAsync;

                await _channel.BasicConsumeAsync(queue: _queueConsume, autoAck: true, consumer: consumer);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error RabbitMQ Init: {ex.Message}");
                throw;
            }
        }

        private Task Consumer_ReceivedAsync(object sender, BasicDeliverEventArgs @event)
        {
            var body = @event.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            OnMessageReceived?.Invoke(message);
            return Task.CompletedTask;
        }

        public async Task SendMessageAsync(string message)
        {
            if (_channel == null) return;

            var body = Encoding.UTF8.GetBytes(message);
            await _channel.BasicPublishAsync(exchange: "", routingKey: _queuePublish, body: body);
        }

        public async Task DisposeAsync()
        {
            if (_channel != null) await _channel.CloseAsync();
            if (_connection != null) await _connection.CloseAsync();
        }
    }
}
