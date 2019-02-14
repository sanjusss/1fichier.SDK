using System;
using System.Collections.Generic;
using System.Text;

namespace _1fichier.SDK.Exception
{
    /// <summary>
    /// 创建文件夹失败。
    /// </summary>
    public class CommonException : System.Exception
    {
        public CommonException(string message) :
            base(message)
        {

        }
    }
}
