﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using ServiceScopingPoc.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ServiceScopingPoc
{
    public class TestHostedService : IHostedService, IScriptHostManager
    {
        private readonly FunctionsServiceProvider _provider;
        private readonly ILoggerFactory loggerFactory;
        private IHost _host;

        public TestHostedService(FunctionsServiceProvider provider, ILoggerFactory loggerFactory)
        {
            _provider = provider;
            this.loggerFactory = loggerFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _host = BuildHost();
            await _host.StartAsync(cancellationToken);
        }

        private IHost BuildHost()
        {
            return new HostBuilder()
                            .UseServiceProviderFactory(new ExternalFunctionsServiceProviderFactory(_provider))
                            .ConfigureServices(s =>
                            {
                                var fa = new LoggerFactory();
                                fa.AddConsole(LogLevel.Warning);
                                s.AddSingleton<ILoggerFactory>(fa);
                                s.AddSingleton<IHostLifetime, ScriptHostLifetime>();
                                s.AddScoped<IServiceA, TestService>(); // test scoped service in child container
                                s.AddSingleton<IServiceB, TestService>(); // test singleton service in child container
                            })
                            .Build();
        }

        public async Task RestartHostAsync(CancellationToken cancellationToken)
        {
            // Set state indicating we're in the middle of a restart (this would be primarily used by our host started checks)
            var _ = _host.StopAsync(cancellationToken).
                ContinueWith(t => { /* Handling similar to existing orphaning logic*/});

            _host = BuildHost();
            await _host.StartAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _host.StopAsync(cancellationToken);
        }
    }
}
