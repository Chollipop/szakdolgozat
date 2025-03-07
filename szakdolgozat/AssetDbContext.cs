using Microsoft.EntityFrameworkCore;
using szakdolgozat.Models;

namespace szakdolgozat
{
    public class AssetDbContext : DbContext
    {
        public DbSet<Asset> Assets { get; set; }
        public DbSet<AssetType> AssetTypes { get; set; }
        public DbSet<AssetAssignment> AssetAssignments { get; set; }
        public DbSet<AssetLog> AssetLogs { get; set; }
        public DbSet<Subtype> Subtypes { get; set; }

        public AssetDbContext(DbContextOptions<AssetDbContext> options): base(options) { }
    }
}
