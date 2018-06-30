using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceScopingPoc.Controllers;

namespace ServiceScopingPoc
{
    public class Startup
    {
        private readonly FunctionsServiceProvider _serviceProvider;

        public Startup(IConfiguration configuration, FunctionsServiceProvider serviceProvider)
        {
            Configuration = configuration;
            _serviceProvider = serviceProvider;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IScriptHostManager>(p => p.GetRequiredService<TestHostedService>());
            services.AddSingleton<IHostedService>(p => p.GetRequiredService<TestHostedService>());
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            _serviceProvider.AddServices(services);
            return _serviceProvider;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseMiddleware<TestResetMiddleware>();
            app.UseMiddleware<TestMiddleware>();
            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
