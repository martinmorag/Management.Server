// ManagementContextFactory.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Management.Server.Data
{
    public class ManagementContextFactory : IDesignTimeDbContextFactory<ManagementContext>
    {
        public ManagementContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<ManagementContextFactory>()
                .Build();

            // Get the connection string named "DefaultConnectionString"
            var connectionString = configuration.GetConnectionString("DefaultConnectionString");

            // You might want to add a check here to ensure the connection string isn't null or empty
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "The 'DefaultConnectionString' is not set. Ensure it's in appsettings.json or as an environment variable."
                );
            }

            // Configure DbContextOptions to use PostgreSQL with your connection string
            var optionsBuilder = new DbContextOptionsBuilder<ManagementContext>();
            optionsBuilder.UseNpgsql(connectionString);

            // Create and return a new instance of your DbContext
            return new ManagementContext(optionsBuilder.Options);
        }
    }
}