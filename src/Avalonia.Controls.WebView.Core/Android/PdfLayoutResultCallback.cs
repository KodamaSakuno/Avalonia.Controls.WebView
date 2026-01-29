#if ANDROID
using System;
using System.Threading.Tasks;
using Android.OS;
using Android.Print;
using Android.Runtime;
using Android.Util;
using Java.Lang;
using Exception = Java.Lang.Exception;

namespace Avalonia.Controls.Android;

[Register("android/print/PdfLayoutResultCallback")]
internal class PdfLayoutResultCallback : PrintDocumentAdapter.LayoutResultCallback
{
    public PdfLayoutResultCallback(IntPtr javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer) { }

    public PdfLayoutResultCallback() : base(IntPtr.Zero, JniHandleOwnership.DoNotTransfer)
    {
        if (Handle == IntPtr.Zero)
        {
            unsafe
            {
                var val = JniPeerMembers!.InstanceMethods.StartCreateInstance("()V", GetType(), null);
                SetHandle(val.Handle, JniHandleOwnership.TransferLocalRef);
                JniPeerMembers!.InstanceMethods.FinishCreateInstance("()V", this, null);
            }
        }
    }

    public PrintDocumentAdapter? Adapter { get; set; }
    public TaskCompletionSource<string> TaskCompletionSource { get; } = new();

    public override void OnLayoutFinished(PrintDocumentInfo? info, bool changed)
    {
        try
        {
            var tempFile = System.IO.Path.GetTempFileName();
            var file = new Java.IO.File(tempFile);
            var fileDescriptor = ParcelFileDescriptor.Open(file, ParcelFileMode.ReadWrite);
            var writeResultCallback = new PdfWriteResultCallback(PDFToHtml);
            Adapter.OnWrite(new[] { PageRange.AllPages! }, fileDescriptor, new CancellationSignal(), writeResultCallback);
        }
        catch (Exception ex)
        {
            TaskCompletionSource.SetException(ex);
        }

        base.OnLayoutFinished(info, changed);
    }

    public override void OnLayoutCancelled()
    {
        base.OnLayoutCancelled();
        TaskCompletionSource.SetCanceled();
    }

    public override void OnLayoutFailed(ICharSequence? error)
    {
        base.OnLayoutFailed(error);
        TaskCompletionSource.SetException(new AndroidException(error?.ToString()));
    }
}
#endif