using System;
using System.Diagnostics;
using System.Threading.Tasks;
using AppSyndication.BackendModel.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AppSyndication.DownloadService.WebSvc
{
    public class Startup
    {
        public Startup(IHostingEnvironment hostingEnvironment)
        {
            this.Configuration = new ConfigurationBuilder()
                .SetBasePath(hostingEnvironment.ContentRootPath)
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();
        }

        public IConfigurationRoot Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IMemoryCache>(
                s => new MemoryCache(new MemoryCacheOptions())
            );

            services.AddSingleton(
                s => new Connection(this.Configuration["Connection:StorageConnectionString"])
            );

            services.AddSingleton<ITagContext, TagContext>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, ITagContext tagContext)
        {
            var sourceSwitch = new SourceSwitch("AppSyndicationDownloadServiceTraceSource") { Level = SourceLevels.Warning };
            loggerFactory.AddTraceSource(sourceSwitch, new AzureApplicationLogTraceListener());
            loggerFactory.AddTraceSource(sourceSwitch, new EventLogTraceListener("Application"));

#if DEBUG
            loggerFactory.AddConsole(this.Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
#endif

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/error");
            }

            var logger = loggerFactory.CreateLogger<Startup>();

            app.Run(async (context) =>
            {
                await ByKey(context, logger, tagContext);
            });
        }

        private static async Task ByKey(HttpContext context, ILogger logger, ITagContext tagContext)
        {
            var key = context.Request.Path.Value.Substring(1); // remove the leading slash.

            if (String.IsNullOrEmpty(key))
            {
                logger.LogWarning("Could not find download redirect: {0}", key);

                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            if (key.Equals("favicon.ico", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            if (key.Equals("error", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 500;
                return;
            }

            var redirectUri = await tagContext.GetTagDownloadRedirectUriAsync(key);

            if (String.IsNullOrEmpty(redirectUri))
            {
                logger.LogError("Could not find download redirect: {0}", key);
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            var connectionFeature = context.Features.Get<IHttpConnectionFeature>();

            var ip = connectionFeature?.RemoteIpAddress.ToString() ?? String.Empty;

            await tagContext.IncrementDownloadRedirectCountAsync(key, ip);

            context.Response.StatusCode = 302;
            context.Response.Headers["Location"] = redirectUri;
        }
    }
}
