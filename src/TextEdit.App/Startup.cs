using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TextEdit.Core.Abstractions;
using TextEdit.Core.SpellChecking;
using System.Net.Http;
using TextEdit.Core.Documents;
using TextEdit.Core.Editing;
using TextEdit.Infrastructure.Autosave;
using TextEdit.Infrastructure.FileSystem;
using TextEdit.Infrastructure.Telemetry;
using TextEdit.Infrastructure.Ipc;
using TextEdit.Infrastructure.Persistence;
using TextEdit.Infrastructure.Themes;
using TextEdit.Core.Preferences;
using TextEdit.Markdown;
using TextEdit.UI.App;
using TextEdit.UI.Services;
using TextEdit.Infrastructure.Logging;
using TextEdit.Core.Searching;

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
        // Add Blazor Server with detailed error reporting
        services.AddRazorPages();
        services.AddServerSideBlazor()
            .AddCircuitOptions(options => options.DetailedErrors = true);

        // Phase 2: Register core/infrastructure services
        services.AddSingleton<IUndoRedoService, UndoRedoService>();
        services.AddSingleton<IFileSystem, FileSystemService>();
        
        // Logging infrastructure - factory that checks LoggingEnabled preference
        services.AddSingleton<IAppLoggerFactory>(sp => 
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            // Get AppState lazily to avoid circular dependency
            return new AppLoggerFactory(loggerFactory, () => 
            {
                var appState = sp.GetService<AppState>();
                return appState?.Preferences.LoggingEnabled ?? false;
            });
        });
        
        // Text search/replace (US1/US2)
        services.AddSingleton<FindService>();
        services.AddSingleton<ReplaceService>(sp => new ReplaceService(sp.GetRequiredService<FindService>()));
        
        // DocumentService with logger
        services.AddSingleton<DocumentService>(sp => 
        {
            var fs = sp.GetRequiredService<IFileSystem>();
            var undo = sp.GetRequiredService<IUndoRedoService>();
            var loggerFactory = sp.GetRequiredService<IAppLoggerFactory>();
            var logger = loggerFactory.CreateLogger<DocumentService>();
            var replace = sp.GetRequiredService<ReplaceService>();
            return new DocumentService(fs, undo, logger, replace);
        });
        
        services.AddSingleton<TabService>();
        services.AddSingleton<FileWatcher>();
        services.AddSingleton<PersistenceService>();
        services.AddSingleton<AutosaveService>(sp => new AutosaveService(intervalMs: 5000)); // 5 second autosave
        services.AddSingleton<IpcBridge>();
        // Performance monitoring
        services.AddSingleton<PerformanceLogger>();
        // Markdown rendering
        services.AddSingleton<MarkdownRenderer>();
        services.AddSingleton<MarkdownFormattingService>();
        
    // Spell checking (v1.2) - with graceful fallback if dictionaries are missing
        TextEdit.Core.SpellChecking.ISpellChecker? spellChecker = null;
        try
        {
            spellChecker = TextEdit.Infrastructure.SpellChecking.DictionaryService.LoadEnglishDictionary();
        }
        catch (Exception ex)
        {
            // Dictionary files not found or failed to load - spell checking will be disabled
            System.Diagnostics.Debug.WriteLine($"Spell checking disabled: {ex.Message}");
        }
        
        // If we couldn't load embedded dictionary earlier, try downloading defaults from configured URLs
        if (spellChecker == null)
        {
            var spellCheckOptions = Configuration.GetSection("SpellCheck").Get<SpellCheckOptions>();
            if (spellCheckOptions?.DefaultDicUrl != null && spellCheckOptions?.DefaultAffUrl != null)
            {
                try
                {
                    var installer = new TextEdit.Infrastructure.SpellChecking.DictionaryInstaller(new HttpClient());
                    var downloaded = installer.DownloadAndInstallDefaultDictionaryAsync(spellCheckOptions.DefaultDicUrl, spellCheckOptions.DefaultAffUrl).GetAwaiter().GetResult();
                    if (downloaded)
                    {
                        var customPath = TextEdit.Infrastructure.SpellChecking.DictionaryService.GetCustomDictionaryPath();
                        var dicPath = Path.Combine(customPath, TextEdit.Infrastructure.SpellChecking.DictionaryService.EnglishDicFileName);
                        var affPath = Path.Combine(customPath, TextEdit.Infrastructure.SpellChecking.DictionaryService.EnglishAffFileName);
                        if (File.Exists(dicPath) && File.Exists(affPath))
                        {
                            spellChecker = TextEdit.Infrastructure.SpellChecking.DictionaryService.LoadFromFiles(dicPath, affPath);
                            System.Diagnostics.Debug.WriteLine("Downloaded and installed default dictionaries successfully.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to download default dictionaries: {ex.Message}");
                }
            }
        }

    // Only register SpellCheckingService if we have a spell checker
        if (spellChecker != null)
        {
            System.Diagnostics.Debug.WriteLine($"Spell checking enabled. SpellChecker initialized: {spellChecker.IsInitialized}");
            services.AddSingleton(spellChecker);
            services.AddSingleton<TextEdit.Infrastructure.SpellChecking.SpellCheckingService>();
            // Ensure embedded dictionaries are copied to user's custom dictionary path, if missing
            try
            {
                TextEdit.Infrastructure.SpellChecking.DictionaryService.EnsureEmbeddedDictionaryInstalledToCustomPath();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to install embedded dictionaries to custom path: {ex.Message}");
            }
        }
        else
        {
            // Dictionary load failed. Register a demo spell checker for development
            // to provide visual feedback in the editor. This is not a replacement
            // for full Hunspell dictionaries and is intended for development/test use.
            
            // Register a stub or demo spell checker as fallback
        services.AddSingleton<TextEdit.Core.SpellChecking.ISpellChecker>(sp => new TextEdit.Infrastructure.SpellChecking.DemoSpellChecker());
            services.AddSingleton<TextEdit.Infrastructure.SpellChecking.SpellCheckingService>();
    }
        
        services.AddSingleton<IPreferencesRepository>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<IAppLoggerFactory>();
            var logger = loggerFactory.CreateLogger<TextEdit.Infrastructure.Persistence.PreferencesRepository>();
            var msLogger = sp.GetRequiredService<ILogger<TextEdit.Infrastructure.Persistence.PreferencesRepository>>();
            return new TextEdit.Infrastructure.Persistence.PreferencesRepository(logger, msLogger);
        });
        services.AddSingleton<WindowStateRepository>();
    // Register dictionary installer for user-initiated dictionary downloads
    services.AddSingleton<TextEdit.Infrastructure.SpellChecking.DictionaryInstaller>(sp => new TextEdit.Infrastructure.SpellChecking.DictionaryInstaller(new System.Net.Http.HttpClient()));
        services.AddSingleton<ThemeDetectionService>();
        
        // Register ThemeManager as singleton without IJSRuntime dependency
        services.AddSingleton<ThemeManager>(sp => new ThemeManager());
        
        // Register ElectronIpcListener as scoped (not singleton) so it can use IJSRuntime
        services.AddScoped<ElectronIpcListener>();
        
        // Phase 3 (v1.2): Auto-updates
        services.AddSingleton<TextEdit.Infrastructure.Updates.AutoUpdateService>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<IAppLoggerFactory>();
            var logger = loggerFactory.CreateLogger<TextEdit.Infrastructure.Updates.AutoUpdateService>();
            return new TextEdit.Infrastructure.Updates.AutoUpdateService(logger);
        });
        
        // UI state (ensure spell checking service is available to AppState so it can apply prefs on load)
        services.AddSingleton<AppState>(sp => new AppState(
            sp.GetRequiredService<DocumentService>(),
            sp.GetRequiredService<TabService>(),
            sp.GetRequiredService<IpcBridge>(),
            sp.GetRequiredService<PersistenceService>(),
            sp.GetRequiredService<AutosaveService>(),
            sp.GetRequiredService<PerformanceLogger>(),
            sp.GetRequiredService<IPreferencesRepository>(),
            sp.GetRequiredService<ThemeDetectionService>(),
            sp.GetRequiredService<ThemeManager>(),
            sp.GetService<IAppLoggerFactory>(),
            sp.GetService<Microsoft.Extensions.Logging.ILogger<AppState>>(),
            sp.GetService<DialogService>(),
            sp.GetService<TextEdit.Infrastructure.SpellChecking.SpellCheckingService>()
        ));
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
