using System;
using System.Collections.Generic;
using System.Text;

namespace _1fichier.SDK.Result
{
    /// <summary>
    /// 移动文件的结果。
    /// </summary>
    public struct MoveFilesResult
    {
        /// <summary>
        /// 被移动的文件。
        /// </summary>
        public int moved;
        /// <summary>
        /// 移动后文件的下载链接。
        /// </summary>
        public IEnumerable<string> urls;
    }
}
