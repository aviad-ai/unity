// Derived from:
// https://github.com/ammariqais/SkiaForUnity/blob/e6ef71b0f1ccc403812e4b60409608b0ea0748c0/SkiaUnity/Assets/SkiaSharp/SkiaSharp-Bindings/SkiaSharp.HarfBuzz.Shared/HarfBuzzSharp.Shared/LibraryLoader.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Aviad
{
    internal static class LibraryLoader
    {
        private static readonly Dictionary<string, LibraryInfo> _loadedLibraries = new Dictionary<string, LibraryInfo>();
        private static readonly object _lock = new object();

        private struct LibraryInfo
        {
            public IntPtr Handle;
            public int LoadCount;
            public string FullPath;
        }

        static LibraryLoader()
        {
        }

        public static IntPtr LoadLibrary(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            lock (_lock)
            {
                // Normalize the path to handle relative/absolute path comparisons
                string normalizedPath = Path.GetFullPath(path);

                // Check if library is already loaded
                if (_loadedLibraries.TryGetValue(normalizedPath, out LibraryInfo existingLib))
                {
                    // Increment reference count and return existing handle
                    existingLib.LoadCount++;
                    _loadedLibraries[normalizedPath] = existingLib;
                    PackageLogger.Debug($"[Aviad] Library '{normalizedPath}' already loaded. Reference count: {existingLib.LoadCount}");
                    return existingLib.Handle;
                }

                // Load the library for the first time
                IntPtr handle = LoadLibraryInternal(path);

                if (handle != IntPtr.Zero)
                {
                    // Store library info with reference count of 1
                    _loadedLibraries[normalizedPath] = new LibraryInfo
                    {
                        Handle = handle,
                        LoadCount = 1,
                        FullPath = normalizedPath
                    };
                    PackageLogger.Debug($"[Aviad] Successfully loaded library '{normalizedPath}'. Handle: {handle}");
                }

                return handle;
            }
        }

        private static IntPtr LoadLibraryInternal(string path)
        {
            IntPtr handle;
            if (PlatformConfiguration.IsWindows)
                handle = Win32.LoadLibrary(path);
            else if (PlatformConfiguration.IsLinux)
                handle = Linux.dlopen(path);
            else if (PlatformConfiguration.IsMac)
                handle = Mac.dlopen(path);
            else
                throw new PlatformNotSupportedException($"Current platform is unknown, unable to load library '{path}'.");

            if (handle == IntPtr.Zero)
            {
                PackageLogger.Error($"[Aviad] LoadLibrary failed for '{path}'.");
            }

            return handle;
        }

        public static T GetSymbolDelegate<T>(IntPtr library, string symbolName) where T : Delegate
        {
            if (library == IntPtr.Zero)
                throw new ArgumentException("Invalid library handle.", nameof(library));

            if (string.IsNullOrEmpty(symbolName))
                throw new ArgumentNullException(nameof(symbolName));

            IntPtr symbol;
            if (PlatformConfiguration.IsWindows)
                symbol = Win32.GetProcAddress(library, symbolName);
            else if (PlatformConfiguration.IsLinux)
                symbol = Linux.dlsym(library, symbolName);
            else if (PlatformConfiguration.IsMac)
                symbol = Mac.dlsym(library, symbolName);
            else
                throw new PlatformNotSupportedException($"Current platform is unknown, unable to load symbol '{symbolName}' from library {library}.");

            if (symbol == IntPtr.Zero)
            {
                PackageLogger.Error($"[Aviad] Failed to load symbol '{symbolName}' from library handle {library}");
                throw new EntryPointNotFoundException($"Unable to load symbol '{symbolName}'.");
            }
            return Marshal.GetDelegateForFunctionPointer<T>(symbol);
        }

        public static void FreeLibrary(IntPtr library)
        {
            if (library == IntPtr.Zero)
                return;

            lock (_lock)
            {
                // Find the library by handle
                string libraryPath = null;
                LibraryInfo libInfo = default;

                foreach (var kvp in _loadedLibraries)
                {
                    if (kvp.Value.Handle == library)
                    {
                        libraryPath = kvp.Key;
                        libInfo = kvp.Value;
                        break;
                    }
                }

                if (libraryPath == null)
                {
                    PackageLogger.Warning($"[Aviad] Attempted to free unknown library handle {library}");
                    // Still attempt to free it in case it was loaded outside our tracking
                    FreeLibraryInternal(library);
                    return;
                }

                // Decrement reference count
                libInfo.LoadCount--;

                if (libInfo.LoadCount <= 0)
                {
                    // No more references, actually free the library
                    FreeLibraryInternal(library);
                    _loadedLibraries.Remove(libraryPath);
                    PackageLogger.Debug($"[Aviad] Library '{libraryPath}' unloaded and removed from cache.");
                }
                else
                {
                    // Still has references, just update the count
                    _loadedLibraries[libraryPath] = libInfo;
                    PackageLogger.Debug($"[Aviad] Library '{libraryPath}' reference count decremented to {libInfo.LoadCount}");
                }
            }
        }

        private static void FreeLibraryInternal(IntPtr library)
        {
            if (PlatformConfiguration.IsWindows)
                Win32.FreeLibrary(library);
            else if (PlatformConfiguration.IsLinux)
                Linux.dlclose(library);
            else if (PlatformConfiguration.IsMac)
                Mac.dlclose(library);
            else
                throw new PlatformNotSupportedException($"Current platform is unknown, unable to close library '{library}'.");
        }

        /// <summary>
        /// Gets information about all currently loaded libraries.
        /// Useful for debugging and monitoring library usage.
        /// </summary>
        public static Dictionary<string, (IntPtr Handle, int LoadCount)> GetLoadedLibraries()
        {
            lock (_lock)
            {
                var result = new Dictionary<string, (IntPtr, int)>();
                foreach (var kvp in _loadedLibraries)
                {
                    result[kvp.Key] = (kvp.Value.Handle, kvp.Value.LoadCount);
                }
                return result;
            }
        }

        /// <summary>
        /// Force unloads all libraries regardless of reference count.
        /// </summary>
        public static void UnloadAllLibraries()
        {
            lock (_lock)
            {
                foreach (var kvp in _loadedLibraries)
                {
                    try
                    {
                        FreeLibraryInternal(kvp.Value.Handle);
                        PackageLogger.Debug($"[Aviad] Force unloaded library '{kvp.Key}'");
                    }
                    catch (Exception ex)
                    {
                        PackageLogger.Error($"[Aviad] Error force unloading library '{kvp.Key}': {ex.Message}");
                    }
                }
                _loadedLibraries.Clear();
            }
        }

        /// <summary>
        /// Checks if a library is currently loaded.
        /// </summary>
        public static bool IsLibraryLoaded(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            lock (_lock)
            {
                string normalizedPath = Path.GetFullPath(path);
                return _loadedLibraries.ContainsKey(normalizedPath);
            }
        }

#pragma warning disable IDE1006 // Naming Styles
        private static class Mac
        {
            private const string SystemLibrary = "/usr/lib/libSystem.dylib";

            private const int RTLD_LAZY = 1;
            private const int RTLD_NOW = 2;

            public static IntPtr dlopen(string path, bool lazy = true) =>
                dlopen(path, lazy ? RTLD_LAZY : RTLD_NOW);

            [DllImport(SystemLibrary)]
            public static extern IntPtr dlopen(string path, int mode);

            [DllImport(SystemLibrary)]
            public static extern IntPtr dlsym(IntPtr handle, string symbol);

            [DllImport(SystemLibrary)]
            public static extern void dlclose(IntPtr handle);
        }

        private static class Linux
        {
            private const string SystemLibrary = "libdl.so";
            private const string SystemLibrary2 = "libdl.so.2"; // newer Linux distros use this

            private const int RTLD_LAZY = 1;
            private const int RTLD_NOW = 2;

            private static bool UseSystemLibrary2 = true;

            public static IntPtr dlopen(string path, bool lazy = true)
            {
                try
                {
                    return dlopen2(path, lazy ? RTLD_LAZY : RTLD_NOW);
                }
                catch (DllNotFoundException)
                {
                    UseSystemLibrary2 = false;
                    return dlopen1(path, lazy ? RTLD_LAZY : RTLD_NOW);
                }
            }

            public static IntPtr dlsym(IntPtr handle, string symbol)
            {
                return UseSystemLibrary2 ? dlsym2(handle, symbol) : dlsym1(handle, symbol);
            }

            public static void dlclose(IntPtr handle)
            {
                if (UseSystemLibrary2)
                    dlclose2(handle);
                else
                    dlclose1(handle);
            }

            [DllImport(SystemLibrary, EntryPoint = "dlopen")]
            private static extern IntPtr dlopen1(string path, int mode);

            [DllImport(SystemLibrary, EntryPoint = "dlsym")]
            private static extern IntPtr dlsym1(IntPtr handle, string symbol);

            [DllImport(SystemLibrary, EntryPoint = "dlclose")]
            private static extern void dlclose1(IntPtr handle);

            [DllImport(SystemLibrary2, EntryPoint = "dlopen")]
            private static extern IntPtr dlopen2(string path, int mode);

            [DllImport(SystemLibrary2, EntryPoint = "dlsym")]
            private static extern IntPtr dlsym2(IntPtr handle, string symbol);

            [DllImport(SystemLibrary2, EntryPoint = "dlclose")]
            private static extern void dlclose2(IntPtr handle);
        }

        private static class Win32
        {
            private const string SystemLibrary = "Kernel32.dll";

            [DllImport(SystemLibrary, SetLastError = true, CharSet = CharSet.Ansi)]
            public static extern IntPtr LoadLibrary(string lpFileName);

            [DllImport(SystemLibrary, SetLastError = true, CharSet = CharSet.Ansi)]
            public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

            [DllImport(SystemLibrary, SetLastError = true, CharSet = CharSet.Ansi)]
            public static extern void FreeLibrary(IntPtr hModule);
        }
#pragma warning restore IDE1006 // Naming Styles
    }
}