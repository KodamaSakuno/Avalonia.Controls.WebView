using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Controls.Utils;
using static Avalonia.Controls.Gtk.GtkInterop;

namespace Avalonia.Controls.Gtk;

internal sealed class SoupHttpHeaders(IntPtr headers) : INativeHttpRequestHeaders
{
    public bool Immutable => false;

    public bool TryClear()
    {
        soup_message_headers_clear(headers);
        return true;
    }

    public unsafe bool TryGetCount(out int count)
    {
        int[] boxedCount = [0];
        var userData = GCHandle.Alloc(boxedCount);
        try
        {
            soup_message_headers_foreach(headers, &MessageHeadersForeachFunc, userData);
        }
        finally
        {
            userData.Free();
        }

        count = boxedCount[0];
        return true;

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static void MessageHeadersForeachFunc(IntPtr namePtr, IntPtr valuePtr, IntPtr data)
        {
            if (data == IntPtr.Zero || GCHandle.FromIntPtr(data).Target is not int[] count)
            {
                return;
            }

            count[0] += 1;
        }
    }

    public string? GetHeader(string name)
    {
        var namePtr = Marshal.StringToHGlobalAnsi(name);
        try
        {
            var handle = soup_message_headers_get_list(headers, namePtr);
            return Marshal.PtrToStringAnsi(handle);
        }
        finally
        {
            Marshal.FreeHGlobal(namePtr);
        }
    }

    public bool Contains(string name)
    {
        var namePtr = Marshal.StringToHGlobalAnsi(name);
        try
        {
            var handle = soup_message_headers_get_one(headers, namePtr);
            return !string.IsNullOrEmpty(Marshal.PtrToStringAnsi(handle));
        }
        finally
        {
            Marshal.FreeHGlobal(namePtr);
        }
    }

    public bool TrySetHeader(string name, string value)
    {
        var namePtr = Marshal.StringToHGlobalAnsi(name);
        var valuePtr = Marshal.StringToHGlobalAnsi(value);
        try
        {
            soup_message_headers_replace(headers, namePtr, valuePtr);
            return true;
        }
        finally
        {
            Marshal.FreeHGlobal(valuePtr);
            Marshal.FreeHGlobal(namePtr);
        }
    }

    public bool TryRemoveHeader(string name)
    {
        var namePtr = Marshal.StringToHGlobalAnsi(name);
        try
        {
            soup_message_headers_remove(headers, namePtr);
            return true;
        }
        finally
        {
            Marshal.FreeHGlobal(namePtr);
        }
    }

    public unsafe INativeHttpHeadersCollectionIterator GetIterator()
    {
        var dictionary = new Dictionary<string, string>();
        var userData = GCHandle.Alloc(dictionary);
        try
        {
            soup_message_headers_foreach(headers, &MessageHeadersForeachFunc, userData);
        }
        finally
        {
            userData.Free();
        }

        return new DictionaryNativeHttpRequestHeaders.Iterator(dictionary);

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static void MessageHeadersForeachFunc(IntPtr namePtr, IntPtr valuePtr, IntPtr data)
        {
            if (data == IntPtr.Zero || GCHandle.FromIntPtr(data).Target is not Dictionary<string, string> adapter)
            {
                return;
            }

            var name = Marshal.PtrToStringAnsi(namePtr);
            var value = Marshal.PtrToStringAnsi(valuePtr);
            if (!string.IsNullOrEmpty(name))
            {
                adapter[name] = value ?? "";
            }
        }
    }
}
