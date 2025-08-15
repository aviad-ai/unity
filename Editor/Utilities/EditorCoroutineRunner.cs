using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Aviad
{
    public static class EditorCoroutineRunner
    {
        private static readonly List<IEnumerator> coroutines = new();
        private static bool initialized = false;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            if (!initialized)
            {
                EditorApplication.update += Update;
                initialized = true;
            }
        }

        public static void Start(IEnumerator coroutine)
        {
            if (coroutine != null)
                coroutines.Add(coroutine);
        }

        private static void Update()
        {
            for (int i = 0; i < coroutines.Count; i++)
            {
                if (!coroutines[i].MoveNext())
                {
                    coroutines.RemoveAt(i);
                    i--;
                }
            }
        }
    }
}