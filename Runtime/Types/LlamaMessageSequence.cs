using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Aviad
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NativeLlamaMessageSequence
    {
        public IntPtr roles;      // char** - pointer to array of string pointers
        public IntPtr contents;   // char** - pointer to array of string pointers
        [MarshalAs(UnmanagedType.U4)] public int message_count;
    }

    [Serializable]
    public class Message
    {
        [SerializeField] public string role;
        [SerializeField] public string content;

        public Message(string role, string content)
        {
            this.role = role;
            this.content = content;
        }

        public Message(Message other)
        {
            this.role = other.role;
            this.content = other.content;
        }
    }

    [Serializable]
    public class LlamaMessageSequence
    {
        [SerializeField] public List<Message> messages = new List<Message>();

        public LlamaMessageSequence()
        {
        }

        public LlamaMessageSequence(LlamaMessageSequence other)
        {
            messages = new List<Message>(other.messages.Count);
            foreach (var message in other.messages)
            {
                messages.Add(new Message(message));
            }
        }

        public LlamaMessageSequence(NativeLlamaMessageSequence nativeSequence)
        {
            messages = FromNative(nativeSequence);
        }

        public NativeMessageSequenceWrapper ToNative()
        {
            return new NativeMessageSequenceWrapper(messages);
        }

        private static List<Message> FromNative(NativeLlamaMessageSequence native)
        {
            var result = new List<Message>();
            int count = native.message_count;

            for (int i = 0; i < count; i++)
            {
                IntPtr rolePtr = Marshal.ReadIntPtr(native.roles, i * IntPtr.Size);
                IntPtr contentPtr = Marshal.ReadIntPtr(native.contents, i * IntPtr.Size);

                string role = Marshal.PtrToStringAnsi(rolePtr);
                string content = Marshal.PtrToStringAnsi(contentPtr);

                result.Add(new Message(role, content));
            }

            return result;
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }

        public static LlamaMessageSequence FromJson(string json)
        {
            return JsonUtility.FromJson<LlamaMessageSequence>(json);
        }
    }

    // Wrapper class to handle memory management for native message sequences
    public class NativeMessageSequenceWrapper : IDisposable
    {
        private NativeLlamaMessageSequence _nativeSequence;
        private IntPtr[] _rolePtrs;
        private IntPtr[] _contentPtrs;
        private bool _disposed = false;

        public NativeLlamaMessageSequence Native => _nativeSequence;

        public NativeMessageSequenceWrapper(List<Message> messages)
        {
            int count = messages.Count;

            _rolePtrs = new IntPtr[count];
            _contentPtrs = new IntPtr[count];

            // Allocate strings
            for (int i = 0; i < count; i++)
            {
                _rolePtrs[i] = Marshal.StringToHGlobalAnsi(messages[i].role);
                _contentPtrs[i] = Marshal.StringToHGlobalAnsi(messages[i].content);
            }

            // Allocate arrays of pointers
            IntPtr rolesArray = Marshal.AllocHGlobal(IntPtr.Size * count);
            IntPtr contentsArray = Marshal.AllocHGlobal(IntPtr.Size * count);

            // Write pointers to arrays
            for (int i = 0; i < count; i++)
            {
                Marshal.WriteIntPtr(rolesArray, i * IntPtr.Size, _rolePtrs[i]);
                Marshal.WriteIntPtr(contentsArray, i * IntPtr.Size, _contentPtrs[i]);
            }

            _nativeSequence = new NativeLlamaMessageSequence
            {
                roles = rolesArray,
                contents = contentsArray,
                message_count = count
            };
        }

        public void Dispose()
        {
            if (_disposed) return;

            // Free individual strings
            if (_rolePtrs != null)
            {
                foreach (var ptr in _rolePtrs)
                {
                    if (ptr != IntPtr.Zero)
                        Marshal.FreeHGlobal(ptr);
                }
            }

            if (_contentPtrs != null)
            {
                foreach (var ptr in _contentPtrs)
                {
                    if (ptr != IntPtr.Zero)
                        Marshal.FreeHGlobal(ptr);
                }
            }

            // Free arrays
            if (_nativeSequence.roles != IntPtr.Zero)
                Marshal.FreeHGlobal(_nativeSequence.roles);

            if (_nativeSequence.contents != IntPtr.Zero)
                Marshal.FreeHGlobal(_nativeSequence.contents);

            _disposed = true;
        }

        ~NativeMessageSequenceWrapper()
        {
            Dispose();
        }
    }

    public class LlamaMessageSequencePreallocator : IDisposable
    {
        public int MaxTurns { get; }
        public int MaxStringLength { get; }

        public IntPtr RolesArrayPtr { get; private set; }
        public IntPtr ContentsArrayPtr { get; private set; }

        private IntPtr[] roleBuffers;    // pointers to fixed buffers for each role string
        private IntPtr[] contentBuffers; // pointers to fixed buffers for each content string

        public LlamaMessageSequencePreallocator(int maxTurns, int maxStringLength)
        {
            MaxTurns = maxTurns;
            MaxStringLength = maxStringLength;

            roleBuffers = new IntPtr[maxTurns];
            contentBuffers = new IntPtr[maxTurns];

            byte[] zeroBytes = new byte[maxStringLength]; // zero buffer to clear memory

            // Allocate fixed-size buffers for each string
            for (int i = 0; i < maxTurns; i++)
            {
                roleBuffers[i] = Marshal.AllocHGlobal(maxStringLength);
                Marshal.Copy(zeroBytes, 0, roleBuffers[i], maxStringLength);

                contentBuffers[i] = Marshal.AllocHGlobal(maxStringLength);
                Marshal.Copy(zeroBytes, 0, contentBuffers[i], maxStringLength);
            }

            // Allocate unmanaged arrays of pointers
            RolesArrayPtr = Marshal.AllocHGlobal(IntPtr.Size * maxTurns);
            ContentsArrayPtr = Marshal.AllocHGlobal(IntPtr.Size * maxTurns);

            Marshal.Copy(roleBuffers, 0, RolesArrayPtr, maxTurns);
            Marshal.Copy(contentBuffers, 0, ContentsArrayPtr, maxTurns);
        }

        public NativeLlamaMessageSequence GetLlamaMessageSequence()
        {
            return new NativeLlamaMessageSequence
            {
                roles = RolesArrayPtr,
                contents = ContentsArrayPtr,
                message_count = MaxTurns
            };
        }

        public string[] ReadRoles()
        {
            var result = new string[MaxTurns];
            for (int i = 0; i < MaxTurns; i++)
            {
                result[i] = Marshal.PtrToStringAnsi(roleBuffers[i]) ?? string.Empty;
            }
            return result;
        }

        public string[] ReadContents()
        {
            var result = new string[MaxTurns];
            for (int i = 0; i < MaxTurns; i++)
            {
                result[i] = Marshal.PtrToStringAnsi(contentBuffers[i]) ?? string.Empty;
            }
            return result;
        }

        public void Dispose()
        {
            if (RolesArrayPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(RolesArrayPtr);
                RolesArrayPtr = IntPtr.Zero;
            }

            if (ContentsArrayPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(ContentsArrayPtr);
                ContentsArrayPtr = IntPtr.Zero;
            }

            if (roleBuffers != null)
            {
                foreach (var ptr in roleBuffers)
                    Marshal.FreeHGlobal(ptr);
                roleBuffers = null;
            }

            if (contentBuffers != null)
            {
                foreach (var ptr in contentBuffers)
                    Marshal.FreeHGlobal(ptr);
                contentBuffers = null;
            }
        }
    }
}