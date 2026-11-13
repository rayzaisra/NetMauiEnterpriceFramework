using Microsoft.EntityFrameworkCore;

using __PROJECT__.Domain.Entities;

namespace __PROJECT__.Infrastructure.Data;

public class AuditLogDbContext : DbContext
{
    public AuditLogDbContext(DbContextOptions<AuditLogDbContext> options)
        : base(options)
    {
    }

    public DbSet<tb_audit_trail> tb_audit_trail => Set<tb_audit_trail>();
    public DbSet<tb_audit_trail_detail> tb_audit_trail_detail => Set<tb_audit_trail_detail>();
    public DbSet<tb_log> tb_log => Set<tb_log>();
    public DbSet<tb_log_activity> tb_log_activity => Set<tb_log_activity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<tb_audit_trail>().HasKey(e => e.id);
        modelBuilder.Entity<tb_audit_trail_detail>()
            .HasKey(e => new { e.tb_audit_trail_id, e.column_name });
        modelBuilder.Entity<tb_log>().HasKey(e => e.id);

        modelBuilder.Entity<tb_log_activity>(entity =>
        {
            entity.ToTable("tb_log_activity");
            entity.HasKey(e => e.id);
            entity.Property(e => e.id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.created_date);
            entity.HasIndex(e => new { e.user_id, e.created_date });
            entity.HasIndex(e => new { e.status_code, e.created_date });
            entity.HasIndex(e => e.correlation_id);
        });
    }
}
