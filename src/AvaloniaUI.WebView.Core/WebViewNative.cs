namespace AvaloniaUI.WebView;

public class WebViewOptions
{
    public string? WebViewNativePath { get; set; }

    /// <remark>
    /// Currently only supported on macOS.
    /// Might block application from being uploaded to the AppStore.
    /// </remark>
    public bool EnableDevTools { get; set; }
}
