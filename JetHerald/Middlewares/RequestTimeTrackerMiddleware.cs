using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace JetHerald.Middlewares;
public class RequestTimeFeature
{
    public RequestTimeFeature() => Stopwatch = Stopwatch.StartNew();
    public Stopwatch Stopwatch { get; }
}

public class RequestTimeTrackerMiddleware : IMiddleware
{
    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        context.Features.Set(new RequestTimeFeature());
        return next(context);
    }
}
