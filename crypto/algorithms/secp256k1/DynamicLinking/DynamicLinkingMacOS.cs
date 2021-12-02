using System;
using System.Runtime.InteropServices;

namespace wallet_beautifier.crypto.algorithms.secp256k1.DynamicLinking
{
    static class DynamicLinkingMacOS
    {
        const string LIBDL = "libdl";

        [DllImport(LIBDL)]
        public static extern IntPtr dlopen(string path, int flags);

        [DllImport(LIBDL)]
        public static extern int dlclose(IntPtr handle);

        [DllImport(LIBDL)]
        public static extern IntPtr dlerror();

        [DllImport(LIBDL)]
        public static extern IntPtr dlsym(IntPtr handle, string name);
    }
}
