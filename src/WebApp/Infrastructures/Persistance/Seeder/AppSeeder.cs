
using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Common.Errors;
using BeasiswaIn.Web.Entities;
using BeasiswaIn.Web.Infrastructures.Identity;
using BeasiswaIn.Web.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace BeasiswaIn.Web.Infrastructures.Persistance.Seeder;

internal class AppSeeder
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IIdentityService _identityService;
    private readonly IConfiguration _configuration;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IDatasetReader _datasetReader;

    private static readonly string[] Roles = [AppRoles.Student];

    public AppSeeder(
        IIdentityService identityService,
        IConfiguration configuration,
        RoleManager<IdentityRole> roleManager,
        IDatasetReader datasetReader,
        IApplicationDbContext dbContext)
    {
        _identityService = identityService;
        _configuration = configuration;
        _roleManager = roleManager;
        _datasetReader = datasetReader;
        _dbContext = dbContext;
    } 

    public async Task<Result> SeedAdminAsync()
    {
        var username = "BeasiswainAdmin";
        var role = AppRoles.Admin;

        var password = _configuration["AdminCredentials:Password"];
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Config 'AdminCredentials:Password' is missing.");

        var email = _configuration["AdminCredentials:Email"];
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("Config 'AdminCredentials:Email' is missing.");
        
        var admin = await _identityService.RegisterUserAccountAsync(
            username: username,
            password: password,
            email: email,
            role: role,
            emailConfirmed: true);
        
        if (admin.IsFailure && admin.Error != AuthenticationErrors.EmailAlreadyExists)
            return Result.Failure(admin.Error);

        return Result.Success;
    }

    public async Task SeedRolesAsync()
    {
        foreach (var role in Roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    public async Task SeedScholarshipsAsync(
        CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Internal", "Datasets", "dataset.csv");
        if (!File.Exists(path))
            throw new FileNotFoundException($"File tidak ditemukan: {path}");

        var scholarships = await _datasetReader.ReadAsync(path, cancellationToken);

        foreach(var items in scholarships)
        {
            var id = Ulid.NewUlid().ToString();
            var scholarship = new Scholarship(
                id: id,
                name: items.Name,
                description: items.Description,
                totalPrize: items.TotalPrize,
                contentImageUrl: items.ContentImageUrl,
                contentImagePublicId: string.Empty,
                guideUrl: items.GuideUrl,
                registrationUrl: items.RegistrationUrl,
                requirement: items.Requirement,
                contactPerson: items.ContactPerson,
                isEligible: items.IsEligible,
                registrationStartAt: items.RegistrationStartAt,
                registrationEndAt: items.RegistrationEndAt,
                isBookmarked: false);

            _dbContext.Scholarships.Add(scholarship);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}