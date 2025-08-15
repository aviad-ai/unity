/*
 * MIT License

   Copyright (c) 2022 Gustavo Santos

   Permission is hereby granted, free of charge, to any person obtaining a copy
   of this software and associated documentation files (the "Software"), to deal
   in the Software without restriction, including without limitation the rights
   to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
   copies of the Software, and to permit persons to whom the Software is
   furnished to do so, subject to the following conditions:

   The above copyright notice and this permission notice shall be included in all
   copies or substantial portions of the Software.

   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
   AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
   LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
   OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
   SOFTWARE.
 */
using System;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.PlayerLoop;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityMainThreadDispatcher
{
    public static class Dispatcher
    {
        private static readonly ConcurrentQueue<Action> _actionQueue = new ConcurrentQueue<Action>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            var subscription = new PlayerLoopSystemSubscription<Update>(Update);
            Application.quitting += subscription.Dispose;
        }

        private static void Update()
        {
            while (_actionQueue.TryDequeue(out var action))
            {
               action?.Invoke();
            }
        }

#if UNITY_EDITOR
        private static bool _editorUpdateRegistered = false;

        // This runs the same Update() but in EditorApplication.update callback
        public static void EditorUpdate()
        {
            Update();
        }

        // Call this to start Editor update loop
        public static void StartEditorUpdate()
        {
            if (!_editorUpdateRegistered)
            {
                EditorApplication.update += EditorUpdate;
                _editorUpdateRegistered = true;
            }
        }

        // Call this to stop Editor update loop
        public static void StopEditorUpdate()
        {
            if (_editorUpdateRegistered)
            {
                EditorApplication.update -= EditorUpdate;
                _editorUpdateRegistered = false;
            }
        }
#endif


        public static void Enqueue(Action action)
        {
            _actionQueue.Enqueue(action);
        }
    }
}