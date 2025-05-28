using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

#if COM_SOURCE_GEN
[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("2FDE08A8-1E9A-4766-8C05-95A9CEB9D1C5")]
internal partial interface ICoreWebView2EnvironmentOptions
{
    [DispId(1610678272)]
    [return: MarshalAs(UnmanagedType.LPWStr)]
    string? GetAdditionalBrowserArguments();

    [DispId(1610678273)]
    void SetAdditionalBrowserArguments([MarshalAs(UnmanagedType.LPWStr)] string additionalBrowserArguments);

    [DispId(1610678274)]
    [return: MarshalAs(UnmanagedType.LPWStr)]
    string? GetLanguage();

    [DispId(1610678275)]
    void SetLanguage([MarshalAs(UnmanagedType.LPWStr)] string language);

    [DispId(1610678276)]
    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetTargetCompatibleBrowserVersion();

    [DispId(1610678277)]
    void SetTargetCompatibleBrowserVersion([MarshalAs(UnmanagedType.LPWStr)] string targetCompatibleBrowserVersion);

    [DispId(1610678278)]
    int GetAllowSingleSignOnUsingOSPrimaryAccount();

    [DispId(1610678279)]
    void SetAllowSingleSignOnUsingOSPrimaryAccount(int allowSingleSignOnUsingOSPrimaryAccount);
}
