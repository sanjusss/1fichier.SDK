using System;
using System.Collections.Generic;
using System.Text;

namespace _1fichier.SDK.Result
{
    public struct FileListResult
    {
        /// <summary>
        /// 查询状态
        /// </summary>
        public string status;
        /// <summary>
        /// 文件数量
        /// </summary>
        public int count;
        /// <summary>
        /// 文件集合
        /// </summary>
        public IEnumerable<FileSimpleInfo> items;
    }
}
