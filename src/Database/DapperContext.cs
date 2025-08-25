using Npgsql;

namespace ApiDiscount.Database
{
    public sealed class DapperContext
    {
        private readonly string _connectionString;

        public DapperContext(string connectionString) => _connectionString = connectionString;

        public NpgsqlConnection CreateConnection() => new NpgsqlConnection(_connectionString);
    }
}
