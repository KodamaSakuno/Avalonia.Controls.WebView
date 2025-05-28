using System.Runtime.InteropServices;

namespace Avalonia.Controls.Win.WebView2.Interop;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct EventRegistrationToken
{
    public long value;
}
