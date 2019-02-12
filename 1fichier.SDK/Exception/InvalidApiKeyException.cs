using System;
using System.Collections.Generic;
using System.Text;

namespace _1fichier.SDK.Exception
{
    public class InvalidApiKeyException : System.Exception
    {
        public InvalidApiKeyException() :
            base("非法的API Key。")
        {

        }
    }
}
