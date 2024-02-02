using System;
using System.Runtime.InteropServices;
using System.Text;

namespace AvaloniaUI.WebView.NativeMac;

internal partial interface IAvnString
{
    public string? String { get; }
    public byte[]? Bytes { get; }
}

internal class AvnString(string? s) : CallbackBase, IAvnString
{
    private IntPtr _native;
    private int _nativeLen;

    public string? String { get; } = s;
    public byte[] Bytes => Encoding.UTF8.GetBytes(String!);

    public unsafe void* Pointer()
    {
        EnsureNative();
        return _native.ToPointer();
    }

    public int Length()
    {
        EnsureNative();
        return _nativeLen;
    }

    protected override void Destroyed()
    {
        if (_native != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_native);
            _native = IntPtr.Zero;
        }
    }

    private unsafe void EnsureNative()
    {
        if (string.IsNullOrEmpty(String))
            return;
        if (_native == IntPtr.Zero)
        {
            _nativeLen = Encoding.UTF8.GetByteCount(String!);
            _native = Marshal.AllocHGlobal(_nativeLen + 1);
            var ptr = (byte*)_native.ToPointer();
            fixed (char* chars = String)
                Encoding.UTF8.GetBytes(chars, String!.Length, ptr, _nativeLen);
            ptr[_nativeLen] = 0;
        }
    }
}
