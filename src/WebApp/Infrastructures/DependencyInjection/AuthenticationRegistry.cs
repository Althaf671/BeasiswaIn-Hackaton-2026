

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using BeasiswaIn.Web.Infrastructures.Identity;
using BeasiswaIn.Web.Infrastructures.Persistance;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Infrastructuress.Middleware;

namespace BeasiswaIn.Web.Infrastructures.DependencyInjection;

internal static class AuthenticationRegistry
{
    public static IServiceCollection AddAppAuthentication(
        this IServiceCollection services,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();
        services.AddScoped<IIdentityService, IdentityService>();

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            // Lockout policy
            options.Lockout.MaxFailedAccessAttempts = 10;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.AllowedForNewUsers = true;

            // password policy
            options.Password.RequiredLength = 12;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireDigit = true;
            options.Password.RequiredUniqueChars = 1;

            // user policy
            options.User.RequireUniqueEmail = true;
            // Google profile names can contain spaces and non-Latin characters.
            options.User.AllowedUserNameCharacters = string.Empty;
            
            // sign-in policy
            options.SignIn.RequireConfirmedEmail = false;
            options.SignIn.RequireConfirmedPhoneNumber = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();  

        services.AddAuthentication().AddGoogle(options =>
        {
            options.ClientId = configuration["Authentication:Google:ClientId"] ?? string.Empty;
            options.ClientSecret = configuration["Authentication:Google:ClientSecret"] ?? string.Empty;
            options.SignInScheme = IdentityConstants.ExternalScheme;
            options.CallbackPath = "/signin-google";
            options.Events.OnRemoteFailure = context =>
            {
                context.HandleResponse();
                context.Response.Redirect("/identity/login-student?googleError=true");
                return Task.CompletedTask;
            };
        });

        services.ConfigureApplicationCookie(options =>
        {
            // auth cookie base config
            options.Cookie.HttpOnly = true;
            options.Cookie.Path = "/";
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.Name = environment.IsDevelopment() 
                ? "__Dev-Auth"
                : "__Secure-Auth";
            options.Cookie.SecurePolicy = environment.IsDevelopment() 
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;

            // cookie duration and renewal
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;

            options.LoginPath = "/identity/login";
            options.LogoutPath = "/identity/logout";
            options.AccessDeniedPath = "/identity/access-denied";

            static Task HandleRedirect(RedirectContext<CookieAuthenticationOptions> ctx, int ajaxStatus)
            {
                if (ctx.Request.Headers.ContainsKey("HX-Request"))
                {
                    ctx.Response.Headers["HX-Redirect"] = ctx.RedirectUri;
                    ctx.Response.StatusCode = StatusCodes.Status200OK;
                }
                else if (ctx.Request.Headers.XRequestedWith == "XMLHttpRequest")
                {
                    ctx.Response.StatusCode = ajaxStatus;
                }
                else
                {
                    ctx.Response.Redirect(ctx.RedirectUri);
                }

                return Task.CompletedTask;
            }

            options.Events.OnRedirectToLogin = ctx =>
                HandleRedirect(ctx, StatusCodes.Status401Unauthorized);

            options.Events.OnRedirectToAccessDenied = ctx =>
                HandleRedirect(ctx, StatusCodes.Status403Forbidden);

            // later need OnValidatePrincipal
        });

        // services.AddDataProtection()
        //     .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"))
        //     .SetApplicationName("NoirCoreAuth");
    
        return services;
    }
}