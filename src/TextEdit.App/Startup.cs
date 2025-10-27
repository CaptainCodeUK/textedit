using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TextEdit.Core.Abstractions;
using TextEdit.Core.Documents;
using TextEdit.Core.Editing;
using TextEdit.Infrastructure.Autosave;
using TextEdit.Infrastructure.FileSystem;
using TextEdit.Infrastructure.Ipc;
using TextEdit.Infrastructure.Persistence;
using TextEdit.Markdown;
using TextEdit.UI.App;

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

        // Phase 2: Register core/infrastructure services
        services.AddSingleton<IUndoRedoService, UndoRedoService>();
        services.AddSingleton<IFileSystem, FileSystemService>();
        services.AddSingleton<DocumentService>();
        services.AddSingleton<TabService>();
        services.AddSingleton<FileWatcher>();
        services.AddSingleton<PersistenceService>();
        services.AddSingleton<AutosaveService>();
        services.AddSingleton<IpcBridge>();
        // Markdown rendering
        services.AddSingleton<MarkdownRenderer>();
        // UI state
        services.AddSingleton<AppState>();
        // Dialog service
        services.AddSingleton<DialogService>();
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
