// Package not supported by NativeAOT as far as I can tell,
// so we create a dummy PublicAPIAttribute when deploying
#if IS_NATIVE_AOT
using System;
namespace JetBrains.Annotations
{
    public class PublicAPIAttribute : Attribute { }
}
#endif
