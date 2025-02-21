using System;

namespace AvaloniaUI.WebView;


public record WebAuthenticatorOptions(Uri RequestUri, Uri CallbackUri)
{
    /// <summary>
    /// If true, WebAuthenticationBroker will avoid platform specific implementation option, and will use webview dialog window.
    /// </summary>
    public bool PreferNativeWebViewDialog { get; set; }
}

public record WebAuthenticationResult(Uri CallbackUri);
