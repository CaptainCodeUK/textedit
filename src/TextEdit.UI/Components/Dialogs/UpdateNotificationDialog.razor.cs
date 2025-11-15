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

    private async Task<bool> TryAttachPortalAsync(ElementReference elementRef)
    {
        try
        {
            var res = await JSRuntime.InvokeAsync<object>("textEditPortal.attach", elementRef);
            return res is bool b && b;
        }
        catch
        {
            return false;
        }
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
                var attached = await TryAttachPortalAsync(_overlayRef);
                if (!attached)
                {
                    var fallback = await TryAttachPortalAsync(dialogElement);
                    if (fallback)
                    {
                        _portalAttachedElementRef = dialogElement;
                        _portalAttached = true;
                    }
                }
                else
                {
                    _portalAttached = true;
                    _portalAttachedElementRef = _overlayRef;
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
