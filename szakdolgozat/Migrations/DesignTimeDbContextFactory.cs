using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Data.SqlClient;
using Azure.Identity;
using DotNetEnv;

namespace szakdolgozat.Migrations
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AssetDbContext>
    {
        private string _tenantId;
        private string _connectionString;

        public AssetDbContext CreateDbContext(string[] args)
        {
            Env.TraversePath().Load(".env");

            _connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
            _tenantId = Environment.GetEnvironmentVariable("TENANT_ID");

            var optionsBuilder = new DbContextOptionsBuilder<AssetDbContext>();
            var credential = new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
            {
                TenantId = _tenantId
            });
            var connectionString = _connectionString;
            var token = credential.GetTokenAsync(
                new Azure.Core.TokenRequestContext(new[] { "https://database.windows.net/.default" })
            ).Result;
            var connection = new SqlConnection(connectionString);
            connection.AccessToken = token.Token;
            optionsBuilder.UseSqlServer(connection);
            return new AssetDbContext(optionsBuilder.Options);
        }
    }
}
