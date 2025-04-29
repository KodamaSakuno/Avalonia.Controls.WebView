using System;
using System.Text;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using static Avalonia.Controls.Gtk.GtkInterop;
using static Avalonia.Controls.Gtk.AvaloniaGtk;

namespace Avalonia.Controls.Gtk;

internal unsafe class GtkOffscreenWebViewAdapter : GtkWebViewAdapter,
    IWebViewAdapterWithOffscreenBuffer, IWebViewAdapterWithOffscreenInput
{
    private readonly bool _useGtkOffscreen;
    private readonly IntPtr _windowHandle;

    public GtkOffscreenWebViewAdapter(bool useGtkOffscreen = true)
    {
        _useGtkOffscreen = useGtkOffscreen;
        _windowHandle = RunOnGlibThread(() =>
        {
            var window = useGtkOffscreen ? gtk_offscreen_window_new() : gtk_window_new(0 /* GTK_WINDOW_TOPLEVEL */);
            gtk_window_set_default_size(window, 100, 100);
            return window;
        });

        RunOnGlibThreadAsync(() =>
        {
            gtk_container_add(_windowHandle, Handle);
            gtk_widget_set_has_window(Handle, true);
            gtk_widget_realize(Handle);
            gtk_widget_show_all(_windowHandle);

            if (!useGtkOffscreen)
            {
                //gtk_widget_hide(_windowHandle);
            }

            return 0;
        });
    }

    public void UpdateWriteableBitmap(ref WriteableBitmap? bitmap)
    {
        var inBitmap = bitmap;
        bitmap = RunOnGlibThreadAsync(() =>
        {
            IntPtr pixbuf;
            if (_useGtkOffscreen)
            {
                pixbuf = gtk_offscreen_window_get_pixbuf(_windowHandle);
            }
            else
            {
                var gdkWindow = gtk_widget_get_window(Handle);
                int wWidth = gtk_widget_get_allocated_width(_windowHandle);
                int wHeight = gtk_widget_get_allocated_height(_windowHandle);

                pixbuf = gdk_pixbuf_get_from_window(gdkWindow, 0, 0, wWidth, wHeight);
                if (pixbuf != IntPtr.Zero && gdk_pixbuf_get_n_channels(pixbuf) == 3)
                {
                    var pixbufRgba = gdk_pixbuf_add_alpha(pixbuf, false, 0, 0, 0);
                    pixbuf = pixbufRgba;
                }
            }

            if (pixbuf == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                var width = gdk_pixbuf_get_width(pixbuf);
                var height = gdk_pixbuf_get_height(pixbuf);
                var stride = gdk_pixbuf_get_rowstride(pixbuf);
                var channels = gdk_pixbuf_get_n_channels(pixbuf);
                var pixelsPtr = gdk_pixbuf_get_pixels(pixbuf);

                var size = new PixelSize(width, height);
                var dpi = new Vector(96, 96);
                var format = PixelFormat.Rgba8888;
                var alpha = AlphaFormat.Unpremul;

                if (channels == 4)
                {
                    if (inBitmap == null || inBitmap.PixelSize != size)
                    {
                        return new WriteableBitmap(
                            format,
                            alpha,
                            pixelsPtr,
                            size,
                            dpi,
                            stride
                        );
                    }

                    // Reuse existing WriteableBitmap — copy data directly
                    using var buf = inBitmap.Lock();
                    var bytesPerRow = Math.Min(stride, buf.RowBytes);
                    var totalBytes = bytesPerRow * height;

                    Buffer.MemoryCopy(
                        source: (void*)pixelsPtr,
                        destination: (void*)buf.Address,
                        destinationSizeInBytes: buf.RowBytes * height,
                        sourceBytesToCopy: totalBytes
                    );
                    return inBitmap;
                }

                return null;
            }
            finally
            {
                g_object_unref(pixbuf);
            }
        }).GetAwaiter().GetResult();
    }

    private PixelSize _sizeRequest;
    public override void SizeChanged(PixelSize containerSize)
    {
        _sizeRequest = containerSize;
        RunOnGlibThreadAsync(() =>
        {
            if (_useGtkOffscreen)
                gtk_window_set_default_size(_windowHandle, _sizeRequest.Width, _sizeRequest.Height);
            else
                gtk_window_resize(_windowHandle, _sizeRequest.Width, _sizeRequest.Height);
        });
    }

    public bool KeyInput(bool press, PhysicalKey physical, string? symbol, KeyModifiers modifiers)
    {
        return RunOnGlibThread(() =>
        {
#if NET6_0_OR_GREATER
            var count = Encoding.UTF8.GetByteCount(symbol ?? "");
            Span<byte> bytes = stackalloc byte[count];
            Encoding.UTF8.GetBytes(symbol ?? "", bytes);
            var bytesPtr = (byte*)bytes.GetPinnableReference();
#else
            var bytes = Encoding.UTF8.GetBytes(symbol ?? "");
            fixed (byte* bytesPtr = bytes)
#endif
            {
                using var state = new EventSendState(press ? GdkEventType.GDK_KEY_PRESS : GdkEventType.GDK_KEY_RELEASE, Handle, _windowHandle);
                var ev = state.Event;
                ev->key.time = 0;
                ev->key.length = symbol?.Length ?? 0;
                ev->key._string = bytesPtr;
                ev->key.keyval = 0; // ?
                ev->key.hardware_keycode = KeyTransform.ScanCodeFromPhysicalKey(physical);
                ev->key.state = ToGtk(modifiers, null);
                return state.Send();
            }
        });
    }

    public bool PointerInput(PointerPoint point, KeyModifiers modifiers)
    {
        var (eventType, button) = point.Properties.PointerUpdateKind switch
        {
            PointerUpdateKind.LeftButtonPressed => (GdkEventType.GDK_BUTTON_PRESS, 1u),
            PointerUpdateKind.MiddleButtonPressed => (GdkEventType.GDK_BUTTON_PRESS, 3u),
            PointerUpdateKind.RightButtonPressed => (GdkEventType.GDK_BUTTON_PRESS, 2u),
            PointerUpdateKind.XButton1Pressed => (GdkEventType.GDK_BUTTON_PRESS, 4u),
            PointerUpdateKind.XButton2Pressed => (GdkEventType.GDK_BUTTON_PRESS, 5u),
            PointerUpdateKind.LeftButtonReleased => (GdkEventType.GDK_BUTTON_RELEASE, 1u),
            PointerUpdateKind.MiddleButtonReleased => (GdkEventType.GDK_BUTTON_RELEASE, 3u),
            PointerUpdateKind.RightButtonReleased => (GdkEventType.GDK_BUTTON_RELEASE, 2u),
            PointerUpdateKind.XButton1Released => (GdkEventType.GDK_BUTTON_RELEASE, 4u),
            PointerUpdateKind.XButton2Released => (GdkEventType.GDK_BUTTON_RELEASE, 5u),
            PointerUpdateKind.Other => (GdkEventType.GDK_MOTION_NOTIFY, 0u),
            _ => (GdkEventType.GDK_NOTHING, 0u)
        };

        if (eventType == GdkEventType.GDK_NOTHING)
        {
            return false;
        }

        return RunOnGlibThread(() =>
        {
            var gdisplay = gdk_display_get_default();
            var seat = gdk_display_get_default_seat (gdisplay);
            var gdevice = gdk_seat_get_pointer (seat);

            using var state = new EventSendState(eventType, Handle, _windowHandle);
            var ev = state.Event;

            //gdk_window_get_root_coords(gtk_widget_get_window(Handle), (int)point.Position.X, (int)point.Position.Y, out int rootX, out int rootY);

            if (eventType == GdkEventType.GDK_MOTION_NOTIFY)
            {
                ev->motion.time = 0;
                ev->motion.x = point.Position.X;
                ev->motion.y = point.Position.Y;
                ev->motion.state = ToGtk(modifiers, point.Properties);
                ev->motion.device = gdevice;
                //ev->motion.x_root = rootX;
                //ev->motion.y_root = rootY;
            }
            else
            {
                ev->button.time = 0;
                ev->button.x = point.Position.X;
                ev->button.y = point.Position.Y;
                ev->button.button = button;
                ev->button.state = ToGtk(modifiers, point.Properties);
                ev->button.device = gdevice;
                //ev->button.x_root = rootX;
                //ev->button.y_root = rootY;
            }

            return state.Send();
        });
    }

    public bool PointerWheelInput(Vector delta, PointerPoint point, KeyModifiers modifiers)
    {
        return RunOnGlibThread(() =>
        {
            var gdisplay = gdk_display_get_default();
            var seat = gdk_display_get_default_seat (gdisplay);
            var gdevice = gdk_seat_get_pointer (seat);

            var x = point.Position.X;
            var y = point.Position.X;
            
            using var state = new EventSendState(GdkEventType.GDK_SCROLL, Handle, _windowHandle);
            var ev = state.Event;
            ev->scroll.x = x;
            ev->scroll.y = y;
            ev->scroll.time = 0;
            ev->scroll.device = gdevice;
            ev->scroll.state = ToGtk(modifiers, point.Properties);

            ev->scroll.delta_x = delta.X;
            ev->scroll.delta_y = delta.Y;
            ev->scroll.direction = GdkScrollDirection.GDK_SCROLL_SMOOTH;

            //gdk_window_get_root_coords(_windowHandle, (int)x, (int)y, out int rootX, out int rootY);
            //ev->scroll.x_root = rootX;
            //ev->scroll.y_root = rootY;

            return state.Send();
        });
    }

    private static GdkModifierType ToGtk(KeyModifiers modifiers, PointerPointProperties? pointProperties)
    {
        var output = GdkModifierType.GDK_NO_MODIFIER_MASK;
        if (modifiers.HasFlag(KeyModifiers.Shift))
            output |= GdkModifierType.GDK_SHIFT_MASK;
        if (modifiers.HasFlag(KeyModifiers.Control))
            output |= GdkModifierType.GDK_CONTROL_MASK;
        if (modifiers.HasFlag(KeyModifiers.Alt))
            output |= GdkModifierType.GDK_ALT_MASK;
        if (modifiers.HasFlag(KeyModifiers.Meta))
            output |= GdkModifierType.GDK_META_MASK;
        if (pointProperties?.IsLeftButtonPressed == true)
            output |= GdkModifierType.GDK_BUTTON1_MASK;
        if (pointProperties?.IsRightButtonPressed == true)
            output |= GdkModifierType.GDK_BUTTON2_MASK;
        if (pointProperties?.IsMiddleButtonPressed == true)
            output |= GdkModifierType.GDK_BUTTON3_MASK;
        if (pointProperties?.IsXButton1Pressed == true)
            output |= GdkModifierType.GDK_BUTTON4_MASK;
        if (pointProperties?.IsXButton2Pressed == true)
            output |= GdkModifierType.GDK_BUTTON5_MASK;
        return output;
    }

    private readonly ref struct EventSendState : IDisposable
    {
        private readonly IntPtr _handle;
        private readonly IntPtr _evPtr;

        public EventSendState(GdkEventType eventType, IntPtr handle, IntPtr windowPtr)
        {
            _handle = handle;
            _evPtr = gdk_event_new(eventType);
            var ev = (GdkEvent*)_evPtr.ToPointer();
            var gdkWindow = gtk_widget_get_window(handle);
            ev->any.window = gdkWindow;
            ev->any.send_event = 1;
            g_object_ref(ev->any.window);
        }

        public GdkEvent* Event => (GdkEvent*)_evPtr.ToPointer();

        public bool Send()
        {
            //while (gtk_events_pending())
                //gtk_main_iteration_do(false);

            gdk_event_put(_evPtr);
            return true;
                //return gtk_widget_event(_handle, _evPtr);
                //gtk_main_do_event(_evPtr);
                //return true;
        }

        public void Dispose()
        {
            if (_evPtr != IntPtr.Zero)
            {
                gdk_event_free(_evPtr);
            }
        }
    }
}
