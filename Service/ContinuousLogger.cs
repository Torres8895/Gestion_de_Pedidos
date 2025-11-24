using Gestion_de_Pedidos.DataBase;
using Gestion_de_Pedidos.Models;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using System.Text;

namespace Gestion_de_Pedidos.Service
{
    public class ContinuousLogger
    {
        private readonly string _logDirectory;
        private readonly ApplicationDbContext _context;
        private static readonly SemaphoreSlim _semaphore = new(1, 1);
        private static readonly ConcurrentDictionary<string, LogEntry> _logsEnProceso = new();

        public ContinuousLogger(IConfiguration configuration, ApplicationDbContext context)
        {
            _context = context;
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
                // Siempre guardamos en TXT 
                try
                {
                    await EscribirLogCompletoAsync(logEntry);
                }
                catch (Exception ex)
                {
                    // Si falla el TXT, al menos intentamos registrarlo en consola
                    Console.WriteLine($"ERROR al escribir log en TXT: {ex.Message}");
                }

                // Guardamos en BD (esto es opcional/secundario)
                try
                {
                    await GuardarEnBaseDeDatosAsync(logEntry);
                }
                catch (Exception ex)
                {
                    // Si falla la BD, NO queremos que rompa la respuesta al usuario
                    Console.WriteLine($"ERROR al guardar log en BD: {ex.Message}");

                    // Escribimos el error en el mismo archivo de log
                    await EscribirErrorEnLogAsync($"Error al guardar log en BD: {ex.Message}");
                }
            }
        }

        private async Task EscribirErrorEnLogAsync(string error)
        {
            try
            {
                var nombreArchivo = $"log_errores_{DateTime.Now:yyyyMMdd}.txt";
                var rutaCompleta = Path.Combine(_logDirectory, nombreArchivo);
                await File.AppendAllTextAsync(
                    rutaCompleta,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {error}{Environment.NewLine}",
                    Encoding.UTF8
                );
            }
            catch
            {
                // Si ni siquiera podemos escribir el error, no hacemos nada
            }
        }

        private async Task EscribirLogCompletoAsync(LogEntry log)
        {
            var logLine = new StringBuilder();

            logLine.Append($"{log.Fecha:yyyy-MM-dd HH:mm:ss}");
            logLine.Append($" | Llamada: {log.Entidad}");
            logLine.Append($" | IP: {log.Ip}");
            logLine.Append($" | Metodo: {log.Metodo}");
            logLine.Append($" | Status: {log.StatusCode}");
            logLine.Append($" | Headers: {LimpiarTexto(log.Headers)}");

            if (!string.IsNullOrEmpty(log.ErrorController))
                logLine.Append($" | Error Controller: {LimpiarTexto(log.ErrorController)}");

            if (!string.IsNullOrEmpty(log.DatosServicio))
                logLine.Append($" |Mensaje Servicio: {LimpiarTexto(log.DatosServicio)}");

            if (!string.IsNullOrEmpty(log.SqlQuery))
                logLine.Append($" | SQL: {LimpiarTexto(log.SqlQuery)}");

            if (!string.IsNullOrEmpty(log.ResultadoServicio))
                logLine.Append($" | Resultado: {LimpiarTexto(log.ResultadoServicio)}");

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

        private async Task GuardarEnBaseDeDatosAsync(LogEntry log)
        {
            _context.Logs.Add(log);
            await _context.SaveChangesAsync();
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
