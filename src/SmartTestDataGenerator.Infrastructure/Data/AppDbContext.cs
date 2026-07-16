using Microsoft.EntityFrameworkCore;
using SmartTestDataGenerator.Core.Entities;

namespace SmartTestDataGenerator.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Template> Templates { get; set; } = null!;
        public DbSet<TemplateTable> TemplateTables { get; set; } = null!;
        public DbSet<TemplateColumn> TemplateColumns { get; set; } = null!;
        public DbSet<GenerationHistory> GenerationHistories { get; set; } = null!;
        public DbSet<RecentActivity> RecentActivities { get; set; } = null!;
        public DbSet<Setting> Settings { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Template -> TemplateTable Cascade Delete
            modelBuilder.Entity<Template>()
                .HasMany(t => t.Tables)
                .WithOne(tb => tb.Template)
                .HasForeignKey(tb => tb.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure TemplateTable -> TemplateColumn Cascade Delete
            modelBuilder.Entity<TemplateTable>()
                .HasMany(tb => tb.Columns)
                .WithOne(c => c.Table)
                .HasForeignKey(c => c.TableId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
