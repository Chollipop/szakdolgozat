using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace szakdolgozat.Migrations
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AssetDbContext>
    {
        public AssetDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AssetDbContext>();
            optionsBuilder.UseSqlServer("Server=tcp:assetinventory.database.windows.net,1433;Initial Catalog=assetinventory;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=\"Active Directory Default\";");
            return new AssetDbContext(optionsBuilder.Options);
        }
    }
}
