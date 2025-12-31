using System.Text;
using System.Text.Json;
using ConverterService.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ConverterService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly FFmpegService _ffmpegService;

    private readonly StorageService _storageService;

    private IConnection _connection;
    private IModel _channel;
    private const string VideoQueueName = "video_queue";


    public Worker(ILogger<Worker> logger, IConfiguration configuration, FFmpegService ffmpegService, StorageService storageService)
    {
        _logger = logger;
        _configuration = configuration;
        _ffmpegService = ffmpegService;
        _storageService = storageService;

        InitializeRabbitMq();
    }

    private void InitializeRabbitMq()
    {
        try
        {
            var factory = new ConnectionFactory()
            {
                HostName = _configuration.GetConnectionString("RabbitMqHost"),
                UserName = "user",
                Password = "password"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(
                queue: VideoQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            _logger.LogInformation("RabbitMQ conectado e fila '{Queue}' declarada.", VideoQueueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Não foi possível conectar ao RabbitMQ.");

            throw;
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            _logger.LogInformation("Mensagem recebida: {Message}", message);
            try
            {
                var data = JsonSerializer.Deserialize<Dictionary<string, string>>(message);
                var fileId = data["FileId"];
                var userEmail = data["UserEmail"];

                _logger.LogInformation("Iniciando conversão para FileId: {FileId}", fileId);


                using var videoStream = await _storageService.DownloadVideoAsync(fileId);
                if (videoStream == null)
                {
                    _logger.LogError("Arquivo de vídeo não encontrado no MongoDB: {FileId}", fileId);
                    _channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }


                var tempDir = Path.Combine(Path.GetTempPath(), "converter_temp");
                Directory.CreateDirectory(tempDir);
                var inputPath = Path.Combine(tempDir, $"{fileId}.mp4");
                var outputPath = Path.Combine(tempDir, $"{fileId}.mp3");


                using (var fileStream = File.Create(inputPath))
                {
                    await videoStream.CopyToAsync(fileStream);
                }


                var success = await _ffmpegService.ConvertToMp3Async(inputPath, outputPath);

                if (success)
                {

                    string newFileId;
                    
                    using (var mp3Stream = File.OpenRead(outputPath)) // Bloco using explícito
                    {
                        newFileId = await _storageService.UploadMp3Async(fileId, mp3Stream, userEmail);
                    } 

                    _logger.LogInformation("Conversão concluída. Novo FileId MP3: {NewFileId}", newFileId);

                    File.Delete(inputPath);
                    File.Delete(outputPath);

                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                else
                {
                    _logger.LogError("Falha na conversão do FFmpeg para FileId: {FileId}", fileId);
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem.");
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        _channel.BasicConsume(queue: VideoQueueName, autoAck: false, consumer: consumer);

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}

