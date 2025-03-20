using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;

namespace Avalonia.Controls.WebView.Samples.Android;

[Activity(
    Label = "Avalonia.Controls.WebView.Samples",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder);
    }
}
