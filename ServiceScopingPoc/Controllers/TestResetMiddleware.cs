using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceScopingPoc.Controllers
{
    public class TestResetMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceScopeFactory _scopeFactory;

        public TestResetMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory)
        {
            _next = next;
            _scopeFactory = scopeFactory;
        }

        public async Task Invoke(HttpContext httpContext, IScriptHostManager manager, ILogger<TestMiddleware> logger)
        {
            
            if (httpContext.Request.Query.ContainsKey("reset"))
            {
                await manager.RestartHostAsync(CancellationToken.None);
                logger.LogWarning("Environment reset");
                var features = httpContext.Features;
                var servicesFeature = features.Get<IServiceProvidersFeature>();

                features.Set<IServiceProvidersFeature>(new RequestServicesFeature(httpContext, _scopeFactory));
            }

            await _next(httpContext);
        }
    }

    public class TestMiddleware
    {
        private readonly RequestDelegate _next;

        public TestMiddleware(RequestDelegate next, IServiceA serviceA = null, IServiceB serviceB = null)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext, IServiceA serviceA, IServiceB serviceB, ILogger<TestMiddleware> logger)
        {
            logger.LogWarning($"Service B ID: {serviceB.Id}");
            await _next(httpContext);
        }
    }
}
