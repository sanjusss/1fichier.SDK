using System;
using System.Collections.Generic;
using System.Text;

namespace _1fichier.SDK.Exception
{
    /// <summary>
    /// 上传文件失败。
    /// </summary>
    public class UploadFailedException : System.Exception
    {
        private readonly string _errorHtml;
        /// <summary>
        /// 服务端返回的html内容。
        /// </summary>
        public string ErrorHtml => _errorHtml;

        public UploadFailedException(string html) :
            base("上传文件失败。")
        {
            _errorHtml = html;
        }
    }
}
