using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Models.Dashboard;

namespace BeasiswaIn.Web.Interfaces;

public interface IDashboardService
{
    public Task<Result<AuditLogListResponse>> GetAuditLogListAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    public Task<Result<DashboardResponse>> GetDashboardDataAsync(
        CancellationToken cancellationToken);
}