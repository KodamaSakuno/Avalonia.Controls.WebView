using System;
using Avalonia.Controls;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Avalonia.Platform;

public sealed class WindowsWebView1EnvironmentRequestedEventArgs : WebViewEnvironmentRequestedEventArgs
{
    /// <summary>
    /// The enterprise ID for apps that are Windows Information Protection-enabled.
    /// </summary>
    public IntPtr EnterpriseId { get; set; }

    /// <summary>
    /// A boolean value indicating whether the privateNetworkClientServer capability is enabled.
    /// </summary>
    public bool? PrivateNetworkClientServerEnabled { get; set; }
}
