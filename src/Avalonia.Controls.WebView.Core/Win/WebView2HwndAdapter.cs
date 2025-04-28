#if !ANDROID && (NET6_0_OR_GREATER || NETFRAMEWORK)
using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia.Platform;
using Microsoft.Web.WebView2.Core;

namespace Avalonia.Controls.Win;

[SupportedOSPlatform("windows6.1")] // win7
internal class WebView2HwndAdapter(IPlatformHandle handle) : WebView2BaseAdapter(handle)
{
    public override IntPtr Handle { get; } = handle.Handle;
    public override string HandleDescriptor { get; } = handle.HandleDescriptor!; // Expected to be HWND always.

    protected override Task<CoreWebView2Controller> CreateWebView2Controller(CoreWebView2Environment env, IntPtr handle)
    {
        return env.CreateCoreWebView2ControllerAsync(Handle);
    }
}
#endif
