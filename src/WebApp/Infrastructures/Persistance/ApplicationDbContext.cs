using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Entities;

namespace BeasiswaIn.Web.Infrastructures.Persistance;

internal sealed class ApplicationDbContext 
    : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Scholarship> Scholarships => Set<Scholarship>();
    public DbSet<ScholarshipCategory> ScholarshipCategories => Set<ScholarshipCategory>();
    public DbSet<ScholarshipCategoryAssignments> ScholarshipCategoryAssignments => Set<ScholarshipCategoryAssignments>();
    public DbSet<UserScholarshipPreferences> UserScholarshipPreferences => Set<UserScholarshipPreferences>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base (options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        foreach(var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(IDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(IDeletable.IsDeleted));
                var condition = Expression.Equal(property, Expression.Constant(false));
                var lambda = Expression.Lambda(condition, parameter);

                builder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }
    
    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct)
    {
        return Database.BeginTransactionAsync(ct);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is IDeletable && entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Property(nameof(IDeletable.IsDeleted)).CurrentValue = true;
                entry.Property(nameof(IDeletable.DeletedAt)).CurrentValue = DateTimeOffset.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}