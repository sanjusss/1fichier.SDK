using System;
using System.Collections.Generic;
using System.Text;

namespace _1fichier.SDK.Exception
{
    public class FileNotExistException : System.Exception
    {
        public FileNotExistException(string path) :
            base($"不存在文件{ path }。")
        {

        }
    }
}
