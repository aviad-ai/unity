using System;

namespace Aviad
{
    public interface IWebGLUtilities
    {
        void DownloadFile(string url, string targetPath, Action<bool> onComplete);
    }
}