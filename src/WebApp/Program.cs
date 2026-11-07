using System.Text.Json;
using System.Text.Json.Serialization;
using AspNetCoreHero.ToastNotification;
using BeasiswaIn.Web.Infrastructures.DependencyInjection;
using BeasiswaIn.Web.Infrastructures.Identity;
using BeasiswaIn.Web.Infrastructures.Integrations.Services;
using BeasiswaIn.Web.Infrastructures.Persistance.Seeder;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Services.Dashboard;
using BeasiswaIn.Web.Services.Personalizations;
using BeasiswaIn.Web.Services.Scholarships;
using BeasiswaIn.Web.Services.Students;
using BeasiswaIn.Web.Services.SubmitScholarships;
using Microsoft.FeatureManagement;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddAppChatbot(builder.Configuration);

// client app configuration
builder.Services
    .AddRazorPages(options =>
    {
        options.Conventions.AuthorizeFolder("/dashboard", AuthorizationPolicies.AdminOnly);
        options.Conventions.AuthorizeFolder("/students", AuthorizationPolicies.StudentOnly);
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// services registration
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IScholarshipService, ScholarshipService>();
builder.Services.AddScoped<IScholarshipCategoryService, ScholarshipCategoryService>();
builder.Services.AddScoped<IUserScholarshipPreferenceService, UserScholarshipPreferenceService>();
builder.Services.AddScoped<ISubmitScholarshipService, SubmitScholarshipService>();
builder.Services.AddScoped<IStudentProfileService, StudentProfileService>();
builder.Services.AddTransient<AppSeeder>();

// fondation infrastructure
builder.Services.AddAppAntiForgery(builder.Environment);
builder.Services.AddAppAuthentication(builder.Environment, builder.Configuration);
builder.Services.AddAppAuthorization();
builder.Services.AddAppCookie(builder.Environment);
builder.Services.AddAppRateLimit();
builder.Services.AddAppCors();
builder.Services.AddScoped<IDatasetReader, DatasetReader>();

// external integrations
builder.Services.AddAppDatabase(builder.Configuration);
builder.Services.AddAppFileStorage(builder.Configuration);
builder.Services.AddAppOpenApi(builder.Configuration);
builder.Services.AddFeatureManagement();
builder.Services.AddNotyf(config =>
{
    config.DurationInSeconds = 7;
    config.IsDismissable = true;
    config.Position = NotyfPosition.TopRight;
});

// external integrations, only for debug
#if DEBUG
    builder.Services.AddSassCompiler();
#endif

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Enable seeder dan disble kalau sudah selesai

    // using var scope = app.Services.CreateScope();
    // var seeder = scope.ServiceProvider.GetRequiredService<AppSeeder>();

    // await seeder.SeedRolesAsync();

    // var adminResult = await seeder.SeedAdminAsync();
    // if(adminResult.IsFailure)
    //     Console.WriteLine(adminResult.Error.ToString());

    // await seeder.SeedScholarshipsAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseNotyf();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
