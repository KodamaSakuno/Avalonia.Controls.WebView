#if !ANDROID && (NET6_0_OR_GREATER || NETFRAMEWORK)
using System;
using System.ComponentModel;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia.Controls.Win.WebView2.Interop;
using Avalonia.Platform;

namespace Avalonia.Controls.Win.WebView2;

[SupportedOSPlatform("windows6.1")] // win7
internal partial class WebView2HwndAdapter(IPlatformHandle handle) : WebView2BaseAdapter(handle)
{
    public override IntPtr Handle { get; } = handle.Handle;
    public override string HandleDescriptor { get; } = handle.HandleDescriptor!; // Expected to be HWND always.

    protected override Task<ICoreWebView2Controller> CreateWebView2Controller(ICoreWebView2Environment env, IntPtr handle)
    {
        var handler = new WebView2ControllerHandler();
        env.CreateCoreWebView2Controller(handle, handler);
        return handler.Result.Task;
    }

    [GeneratedComClass]
    private partial class WebView2ControllerHandler : CallbackBase, ICoreWebView2CreateCoreWebView2ControllerCompletedHandler
    {
        public TaskCompletionSource<ICoreWebView2Controller> Result { get; } = new();
        public void Invoke(int errorCode, ICoreWebView2Controller result)
        {
            if (errorCode != 0)
                Result?.TrySetException(new Win32Exception(errorCode));
            else
                Result?.TrySetResult(result);
        }
    }
}
#endif
