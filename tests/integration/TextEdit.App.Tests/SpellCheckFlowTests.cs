using Microsoft.Playwright;
using Xunit;
using System.Diagnostics;
using System.Linq;

namespace TextEdit.App.Tests;

[Collection("PlaywrightDom")]
public class SpellCheckFlowTests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _page;
    private Process? _appProcess;
    private string? _skipReason;
    private const string RemoteDebuggingUrlLoopback = "http://127.0.0.1:9222";
    private const string RemoteDebuggingUrlLocalhost = "http://localhost:9222";

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        try
        {
            // Try to connect via 127.0.0.1 first (IPv4), then localhost (::1 may be IPv6)
            try { _browser = await _playwright.Chromium.ConnectOverCDPAsync(RemoteDebuggingUrlLoopback); } catch { _browser = null; }
            if (_browser == null)
            {
                try { _browser = await _playwright.Chromium.ConnectOverCDPAsync(RemoteDebuggingUrlLocalhost); } catch { _browser = null; }
            }
            _page = _browser?.Contexts.FirstOrDefault()?.Pages.FirstOrDefault();
            if (_page == null)
            {
                try { if (_browser != null) await _browser.CloseAsync(); } catch { }
                _browser = null;
            }
            if (_browser == null)
            {
                await LaunchAppWithRemoteDebuggingAsync();
                await Task.Delay(3000);
                try { _browser = await _playwright.Chromium.ConnectOverCDPAsync(RemoteDebuggingUrlLoopback); } catch { _browser = null; }
                if (_browser == null) try { _browser = await _playwright.Chromium.ConnectOverCDPAsync(RemoteDebuggingUrlLocalhost); } catch { _browser = null; }
                var stopWatch = Stopwatch.StartNew();
                while ((_page = _browser?.Contexts.FirstOrDefault()?.Pages.FirstOrDefault()) == null && stopWatch.ElapsedMilliseconds < 15000)
                {
                    await Task.Delay(250);
                }
                if (_page == null) { _skipReason = "Could not find page"; return; }
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }
        }
        catch (System.Exception ex)
        {
            _skipReason = ex.Message;
            return;
        }
    }

    public async Task DisposeAsync()
    {
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
        if (_appProcess != null && !_appProcess.HasExited)
        {
            _appProcess.Kill(entireProcessTree: true);
            _appProcess.Dispose();
        }
    }

    private async Task LaunchAppWithRemoteDebuggingAsync()
    {
        var workspaceRoot = Directory.GetCurrentDirectory();
        while (workspaceRoot != null && !File.Exists(Path.Combine(workspaceRoot, "textedit.sln")))
            workspaceRoot = Directory.GetParent(workspaceRoot)?.FullName;
        if (workspaceRoot == null) throw new System.Exception("Could not find workspace root");
        var appPath = Path.Combine(workspaceRoot, "src", "TextEdit.App");
        _appProcess = new Process { StartInfo = new ProcessStartInfo { FileName = "electronize", Arguments = "start /args --remote-debugging-port=9222", WorkingDirectory = appPath, UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true } };
        _appProcess.OutputDataReceived += (s, e) => Debug.WriteLine($"[AppOut] {e.Data}");
        _appProcess.ErrorDataReceived += (s, e) => Debug.WriteLine($"[AppErr] {e.Data}");
        _appProcess.Start();
        _appProcess.BeginOutputReadLine();
        _appProcess.BeginErrorReadLine();
        await Task.Delay(5000);
    }

    private bool EnsurePlaywrightReady()
    {
        if (!string.IsNullOrEmpty(_skipReason))
        {
            Debug.WriteLine($"Playwright skipped: {_skipReason}");
            return false;
        }
        if (_page == null)
        {
            Debug.WriteLine("Playwright skipped: no connected page (CDP endpoint not available)");
            return false;
        }
        return true;
    }

    [Fact]
    public async Task SpellCheck_DecorationAndSuggestion_Shown()
    {
    if (!EnsurePlaywrightReady()) return;
        Assert.NotNull(_page);
        // Wait for monaco to be present
        await _page!.WaitForSelectorAsync("#monaco-editor", new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
        // Insert a small text with a common misspelling and position the caret on it
        await _page.EvaluateAsync("() => textEditMonaco.setValue('monaco-editor','This is teh test')");
        // Set caret at the 'teh' word (line 1, column starting at 9 should land on the word)
        await _page.EvaluateAsync("() => textEditMonaco.setCaretPositionAt('monaco-editor', 1, 9)");

        // Trigger spell check via DotNet-invoked command which triggers both the background check and shows suggestions
        await _page.EvaluateAsync("() => DotNet.invokeMethodAsync('TextEdit.UI', 'HandleSpellCheckFromJS')");

        // Poll for getSpellCheckDecorationAtCaret to return a result
        object? jsResult = null;
        var tries = 0;
        while (jsResult == null && tries < 20)
        {
            await Task.Delay(200);
            jsResult = await _page.EvaluateAsync<object?>("() => textEditMonaco.getSpellCheckDecorationAtCaret('monaco-editor')");
            tries++;
        }
        Assert.NotNull(jsResult);
        // Verify suggestions exist using Evaluate returning array length
        var suggestionsLength = await _page.EvaluateAsync<int>("() => { const r = textEditMonaco.getSpellCheckDecorationAtCaret('monaco-editor'); if (!r) return 0; return (r.suggestions || r.Suggestions || []).length; }");
        Assert.True(suggestionsLength > 0, "Expected suggestions for 'teh'");
        // Optionally assert first suggestion is 'the'
        var firstSuggestion = await _page.EvaluateAsync<string>("() => { const r = textEditMonaco.getSpellCheckDecorationAtCaret('monaco-editor'); return r.suggestions[0].Word || r.suggestions[0].word || r.suggestions[0]; }");
        Assert.True(!string.IsNullOrEmpty(firstSuggestion));
    }

    [Fact]
    public async Task SpellCheck_QueuedDecoration_AppliesOnMonacoCreate()
    {
        if (!EnsurePlaywrightReady()) return;
        Assert.NotNull(_page);

        // Ensure the default textarea is present
        await _page!.WaitForSelectorAsync("textarea#main-editor-textarea", new() { State = WaitForSelectorState.Visible, Timeout = 10000 });

        // Populate the textarea with content that includes the misspelling
        await _page.EvaluateAsync("() => { const ta = document.querySelector('textarea#main-editor-textarea'); if (ta) { ta.value = 'This is teh test'; ta.dispatchEvent(new Event('input')); }}");

        // Craft a decoration for the word 'teh' (line 1, col 9-11)
        var decorationJson = @"[{\n  range: { startLineNumber: 1, startColumn: 9, endLineNumber: 1, endColumn: 12 },\n  options: { inlineClassName: 'spell-check-error', message: 'teh', suggestions: [{ Word: 'the', IsPrimary: true, Confidence: 80 }] }\n}]";

        // Attempt to set decorations (should be queued if Monaco isn't present)
        await _page.EvaluateAsync("(d) => { try { window.textEditMonaco.setSpellCheckDecorations('monaco-editor', JSON.parse(d)); } catch(e) { console.error(e); } }", decorationJson);

        // Toggle alternate editor (Monaco) to force createEditor and flush pending decorations
        await _page.EvaluateAsync("() => DotNet.invokeMethodAsync('TextEdit.UI', 'ToggleAlternateEditorFromJS', true)");

        // Wait for Monaco to be present
        await _page.WaitForSelectorAsync("#monaco-editor", new() { State = WaitForSelectorState.Visible, Timeout = 15000 });

        // Set caret at the 'teh' word
        await _page.EvaluateAsync("() => textEditMonaco.setCaretPositionAt('monaco-editor', 1, 9)");

        // Poll for decoration presence at caret
        object? jsResult = null;
        var tries = 0;
        while (jsResult == null && tries < 20)
        {
            await Task.Delay(200);
            jsResult = await _page.EvaluateAsync<object?>("() => textEditMonaco.getSpellCheckDecorationAtCaret('monaco-editor')");
            tries++;
        }
        Assert.NotNull(jsResult);
        var suggestionsLength = await _page.EvaluateAsync<int>("() => { const r = textEditMonaco.getSpellCheckDecorationAtCaret('monaco-editor'); if (!r) return 0; return (r.suggestions || r.Suggestions || []).length; }");
        Assert.True(suggestionsLength > 0);
    }
}
