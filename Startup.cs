using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using KafkaTester.Service;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using System;

namespace KafkaTester;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    private IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTelemetry();
        services.AddHealthChecks();
        services.AddRazorPages();
        services.AddServerSideBlazor();
        services.AddScoped<KafkaTesterService>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        
        app.UseDeveloperExceptionPage();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseTelemetry();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHealthChecks("/healthcheck");
            endpoints.MapBlazorHub();
            endpoints.MapFallbackToPage("/_Host");
        });
    }
}

public static class TelemetryExtensions
{
    public const string SERVICE_NAME = "KafkaTester";

    public static IServiceCollection AddTelemetry(this IServiceCollection services)
    {
        if (Convert.ToBoolean(Environment.GetEnvironmentVariable("ENABLE_OPEN_TELEMETRY")))
        {
            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService(SERVICE_NAME))
                .WithMetrics(metrics =>
                    metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                .AddPrometheusExporter());
        }

        return services;
    }

    public static IApplicationBuilder UseTelemetry(this IApplicationBuilder app)
    {
        if (Convert.ToBoolean(Environment.GetEnvironmentVariable("ENABLE_OPEN_TELEMETRY")))
            app.UseOpenTelemetryPrometheusScrapingEndpoint();

        return app;
    }
}