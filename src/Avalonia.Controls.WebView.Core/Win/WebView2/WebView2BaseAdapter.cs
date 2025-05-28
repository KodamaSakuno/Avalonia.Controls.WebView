#if !ANDROID && (NET6_0_OR_GREATER || NETFRAMEWORK)
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Avalonia.Controls.Utils;
using Avalonia.Controls.Win.WebView2.Interop;
using Avalonia.Logging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.Controls.Win.WebView2;

[SupportedOSPlatform("windows6.1")] // win7
internal abstract partial class WebView2BaseAdapter : IWebViewAdapterWithCookieManager
{
    private ICoreWebView2Controller? _controller;
    private Action? _subscriptions;

    protected WebView2BaseAdapter(IPlatformHandle parent)
    {
        Initialize(parent);
    }

    public abstract IntPtr Handle { get; }
    public abstract string? HandleDescriptor { get; }

    protected unsafe ICoreWebView2? TryGetWebView2()
    {
        try
        {
            return _controller?.GetCoreWebView2();
        }
        // That's what WPF control does.
        catch (COMException ex) when (ex.HResult == -2147019873)
        {
            return null;
        }
    }

    public bool IsInitialized { get; private set; }

    public bool CanGoBack => true;// TryGetWebView2()?.CanGoBack ?? false;

    public bool CanGoForward => true;// TryGetWebView2()?.CanGoForward ?? false;

    public Uri Source
    {
        get
        {
            return WebViewHelper.EmptyPage;
            //return Uri.TryCreate(TryGetWebView2()?.Source, UriKind.Absolute, out var url) ? url : null!;
        }
        set => Navigate(value);
    }

    public event EventHandler<WebViewNavigationCompletedEventArgs>? NavigationCompleted;
    public event EventHandler<WebViewNavigationStartingEventArgs>? NavigationStarted;
    public event EventHandler<WebViewNewWindowRequestedEventArgs>? NewWindowRequested;
    public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;
    public event EventHandler? Initialized;

    public bool GoBack()
    {
        if (TryGetWebView2() is { } webView2)
        {
            webView2.GoBack();
            return true;
        }
        return false;
    }

    public bool GoForward()
    {
        if (TryGetWebView2() is { } webView2)
        {
            webView2.GoForward();
            return true;
        }
        return false;
    }

    public Task<string?> InvokeScript(string scriptName)
    {
        return Task.FromResult<string?>(null);
        //return TryGetWebView2()?.ExecuteScriptAsync(scriptName) ?? Task.FromResult<string?>(null);
    }

    public void Navigate(Uri url)
    {
        TryGetWebView2()?.Navigate(url.AbsoluteUri);
    }

    public void NavigateToString(string text)
    {
        //TryGetWebView2()?.NavigateToString(text);
    }

    public bool Refresh()
    {
        TryGetWebView2()?.Reload();
        return true;
    }

    public bool Stop()
    {
        //TryGetWebView2()?.Stop();
        return true;
    }

    public virtual void SizeChanged(PixelSize containerSize)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (PInvoke.GetWindowRect(new HWND(Handle), out var rect)
                && _controller is not null)
            {
                //_controller.BoundsMode = CoreWebView2BoundsMode.UseRawPixels;
                _controller.SetBounds(new tagRECT
                {
                    right = rect.Width,
                    bottom = rect.Height
                });
                _controller.NotifyParentWindowPositionChanged();
            }
        });
    }

    public virtual void SetParent(IPlatformHandle parent)
    {
        if (_controller is null)
            return;

        if (parent.HandleDescriptor != "HWND")
            throw new InvalidOperationException("IPlatformHandle.HandleDescriptor must be HWND");

        _controller.SetParentWindow(parent.Handle);
    }

    private async void Initialize(IPlatformHandle parentHost)
    {
        var env = await CoreWebView2Environment.CreateAsync();
        var controller = await CreateWebView2Controller(env, parentHost.Handle);
        var webView = controller.GetCoreWebView2();

        //webView.AddScriptToExecuteOnDocumentCreated(
          //  "function invokeCSharpAction(data){window.chrome.webview.postMessage(data);}", IntPtr.Zero);
        controller.SetIsVisible(1);

        if (controller is ICoreWebView2Controller3 controller3)
        {
            controller3.SetShouldDetectMonitorScaleChanges(0);
        }

        _controller = controller;

        SizeChanged(default);

        _subscriptions = AddHandlers(webView);

        IsInitialized = true;
        Initialized?.Invoke(this, EventArgs.Empty);
    }

    protected abstract Task<ICoreWebView2Controller> CreateWebView2Controller(ICoreWebView2Environment env, IntPtr handle);

    private Action AddHandlers(ICoreWebView2 webView)
    {
        // webView.NavigationStarting += WebViewOnNavigationStarting;
        //
        // void WebViewOnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        // {
        //     if (Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri))
        //     {
        //         var args = new WebViewNavigationStartingEventArgs { Request = uri };
        //         NavigationStarted?.Invoke(this, args);
        //         if (args.Cancel) e.Cancel = true;
        //     }
        // }
        //
        // webView.NavigationCompleted += WebViewOnNavigationCompleted;
        //
        // void WebViewOnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        // {
        //     NavigationCompleted?.Invoke(this,
        //         new WebViewNavigationCompletedEventArgs
        //         {
        //             Request = new Uri(((CoreWebView2)sender!).Source),
        //             IsSuccess = e.IsSuccess
        //         });
        // }
        //
        // webView.WebMessageReceived += WebViewOnWebMessageReceived;
        //
        // void WebViewOnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        // {
        //     string? message = null;
        //
        //     try
        //     {
        //         // this `Try` method can throw undescriptive ArgumentException. Keep going WinRT.
        //         message = e.TryGetWebMessageAsString();
        //     }
        //     catch
        //     {
        //         // ignore
        //     }
        //
        //     message ??= e.WebMessageAsJson;
        //
        //     WebMessageReceived?.Invoke(this, new WebMessageReceivedEventArgs { Body = message });
        // }
        //
        // webView.NewWindowRequested += WebViewOnNewWindowRequested;
        //
        // void WebViewOnNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
        // {
        //     if (Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri))
        //     {
        //         var args = new WebViewNewWindowRequestedEventArgs { Request = uri };
        //         NewWindowRequested?.Invoke(this, args);
        //         if (args.Handled) e.Handled = true;
        //     }
        // }

        return () =>
        {
            // webView.NavigationStarting -= WebViewOnNavigationStarting;
            // webView.NavigationCompleted -= WebViewOnNavigationCompleted;
            // webView.WebMessageReceived -= WebViewOnWebMessageReceived;
        };
    }

    public void AddOrUpdateCookie(Cookie cookie)
    {
        if (TryGetWebView2() is { } webView)
        {
            // var webViewCookie = webView.CookieManager.CreateCookieWithSystemNetCookie(cookie);
            // webView.CookieManager.AddOrUpdateCookie(webViewCookie);
        }
    }

    public void DeleteCookie(string name, string domain, string path)
    {
        //TryGetWebView2()?.CookieManager.DeleteCookiesWithDomainAndPath(name, domain, path);
    }

    public Task<IReadOnlyList<Cookie>> GetCookiesAsync()
    {
        return Task.FromResult<IReadOnlyList<Cookie>>([]);
        // if (TryGetWebView2() is not { } webView)
        //     return [];
        // var cookies = await webView.CookieManager.GetCookiesAsync(null);
        // return cookies.Select(c => c.ToSystemNetCookie()).ToArray();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _controller?.Close();
            _subscriptions?.Invoke();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~WebView2BaseAdapter()
    {
        Dispose(false);
    }
}
#endif
