using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using TextEdit.Core.Updates;
using TextEdit.Infrastructure.Ipc;
using System.Runtime.InteropServices;

namespace TextEdit.UI.Components.Dialogs;

public partial class UpdateNotificationDialog : ComponentBase, IDisposable
{
    [Inject] protected IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] protected IpcBridge IpcBridge { get; set; } = default!;

    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public UpdateMetadata? UpdateMetadata { get; set; }
    [Parameter] public EventCallback OnInstall { get; set; }
    [Parameter] public EventCallback OnRemindLater { get; set; }

    private ElementReference dialogElement;
    private ElementReference _overlayRef;
    private DotNetObjectReference<UpdateNotificationDialog>? _dotNetRef;
    private ElementReference installButtonElement;
    private ElementReference remindButtonElement;
    private bool _isClosing = false;
    private bool _portalAttached = false;
    private ElementReference _portalAttachedElementRef;

    private async Task<bool> TryAttachPortalAsync(object selectorOrElement)
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("console.log", "textEditPortal: trying to attach", selectorOrElement);
            var res = await JSRuntime.InvokeAsync<object>("textEditPortal.attach", selectorOrElement);
            // Ensure the portal actually ended up attached.
            // textEditPortal.attach may log a success in JS but return undefined to .NET
            // (or a proxy object), so we double-check using isAttached.
            var ok = false;
            if (res is bool b && b) ok = true;
            else
            {
                try
                {
                    ok = await JSRuntime.InvokeAsync<bool>("textEditPortal.isAttached", selectorOrElement);
                }
                catch
                {
                    ok = false;
                }
            }
            await JSRuntime.InvokeVoidAsync("console.log", "textEditPortal: attach result", ok, selectorOrElement);
            return ok;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> TryAttachWithRetryAsync(object selectorOrElement, int retries = 3, int delayMs = 60)
    {
        for (int attempt = 0; attempt < retries; attempt++)
        {
            if (await TryAttachPortalAsync(selectorOrElement)) return true;
            await Task.Delay(delayMs);
        }
        return false;
    }

    private async Task TryDetachPortalAsync(ElementReference elementRef)
    {
        try
        {
            await JSRuntime.InvokeAsync<object>("textEditPortal.detach", elementRef);
        }
        catch { }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (IsVisible && !_isClosing)
        {
            await Task.Delay(10);
            try { await JSRuntime.InvokeVoidAsync("eval", "document.querySelector('[role=\\\"dialog\\\"] button:last-child').focus()"); } catch { }

            if (!_portalAttached)
            {
                await Task.Delay(50);
                var attached = await TryAttachWithRetryAsync(_overlayRef, retries: 5, delayMs: 80);
                if (!attached)
                {
                    var fallback = await TryAttachWithRetryAsync(dialogElement, retries: 3, delayMs: 80);
                    if (fallback)
                    {
                        _portalAttachedElementRef = dialogElement;
                        _portalAttached = true;
                    }
                    else
                    {
                        // Last-ditch: try attaching by selector string to match id
                        var selectorFallback = await TryAttachWithRetryAsync("#update-dialog", retries: 3, delayMs: 80);
                        if (selectorFallback)
                        {
                            _portalAttachedElementRef = _overlayRef; // overlay moved
                            _portalAttached = true;
                        }
                    }
                }
                else
                {
                    _portalAttached = true;
                    _portalAttachedElementRef = _overlayRef;
                }
            }

            // Re-verify if the portal is attached (re-render may have replaced the node)
            if (_portalAttached)
            {
                var stillAttached = await JSRuntime.InvokeAsync<bool>("textEditPortal.isAttached", _portalAttachedElementRef);
                if (!stillAttached)
                {
                    // try to reattach after a short delay
                    _portalAttached = false;
                    await Task.Delay(50);
                    var reattach = await TryAttachWithRetryAsync(_overlayRef, retries: 3, delayMs: 80);
                    if (!reattach)
                        reattach = await TryAttachWithRetryAsync(dialogElement, retries: 2, delayMs: 80);
                    if (reattach)
                    {
                        _portalAttached = true;
                        _portalAttachedElementRef = _overlayRef;
                    }
                }
            }

            _dotNetRef ??= DotNetObjectReference.Create(this);
            await JSRuntime.InvokeVoidAsync("textEditFocusTrap.trap", "#update-dialog", _dotNetRef, "HandleRemindLater");

            try { await dialogElement.FocusAsync(); } catch { }
        }
    }

    private async Task HandleInstallInternalAsync()
    {
        if (_isClosing) return;
        _isClosing = true;
        await OnInstall.InvokeAsync();
        if (_portalAttached)
        {
            try { await JSRuntime.InvokeVoidAsync("textEditFocusTrap.release", "#update-dialog"); } catch { }
            await TryDetachPortalAsync(_portalAttachedElementRef);
            _portalAttached = false;
        }
    }

    // Wrappers used by markup / focus trap
    public async Task HandleInstall() => await HandleInstallInternalAsync();

    public async Task OnDownloadClick(MouseEventArgs e) => await OnDownloadClickInternal(e);

    public async Task HandleRemindLater() => await HandleRemindLaterInternalAsync();

    public async Task HandleBackdropClick() => await HandleBackdropClickInternalAsync();

    public async Task HandleKeyDown(KeyboardEventArgs e) => await HandleKeyDownInternalAsync(e);

    public Task HandleButtonKeyDown(KeyboardEventArgs e) => Task.CompletedTask;

    private async Task OnDownloadClickInternal(MouseEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(UpdateMetadata?.DownloadUrl)) return;
        try { await IpcBridge.OpenExternalAsync(UpdateMetadata.DownloadUrl); } catch { }
    }

    private async Task HandleRemindLaterInternalAsync()
    {
        if (_isClosing) return;
        _isClosing = true;
        await OnRemindLater.InvokeAsync();
        if (_portalAttached)
        {
            try { await JSRuntime.InvokeVoidAsync("textEditFocusTrap.release", "#update-dialog"); } catch { }
            await TryDetachPortalAsync(_portalAttachedElementRef);
            _portalAttached = false;
        }
    }

    private async Task HandleBackdropClickInternalAsync()
    {
        if (UpdateMetadata?.IsCritical == true) return;
        await HandleRemindLaterInternalAsync();
    }

    private async Task HandleKeyDownInternalAsync(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
        {
            if (UpdateMetadata?.IsCritical == true) return;
            await HandleRemindLaterInternalAsync();
        }
    }

    public void Dispose()
    {
        _dotNetRef?.Dispose();
        if (_portalAttached)
        {
            _ = JSRuntime.InvokeVoidAsync("textEditFocusTrap.release", "#update-dialog");
            _ = JSRuntime.InvokeAsync<System.Object>("textEditPortal.detach", _portalAttachedElementRef);
            _portalAttached = false;
        }
    }

    public string FormatFileSize(long bytes)
    {
        if (bytes <= 0) return "0 B";
        var sizes = new[] { "B", "KB", "MB", "GB", "TB" };
        var order = (int)Math.Floor(Math.Log(bytes, 1024));
        order = Math.Min(order, sizes.Length - 1);
        var adjusted = bytes / Math.Pow(1024, order);
        return $"{adjusted:0.##} {sizes[order]}";
    }
}
