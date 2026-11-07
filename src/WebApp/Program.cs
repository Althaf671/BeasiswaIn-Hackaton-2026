using System.Text.Json;
using System.Text.Json.Serialization;
using AspNetCoreHero.ToastNotification;
using BeasiswaIn.Web.Infrastructures.DependencyInjection;
using BeasiswaIn.Web.Infrastructures.Identity;
using BeasiswaIn.Web.Infrastructures.Persistance.Seeder;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Services.Dashboard;
using BeasiswaIn.Web.Services.Personalizations;
using BeasiswaIn.Web.Services.Scholarships;

var builder = WebApplication.CreateBuilder(args);

// client app configuration
builder.Services
    .AddRazorPages(options =>
    {
        options.Conventions.AuthorizeFolder("/dashboard", AppRoles.Admin);
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
builder.Services.AddTransient<AppSeeder>();

// fondation infrastructure
builder.Services.AddAppAntiForgery(builder.Environment);
builder.Services.AddAppAuthentication(builder.Environment);
builder.Services.AddAppAuthorization();
builder.Services.AddAppCookie(builder.Environment);
builder.Services.AddAppRateLimit();
builder.Services.AddAppCors();

// external integrations
builder.Services.AddAppDatabase(builder.Configuration);
builder.Services.AddAppFileStorage(builder.Configuration);
builder.Services.AddAppOpenApi(builder.Configuration);
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

    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<AppSeeder>();

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
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
