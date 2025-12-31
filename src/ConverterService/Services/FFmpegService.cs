using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ConverterService.Services
{
    public class FFmpegService
    {
        private readonly ILogger<FFmpegService> _logger;

        public FFmpegService(ILogger<FFmpegService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Converte um arquivo de vídeo (MP4) para áudio (MP3) usando FFmpeg.
        /// </summary>
        /// <param name="inputPath">Caminho completo para o arquivo de entrada (MP4).</param>
        /// <param name="outputPath">Caminho completo para o arquivo de saída (MP3).</param>
        /// <returns>True se a conversão for bem-sucedida, caso contrário, false.</returns>

        public async Task<bool> ConvertToMp3Async(string inputPath, string outputPath)
        {
            var arguments = $"-i \"{inputPath}\" -vn -ab 192k -y \"{outputPath}\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _logger.LogInformation("Iniciando conversão FFmpeg: {Arguments}", arguments);

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                _logger.LogInformation("Conversão concluída com sucesso. Saída: {Output}", output);
                return true;

            }
            else
            {
                _logger.LogError("Falha na conversão. Erro: {Error}", error);
                return false;
            }
        }
    }
}