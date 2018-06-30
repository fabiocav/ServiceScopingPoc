using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using ServiceScopingPoc.Controllers;

namespace ServiceScopingPoc
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
            .ConfigureServices(s =>
            {
                s.AddSingleton<TestHostedService>();
                s.Replace(ServiceDescriptor.Singleton<IServiceProviderFactory<IServiceCollection>>(new FunctionsServiceProviderFactory()));
            })
            .UseStartup<Startup>()
            .ConfigureLogging(b => b.ClearProviders());
    }
}
