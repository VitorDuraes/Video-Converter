using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace GatewayService.Services
{
    public class MessageService : IDisposable
    {

        private readonly IConnection _connection;
        private readonly IModel _channel;
        private const string VideoQueueName = "video_queue";

        public MessageService(IConfiguration configuration)
        {
            var factory = new ConnectionFactory()
            {
                HostName = configuration.GetConnectionString("RabbitMqHost"),
                UserName = "user",
                Password = "password"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declara a fila para garantir que ela exista
            _channel.QueueDeclare(
                queue: VideoQueueName,
                durable: true, // A fila persiste após o restart do RabbitMQ
                exclusive: false,
                autoDelete: false,
                arguments: null
            );
        }

        public void PublishVideoUpload(string fileId, string userEmail)
        {
            var message = new
            {
                FileId = fileId,
                UserEmail = userEmail
            };

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            _channel.BasicPublish(
                exchange: "", // Usamos a exchange padrão (default)
                routingKey: VideoQueueName,
                basicProperties: null,
                body: body
            );
        }

        public void Dispose()
        {
            _channel.Dispose();
            _connection.Dispose();
        }
    }
}