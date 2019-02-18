using _1fichier.SDK.Exception;
using DotnetSpider.Extraction;
using System;
using System.Collections.Generic;
using System.Text;

namespace _1fichier.SDK.Result
{
    public struct UploadResult
    {
        /// <summary>
        /// 文件名
        /// </summary>
        public string fileName;
        /// <summary>
        /// 文件大小
        /// </summary>
        public string fileSize;
        /// <summary>
        /// 下载链接
        /// </summary>
        public string downloadLink;
        /// <summary>
        /// 删除链接
        /// </summary>
        public string removeLink;

        /// <summary>
        /// 解析上传的结果。
        /// </summary>
        /// <param name="html">服务器返回的html内容</param>
        /// <returns>上传结果的集合。</returns>
        /// <exception cref="UploadFailedException">不能解析返回的html。</exception>
        public static IReadOnlyList<UploadResult> Parse(string html)
        {
            Selectable selectable = new Selectable(html);
            var tables = selectable.SelectList(Selectors.XPath("//table[@class='premium']")).Nodes();
            foreach (var i in tables)
            {
                List<UploadResult> results = new List<UploadResult>();
                var nodes = i.SelectList(Selectors.XPath("./tr")).Nodes();
                bool first = true;
                foreach (var j in nodes)
                {
                    if (first)
                    {
                        first = false;
                        continue;
                    }

                    UploadResult result = new UploadResult();
                    result.fileName = j.Select(Selectors.XPath("./td[1]")).GetValue();
                    result.fileSize = j.Select(Selectors.XPath("./td[2]")).GetValue();
                    result.downloadLink = j.Select(Selectors.XPath("./td[3]/a")).GetValue();
                    result.removeLink = j.Select(Selectors.XPath("./td[4]")).GetValue();

                    results.Add(result);
                }

                return results;
            }

            throw new UploadFailedException(html);
        }
    }
}
