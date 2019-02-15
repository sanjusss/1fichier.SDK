using System;
using System.Collections.Generic;
using System.Text;

namespace _1fichier.SDK.Exception
{
    public class FolderNotExistException : System.Exception
    {
        public FolderNotExistException(string path) :
            base($"不存在文件夹{ path }。")
        {

        }
    }
}
