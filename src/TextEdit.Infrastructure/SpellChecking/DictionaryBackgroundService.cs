using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TextEdit.Core.SpellChecking;

namespace TextEdit.Infrastructure.SpellChecking;

/// <summary>
/// Background hosted service that attempts to download and install dictionaries when the app starts.
/// It updates AppState with progress and requests notifications when complete.
/// </summary>
public class DictionaryBackgroundService : IHostedService, IDisposable
{
    private readonly IServiceProvider _sp;
    private readonly IConfiguration _config;
    private readonly ILogger<DictionaryBackgroundService> _logger;
    private CancellationTokenSource? _cts;
    private Task? _backgroundTask;

    public DictionaryBackgroundService(IServiceProvider sp, IConfiguration config, ILogger<DictionaryBackgroundService> logger)
    {
        _sp = sp;
        _config = config;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _backgroundTask = Task.Run(() => RunAsync(_cts.Token));
        return Task.CompletedTask;
    }

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            var options = _config.GetSection("SpellCheck").Get<TextEdit.Core.SpellChecking.SpellCheckOptions>() ?? new TextEdit.Core.SpellChecking.SpellCheckOptions();
            var isCi = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"));
            if (isCi)
            {
                _logger.LogInformation("CI detected - skipping background dictionary download.");
                return;
            }

            if (!options.AutoDownloadOnStartup || string.IsNullOrEmpty(options.DefaultDicUrl) || string.IsNullOrEmpty(options.DefaultAffUrl))
            {
                _logger.LogInformation("Auto-download disabled or no default URLs configured.");
                return;
            }

            using var scope = _sp.CreateScope();
            var installer = scope.ServiceProvider.GetRequiredService<DictionaryInstaller>();
            var appState = scope.ServiceProvider.GetRequiredService<IDictionaryInstallNotifier>();
            var spellSvc = scope.ServiceProvider.GetService<TextEdit.Infrastructure.SpellChecking.SpellCheckingService>();

            await appState.SetDictionaryInstallStateAsync(DictionaryDownloadState.InProgress, "Installing dictionaries...");

            var retry = Math.Max(1, options.DownloadRetryCount);
            var timeoutSec = Math.Max(5, options.DownloadTimeoutSeconds);
            var success = false;
            for (int attempt = 1; attempt <= retry && !ct.IsCancellationRequested; attempt++)
            {
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    cts.CancelAfter(TimeSpan.FromSeconds(timeoutSec));
                    _logger.LogInformation("DictionaryBackgroundService: download attempt {Attempt}/{Retry}", attempt, retry);
                    var downloaded = await installer.DownloadAndInstallDefaultDictionaryAsync(options.DefaultDicUrl!, options.DefaultAffUrl!, cts.Token);
                    if (downloaded)
                    {
                        // Try to load dictionary and swap spell checker
                        var customPath = DictionaryService.GetCustomDictionaryPath();
                        var dicPath = Path.Combine(customPath, DictionaryService.EnglishDicFileName);
                        var affPath = Path.Combine(customPath, DictionaryService.EnglishAffFileName);
                        if (File.Exists(dicPath) && File.Exists(affPath))
                        {
                            var hun = DictionaryService.LoadFromFiles(dicPath, affPath);
                            // Replace runtime spell checker if available
                            if (spellSvc != null)
                            {
                                spellSvc.ReplaceSpellChecker(hun);
                                _logger.LogInformation("DictionaryBackgroundService: installed and replaced spell checker successfully.");
                            }
                            await appState.SetDictionaryInstallStateAsync(DictionaryDownloadState.Completed, $"Dictionary installed ({DictionaryService.LastLoadedDictionaryWordCount} words)");
                            success = true;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "DictionaryBackgroundService: attempt {Attempt} failed", attempt);
                }
                await Task.Delay(1000, ct);
            }

            if (!success)
            {
                await appState.SetDictionaryInstallStateAsync(DictionaryDownloadState.Failed, "Failed to install dictionary");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DictionaryBackgroundService encountered an unexpected error");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cts?.Cancel();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _cts?.Dispose();
    }
}
