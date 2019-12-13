using System;
using System.Collections.Generic;
using System.Text;

namespace _1fichier.SDK.Exception
{
    /// <summary>
    /// 服务器通用异常。
    /// </summary>
    public class CommonException : System.Exception
    {
        public CommonException(string message) :
            base(message)
        {

        }
    }
}
