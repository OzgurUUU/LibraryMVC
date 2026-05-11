using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace LibraryMVC.Helpers
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                              ?? "Server=localhost;Database=LibraryDB;Trusted_Connection=True;TrustServerCertificate=True;";
        }

        // Controller'ların bağlantı açması için genel metot
        public SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}