using System;
using System.Runtime.InteropServices;
using System.Text;

namespace AvaloniaUI.WebView.NativeMac.Impl;

internal unsafe partial class __MicroComIAvnStringProxy
{
    private string? _managed;
    private byte[]? _bytes;

    public string? String
    {
        get
        {
            if (_managed == null)
            {
                var ptr = Pointer();
                if (ptr == null)
                    return null;
                _managed = Encoding.UTF8.GetString((byte*)ptr, Length());
            }

            return _managed;
        }
    }

    public byte[] Bytes
    {
        get
        {
            if (_bytes == null)
            {
                _bytes = new byte[Length()];
                Marshal.Copy(new IntPtr(Pointer()), _bytes, 0, _bytes.Length);
            }

            return _bytes;
        }
    }

    public override string? ToString() => String;
}
