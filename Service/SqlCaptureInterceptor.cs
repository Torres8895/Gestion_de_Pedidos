using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
namespace Gestion_de_Pedidos.Service
{
    public class SqlCaptureInterceptor : DbCommandInterceptor
    {
        private static readonly AsyncLocal<List<string>> _currentSqlQueries = new();

        public static void IniciarCaptura()
        {
            _currentSqlQueries.Value = new List<string>();
        }

        public static string ObtenerSqlCapturado()
        {
            var queries = _currentSqlQueries.Value;
            if (queries == null || !queries.Any())
                return string.Empty;

            var resultado = string.Join("; ", queries);
            _currentSqlQueries.Value = null;
            return resultado;
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            CapturarSql(command);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            CapturarSql(command);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override InterceptionResult<int> NonQueryExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result)
        {
            CapturarSql(command);
            return base.NonQueryExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            CapturarSql(command);
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }

        private void CapturarSql(DbCommand command)
        {
            if (_currentSqlQueries.Value != null)
            {
                var sql = command.CommandText;

                if (command.Parameters.Count > 0)
                {
                    var parametros = string.Join(", ", command.Parameters
                        .Cast<DbParameter>()
                        .Select(p => $"{p.ParameterName}={p.Value}"));

                    sql = $"{sql} [{parametros}]";
                }

                _currentSqlQueries.Value.Add(sql);
            }
        }
    }
}
