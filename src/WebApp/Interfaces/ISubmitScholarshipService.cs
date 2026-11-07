namespace BeasiswaIn.Web.Interfaces;

public interface ISubmitScholarshipService
{
    public Task SubmitAsync(CancellationToken cancellationToken);
    public Task ValidateAsync(CancellationToken cancellationToken);
    public Task GetAllAsync(CancellationToken cancellationToken);
    public Task GetByIdAsync(CancellationToken cancellationToken);
}