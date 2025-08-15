using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Aviad
{
    [Serializable]
    public class Optional<T>
    {
        [SerializeField] public bool hasValue;
        [SerializeField] public T value;

        public Optional()
        {
        }

        public Optional(T value)
        {
            hasValue = true;
            this.value = value;
        }

        public bool HasValue => hasValue;

        public static implicit operator Optional<T>(T value)
        {
            return new Optional<T>(value);
        }
    }

    // Marshaling-friendly optional types for C++ interop
    [StructLayout(LayoutKind.Sequential)]
    public struct OptionalInt32
    {
        [MarshalAs(UnmanagedType.I1)] public bool has_value;
        [MarshalAs(UnmanagedType.I4)] public int value;

        public static OptionalInt32 None => new OptionalInt32 { has_value = false, value = 0 };
        public static OptionalInt32 Some(int val) => new OptionalInt32 { has_value = true, value = val };
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OptionalUInt32
    {
        [MarshalAs(UnmanagedType.I1)] public bool has_value;
        [MarshalAs(UnmanagedType.U4)] public uint value;

        public static OptionalUInt32 None => new OptionalUInt32 { has_value = false, value = 0 };
        public static OptionalUInt32 Some(uint val) => new OptionalUInt32 { has_value = true, value = val };
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OptionalFloat
    {
        [MarshalAs(UnmanagedType.I1)] public bool has_value;
        [MarshalAs(UnmanagedType.R4)] public float value;

        public static OptionalFloat None => new OptionalFloat { has_value = false, value = 0f };
        public static OptionalFloat Some(float val) => new OptionalFloat { has_value = true, value = val };
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OptionalBool
    {
        [MarshalAs(UnmanagedType.I1)] public bool has_value;
        [MarshalAs(UnmanagedType.I1)] public bool value;

        public static OptionalBool None => new OptionalBool { has_value = false, value = false };
        public static OptionalBool Some(bool val) => new OptionalBool { has_value = true, value = val };
    }
}