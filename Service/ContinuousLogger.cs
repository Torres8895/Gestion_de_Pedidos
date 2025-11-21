using System.Text;
using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Gestion_de_Pedidos.Models;

namespace Gestion_de_Pedidos.Service
{
    public class ContinuousLogger
    {
        private readonly string _logDirectory;
        private static readonly SemaphoreSlim _semaphore = new(1, 1);
        private static readonly ConcurrentDictionary<string, LogEntry> _logsEnProceso = new();

        public ContinuousLogger(IConfiguration configuration)
        {
            _logDirectory = configuration["Logging:LogDirectory"] ?? "Logs";

            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public string IniciarLog(DateTime fecha, string entidad, string ip, string metodo,
            string headers, int statusCode)
        {
            var logEntry = new LogEntry
            {
                Fecha = fecha,
                Entidad = entidad,
                Ip = ip,
                Metodo = metodo,
                Headers = headers,
                StatusCode = statusCode
            };

            _logsEnProceso.TryAdd(logEntry.Id, logEntry);

            return logEntry.Id;
        }

        public Task CompletarLogDesdeServicio(string logId, string datosServicio,
            string? sqlQuery = null, string? resultado = null)
        {
            if (_logsEnProceso.TryGetValue(logId, out var logEntry))
            {
                logEntry.DatosServicio = datosServicio;

                if (!string.IsNullOrEmpty(sqlQuery))
                {
                    logEntry.SqlQuery = sqlQuery;
                }

                logEntry.ResultadoServicio = resultado;
            }

            return Task.CompletedTask;
        }

        // NUEVO: Para registrar errores del controller
        public Task RegistrarErrorController(string logId, string error)
        {
            if (_logsEnProceso.TryGetValue(logId, out var logEntry))
            {
                logEntry.ErrorController = error;
            }

            return Task.CompletedTask;
        }

        public async Task FinalizarLog(string logId)
        {
            if (_logsEnProceso.TryRemove(logId, out var logEntry))
            {
                await EscribirLogCompletoAsync(logEntry);
            }
        }

        private async Task EscribirLogCompletoAsync(LogEntry log)
        {
            var logLine = new StringBuilder();

            logLine.Append($"{log.Fecha:yyyy-MM-dd HH:mm:ss}");
            logLine.Append($" | Entidad: {log.Entidad}");
            logLine.Append($" | IP: {log.Ip}");
            logLine.Append($" | Método: {log.Metodo}");
            logLine.Append($" | Status: {log.StatusCode}");
            logLine.Append($" | Headers: {LimpiarTexto(log.Headers)}");

            // Error en controller (validaciones, etc)
            if (!string.IsNullOrEmpty(log.ErrorController))
            {
                logLine.Append($" | Error Controller: {LimpiarTexto(log.ErrorController)}");
            }

            // Datos del servicio
            if (!string.IsNullOrEmpty(log.DatosServicio))
            {
                logLine.Append($" | Servicio: {LimpiarTexto(log.DatosServicio)}");
            }

            // SQL capturado
            if (!string.IsNullOrEmpty(log.SqlQuery))
            {
                logLine.Append($" | SQL: {LimpiarTexto(log.SqlQuery)}");
            }

            // Resultado
            if (!string.IsNullOrEmpty(log.ResultadoServicio))
            {
                logLine.Append($" | Resultado: {LimpiarTexto(log.ResultadoServicio)}");
            }

            await _semaphore.WaitAsync();

            try
            {
                var nombreArchivo = $"log_{DateTime.Now:yyyyMMdd}.txt";
                var rutaCompleta = Path.Combine(_logDirectory, nombreArchivo);

                await File.AppendAllTextAsync(
                    rutaCompleta,
                    logLine.ToString() + Environment.NewLine + Environment.NewLine,
                    Encoding.UTF8
                );
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private string LimpiarTexto(string texto)
        {
            if (string.IsNullOrEmpty(texto))
                return string.Empty;

            return texto
                .Replace(Environment.NewLine, " ")
                .Replace("\n", " ")
                .Replace("\r", " ")
                .Replace("\t", " ");
        }
    }
}
