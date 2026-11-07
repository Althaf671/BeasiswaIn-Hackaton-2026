using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Models.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace BeasiswaIn.Web.Services.Dashboard;

internal sealed class DashboardService : IDashboardService
{
    private readonly IApplicationDbContext _dbContext;

    public DashboardService(
        IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<Result<AuditLogListResponse>> GetAuditLogListAsync(
        int page = 1, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default)
    {
        var query = 
            from auditLog in _dbContext.AuditLogs.AsNoTracking()
            join user in _dbContext.ApplicationUsers.AsNoTracking()
                on auditLog.ActorUserId equals user.Id
            orderby
                auditLog.OccurredOn,
                auditLog.Action
            select new AuditLogResponse
            {
                Id = auditLog.Id,
                ActorUserNameUnformmated = user.UserName ?? "Unknown",
                Action = auditLog.Action,
                EntityType = auditLog.EntityType,
                Description = auditLog.Description,
                IpAdress = auditLog.IpAddress == null ? "-" : auditLog.IpAddress.ToString(),
                OccuredOn = auditLog.OccurredOn
            };

        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Result<AuditLogListResponse>.Success(
            new AuditLogListResponse
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            });
    }

    public async Task<Result<DashboardResponse>> GetDashboardDataAsync(
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var monthStart = new DateTimeOffset(
            now.Year,
            now.Month,
            1,
            0, 0, 0,
            TimeSpan.Zero
        );

        var closingSoonLimit = now.AddDays(7);
        var weekStart = now.AddDays(-7);

        var scholarships = _dbContext.Scholarships
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        var scholarshipCategories = _dbContext.ScholarshipCategories
            .AsNoTracking()
            .Where(x => x.IsActive);

        var data = await (
            from root in _dbContext.ApplicationUsers
                .AsNoTracking()
                .Take(1)
            select new DashboardResponse
            {
                // ==========================================
                // Registration Deadline Metrics
                // ==========================================

                TotalScholarshipClosingSoon = new MetricResponse
                {
                    Value = scholarships.Count(x =>
                        x.RegistrationEndAt >= now &&
                        x.RegistrationEndAt <= closingSoonLimit)
                },

                TotalExpiredScholarship = new MetricResponse
                {
                    Value = scholarships.Count(x =>
                        x.RegistrationEndAt < now)
                },

                // ==========================================
                // Content Activity Metrics
                // ==========================================

                TotalScholarshipCreatedThisMonth = new MetricResponse
                {
                    Value = scholarships.Count(x =>
                        x.CreatedAt >= monthStart)
                },

                // ==========================================
                // Category Metrics
                // ==========================================

                TotalScholarshipCategory = new MetricResponse
                {
                    Value = scholarshipCategories.Count(),

                    TrendValue = scholarshipCategories.Count(x =>
                        x.CreatedAt >= monthStart)
                }
            }
        ).FirstOrDefaultAsync(cancellationToken);

        return Result<DashboardResponse>.Success(
            data ?? new DashboardResponse()
        );
    }

}