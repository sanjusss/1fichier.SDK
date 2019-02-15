using System;
using System.Collections.Generic;
using System.Text;

namespace _1fichier.SDK.Exception
{
    /// <summary>
    /// 滥用API产生的异常。
    /// </summary>
    public class AbuseException : System.Exception
    {
        public AbuseException(string message) :
            base(message)
        {

        }
    }
}
