using Avalonia.Controls;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Avalonia.Platform;

public sealed class AppleWKWebViewEnvironmentRequestedEventArgs : WebViewEnvironmentRequestedEventArgs
{
    /// <summary>
    /// Creates a new data store object that stores website data in memory, and does not write that data to disk.
    /// </summary>
    public bool NonPersistentDataStore { get; set; }

    /// <summary>
    /// Creates a new persistent data store object with the unique identifier you provide.
    /// </summary>
    public string? DataStoreIdentifier { get; set; }

    /// <summary>
    /// The app name that appears in the user agent string.
    /// </summary>
    public string? ApplicationNameForUserAgent { get; set; }

    /// <summary>
    /// A Boolean value that indicates whether the web view should automatically upgrade supported HTTP requests to HTTPS.
    /// </summary>
    public bool UpgradeKnownHostsToHTTPS { get; set; } = true;

    /// <summary>
    /// A Boolean value that indicates whether the web view limits navigation to pages within the app’s domain.
    /// </summary>
    public bool LimitsNavigationsToAppBoundDomains { get; set; } = false;
}
