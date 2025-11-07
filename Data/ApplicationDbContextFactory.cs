using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace StudyPlannerApi.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            
            // Build configuration to read from appsettings.json and user secrets
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddUserSecrets<ApplicationDbContextFactory>()
                .AddEnvironmentVariables()
                .Build();
            
            // Try to get connection string from configuration
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            // If not found in DefaultConnection, try RenderPostgreSQL (from user secrets)
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = configuration.GetConnectionString("RenderPostgreSQL");
            }
            
            // Convert PostgreSQL URI format to standard format if needed
            if (!string.IsNullOrEmpty(connectionString) && connectionString.StartsWith("postgresql://"))
            {
                connectionString = ConvertPostgresUriToConnectionString(connectionString);
            }
            
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Database connection string is not configured. Please set it in User Secrets or environment variables.");
            }
            
            optionsBuilder.UseNpgsql(connectionString);

            return new ApplicationDbContext(optionsBuilder.Options);
        }
        
        private string ConvertPostgresUriToConnectionString(string uri)
        {
            var uriBuilder = new UriBuilder(uri);
            var host = uriBuilder.Host;
            var port = uriBuilder.Port > 0 ? uriBuilder.Port : 5432;
            var database = uriBuilder.Path.TrimStart('/');
            var username = uriBuilder.UserName;
            var password = uriBuilder.Password;
            
            return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
        }
    }
}
