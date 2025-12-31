using GatewayService.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace GatewayService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MediaController : ControllerBase
    {
        private readonly AuthServiceProxy _authService;
        private readonly StorageService _storageService;
        private readonly MessageService _messageService;

        public MediaController(AuthServiceProxy authService, StorageService storageService, MessageService messageService)
        {
            _authService = authService;
            _storageService = storageService;
            _messageService = messageService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login()
        {
            return BadRequest("O login deve ser feito diretamente no AuthService para obter o token JWT.");
        }
        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            // 1. Validação da Autenticação
            var authResult = await AuthenticateAsync();
            if (!authResult.IsValid)
            {
                return Unauthorized(new { message = "Não autorizado. Token JWT inválido ou ausente." });
            }

            // 2. Validação do Arquivo
            if (file == null || file.Length == 0)
            {
                return BadRequest("Nenhum arquivo enviado.");
            }
            if (!file.FileName.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Apenas arquivos MP4 são aceitos para upload.");
            }

            // 3. Armazenamento no MongoDB (GridFS)
            string fileId;
            using (var stream = file.OpenReadStream())
            {
                fileId = await _storageService.UploadVideoAsync(stream, file.FileName);
            }

            // 4. Publicação da Mensagem no RabbitMQ
            _messageService.PublishVideoUpload(fileId, authResult.Email);

            return Ok(new { message = "Upload iniciado. A conversão será processada em segundo plano.", fileId });
        }

        // GET api/media/download?fileId={id}
        [HttpGet("download")]
        public async Task<IActionResult> Download([FromQuery] string fileId)
        {
            // 1. Validação da Autenticação
            var authResult = await AuthenticateAsync();
            if (!authResult.IsValid)
            {
                return Unauthorized(new { message = "Não autorizado. Token JWT inválido ou ausente." });
            }
            var (stream, fileName, contentType) = await _storageService.DownloadFileAsync(fileId);

            if (stream == null)
            {
                return NotFound("Arquivo não encontrado.");
            }

            // 2. Verificar se o MP3 está pronto
            if (contentType != "audio/mpeg")
            {
                return StatusCode(202, new { message = "O arquivo MP3 ainda está em processamento. Tente novamente mais tarde." });
            }

            // 3. Download do MP3
            return File(stream, contentType, fileName);
        }

        // Método auxiliar para extrair e validar o token
        private async Task<(bool IsValid, string Email, string Role)> AuthenticateAsync()
        {
            try
            {
                if (Request.Headers.TryGetValue("Authorization", out var authHeader))
                {
                    var header = AuthenticationHeaderValue.Parse(authHeader.ToString());
                    if (header.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase))
                    {
                        var token = header.Parameter;
                        return await _authService.ValidateTokenAsync(token);
                    }
                }
            }
            catch (Exception ex)
            {
                // LOG DE ERRO AQUI
                Console.WriteLine($"[DEBUG - MEDIA CONTROLLER] Erro de Autenticação: {ex.Message}");
            }
            return (false, string.Empty, string.Empty);
        }
    }
}