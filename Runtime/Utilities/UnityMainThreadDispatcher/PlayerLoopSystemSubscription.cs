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
using UnityEngine.LowLevel;

namespace MainThreadDispatcher
{
    internal class PlayerLoopSystemSubscription<T> : IDisposable
    {
        private readonly Action _callback;

        public PlayerLoopSystemSubscription(Action callback)
        {
            _callback = callback;
            Subscribe();
        }

        private void Invoke()
        {
            _callback.Invoke();
        }

        private void Subscribe()
        {
            var loop = PlayerLoop.GetCurrentPlayerLoop();
            ref var system = ref loop.Find<T>();
            system.updateDelegate += Invoke;
            PlayerLoop.SetPlayerLoop(loop);
        }

        private void Unsubscribe()
        {
            var loop = PlayerLoop.GetCurrentPlayerLoop();
            ref var system = ref loop.Find<T>();
            system.updateDelegate -= Invoke;
            PlayerLoop.SetPlayerLoop(loop);
        }

        public void Dispose()
        {
            Unsubscribe();
        }
    }
}