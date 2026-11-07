using BeasiswaIn.Web.Entities;
using BeasiswaIn.Web.Infrastructures.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BeasiswaIn.Web.Interfaces;

public interface IApplicationDbContext
{
    internal DbSet<ApplicationUser> ApplicationUsers { get; }
    internal DbSet<Scholarship> Scholarships { get; }
    internal DbSet<ScholarshipCategory> ScholarshipCategories { get; }
    internal DbSet<ScholarshipCategoryAssignments> ScholarshipCategoryAssignments { get; }
    internal DbSet<UserScholarshipPreferences> UserScholarshipPreferences { get; }
    internal DbSet<AuditLog> AuditLogs { get; }
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct);
}