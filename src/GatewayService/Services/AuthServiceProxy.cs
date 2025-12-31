using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GatewayService.Services
{
    public class AuthServiceProxy
    {
        private readonly HttpClient _httpClient;
        private readonly string _validateUrl;

        public AuthServiceProxy(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _validateUrl = configuration["AuthServiceUrl"] ?? throw new InvalidOperationException("AuthServiceUrl não configurada.");
        }

        public async Task<(bool isValid, string Email, string Role)> ValidateTokenAsync(string token)
        {
            var content = new StringContent(
              JsonSerializer.Serialize(token),
              Encoding.UTF8,
              "application/json"
          );

            try
            {
                var response = await _httpClient.PostAsync(_validateUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseBody);
                    var root = doc.RootElement;

                    return (true, root.GetProperty("email").GetString()!, root.GetProperty("role").GetString()!);
                }
                else
                {
                    // Log de erro 401/400
                    var errorBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[DEBUG - AUTH PROXY] Falha na validação. Status: {response.StatusCode}. Body: {errorBody}");
                }
            }
            catch (Exception ex) // CAPTURA QUALQUER EXCEÇÃO
            {
                // ADICIONE ESTE LOG DE ERRO GENÉRICO
                Console.WriteLine($"[DEBUG - AUTH PROXY] Erro GERAL na Requisição HTTP: {ex.Message}");
                return (false, string.Empty, string.Empty);
            }

            return (false, string.Empty, string.Empty);
        }
    }
}