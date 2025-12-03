using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;
using Avalonia.Controls.Win.WebView1.Interop;

namespace Avalonia.Controls.Win.Interop;

#if COM_SOURCE_GEN
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("B403CA50-7F8C-4E83-985F-CC45060036D8")]
internal partial interface ICompositor : IInspectable
{
    IntPtr CreateColorKeyFrameAnimation();
    IntPtr CreateColorBrush();
    IntPtr CreateColorBrushWithColor(winrtColor color);
    IContainerVisual CreateContainerVisual();
}
