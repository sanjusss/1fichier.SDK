using System;
using System.Collections.Generic;
using System.Text;

namespace _1fichier.SDK.Exception
{
    /// <summary>
    /// 获取操作ID时发生异常。
    /// </summary>
    public class NoIdException : System.Exception
    {
        public NoIdException(System.Exception inner) :
            base("无法获取操作ID。", inner)
        {

        }
    }
}
