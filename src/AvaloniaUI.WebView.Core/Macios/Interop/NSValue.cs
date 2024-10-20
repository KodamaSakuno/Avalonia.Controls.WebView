using System;

namespace AppleInterop;

internal abstract class NSValue(IntPtr handle, bool owns) : NSObject(handle, owns);
