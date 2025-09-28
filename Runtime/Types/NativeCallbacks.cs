using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Aviad
{
    public class NativeCallbacks
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LogCallbackWithLevelDelegate(int level, [MarshalAs(UnmanagedType.LPStr)] string message);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void TokenStreamCallback([MarshalAs(UnmanagedType.LPStr)] string token);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void StreamDoneCallback([MarshalAs(UnmanagedType.I1)] bool done);

        public static TokenStreamCallback TokenStreamCallbackFromAction(Action<string> action)
        {
            if (action == null) return null;
            return new TokenStreamCallback(action.Invoke);
        }


        public static StreamDoneCallback StreamDoneCallbackFromAction(Action<bool> action)
        {
            if (action == null) return null;
            return new StreamDoneCallback(action.Invoke);
        }
    }
}