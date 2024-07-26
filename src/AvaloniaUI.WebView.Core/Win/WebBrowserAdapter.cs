using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Avalonia.Platform;
using MicroCom.Runtime;

namespace AvaloniaUI.WebView.Win;

#if NET6_0_OR_GREATER
[SupportedOSPlatform("windows7.0")]
#endif
internal unsafe class WebBrowserAdapter : IWebViewAdapter
{
    private readonly IWebBrowser2 _webBrowser;

    public WebBrowserAdapter()
    {
        var guid = Guid.Parse("8856f961-340a-11d0-a96b-00c04fd705a2");
        var unknown = Guid.Parse("00000000-0000-0000-C000-000000000046");
        void* result;
        var res = PInvoke.CoCreateInstance(&guid, default, CLSCTX.CLSCTX_INPROC_SERVER, &unknown, &result);
        if (res != 0)
            throw new Win32Exception(res);

        using var browser = MicroComRuntime.CreateProxyFor<IWebBrowser>(result, false);
        _webBrowser = browser.QueryInterface<IWebBrowser2>();
        Handle = new IntPtr(result);
    }

    public IntPtr Handle { get; }
    public string? HandleDescriptor => "HWDN";
    public event EventHandler<WebViewNavigationCompletedEventArgs>? NavigationCompleted;
    public event EventHandler<WebViewNavigationStartingEventArgs>? NavigationStarted;
    public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;
    public bool CanGoBack => true;
    public bool CanGoForward => true;
    public Uri Source { get => throw new NotImplementedException(); set => Navigate(value); }

    public bool GoBack()
    {
        _webBrowser.GoBack();
        return true;
    }

    public bool GoForward()
    {
        _webBrowser.GoForward();
        return true;
    }

    public Task<string?> InvokeScript(string script)
    {
        return Task.FromResult<string?>(null);
    }

    public void Navigate(Uri url)
    {
        var str = Marshal.StringToBSTR(url.AbsoluteUri);
        int[] arr = [0];
        fixed (void* p = arr)
        {
            _webBrowser.Navigate(str, p, null, null, null);
        }

        Marshal.FreeBSTR(str);
    }

    public void NavigateToString(string text)
    {
        throw new NotImplementedException();
    }

    public bool Refresh()
    {
        throw new NotImplementedException();
    }

    public bool Stop()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        _webBrowser.Dispose();
    }

    public event EventHandler? Initialized;
    public bool IsInitialized => true;

    public void SizeChanged()
    {
    }
}
