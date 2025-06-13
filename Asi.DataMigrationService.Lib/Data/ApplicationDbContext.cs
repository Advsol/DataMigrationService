using Asi.DataMigrationService.Lib.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Asi.DataMigrationService.Lib.Data
{
    /// <summary>   An application database context. </summary>
    public class ApplicationDbContext : DbContext
    {
        /// <summary>   Constructor. </summary>
        ///
        /// <param name="options">  Options for controlling the operation. </param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            Database.SetCommandTimeout(300);
        }

        public DbSet<Tenant> Tenants { get; set; }

        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectDataSource> ProjectDataSources { get; set; }
        public DbSet<ProjectImport> ProjectImports { get; set; }
        public DbSet<ProjectImportData> ProjectImportDatas { get; set; }
        public DbSet<ProjectJob> ProjectJobs { get; set; }
        public DbSet<ProjectJobMessage> ProjectJobMessages { get; set; }
        public DbSet<ImportMap> ImportMaps { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Project>(b =>
            {
                b.Property(e => e.ProjectId)
                .HasValueGenerator<ShortGuidValueGenerator>();
            });
            modelBuilder.Entity<Project>(d =>
            {
                d.HasIndex(p => new { p.TenantId, p.Name })
                .IsUnique();
            });
            modelBuilder.Entity<ProjectDataSource>(d =>
            {
                d.HasIndex(p => new { p.ProjectId, p.DataSourceType , p.Name })
                .IsUnique();
            });
            modelBuilder.Entity<ProjectImport>(d =>
            {
                d.HasIndex(p => new { p.ProjectDataSourceId, p.Name })
               .IsUnique();
            });
            modelBuilder.Entity<ProjectImportData>(d =>
            {
                d.HasIndex(p => new { p.ProjectImportId, p.RowNumber })
                .IsUnique();
            });
            modelBuilder.Entity<ImportMap>(d =>
            {
                d.HasIndex(p => new { p.TenantId, p.Name })
               .IsUnique();
            });
        }
    }
}
