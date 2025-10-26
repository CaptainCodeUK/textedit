using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TextEdit.App;

/// <summary>
/// Configures services and middleware for the ASP.NET Core host
/// </summary>
public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    /// <summary>
    /// Configure dependency injection container
    /// </summary>
    public void ConfigureServices(IServiceCollection services)
    {
        // Add Blazor Server
        services.AddRazorPages();
        services.AddServerSideBlazor();

        // Core services will be registered here in Phase 2
        // services.AddScoped<IDocumentService, DocumentService>();
        // services.AddScoped<ITabService, TabService>();
        // etc.
    }

    /// <summary>
    /// Configure HTTP request pipeline
    /// </summary>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseStaticFiles();
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapBlazorHub();
            endpoints.MapFallbackToPage("/_Host");
        });
    }
}
