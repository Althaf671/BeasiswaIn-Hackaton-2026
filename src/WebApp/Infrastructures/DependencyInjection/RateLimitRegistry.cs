

using System.Threading.RateLimiting;
using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Common.Errors;
using BeasiswaIn.Web.Common.Helpers;

namespace BeasiswaIn.Web.Infrastructures.DependencyInjection;

internal static class RateLimitRegistry
{
    public const string LowRateAuthPolicy = nameof(LowRateAuthPolicy);
    public const string MediumRateAuthPolicy = nameof(MediumRateAuthPolicy);
    public const string HighRateAuthPolicy = nameof(HighRateAuthPolicy);
    public const string LowRateResourcePolicy = nameof(LowRateResourcePolicy);
    public const string MediumRateResourcePolicy = nameof(MediumRateResourcePolicy);
    public const string HighRateResourcePolicy = nameof(HighRateResourcePolicy);


    public static IServiceCollection AddAppRateLimit(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // response after limit reached
            options.OnRejected = async (context, cancellationToken) =>
            {
                var problemDetails = Result.Failure(SystemError.TooManyRequests)
                    .ToProblemDetailsObject(context.HttpContext);
                    
                context.HttpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/problem+json";
                
                await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            };

            // rate limiter for auth endpoints
            options.AddPolicy(LowRateAuthPolicy, httpContext =>
                RateLimitPartition.GetTokenBucketLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 25,
                        TokensPerPeriod = 25,
                        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true
                    }));

            options.AddPolicy(MediumRateAuthPolicy, httpContext =>
                RateLimitPartition.GetTokenBucketLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 10,
                        TokensPerPeriod = 10,
                        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true
                    }));

            options.AddPolicy(HighRateAuthPolicy, httpContext =>
                RateLimitPartition.GetTokenBucketLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 7,
                        TokensPerPeriod = 7,
                        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true
                    }));

            // rate limiter for internal endpoints
            options.AddPolicy(LowRateResourcePolicy, httpcontext =>
                RateLimitPartition.GetTokenBucketLimiter(
                    partitionKey: httpcontext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 200,
                        TokensPerPeriod = 200,
                        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true
                    }));

            options.AddPolicy(MediumRateResourcePolicy, httpcontext =>
                RateLimitPartition.GetTokenBucketLimiter(
                    partitionKey: httpcontext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 100,
                        TokensPerPeriod = 100,
                        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true
                    }));

            options.AddPolicy(HighRateResourcePolicy, httpcontext =>
                RateLimitPartition.GetTokenBucketLimiter(
                    partitionKey: httpcontext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 50,
                        TokensPerPeriod = 50,
                        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true
                    }));
        });

        return services;
    }
}