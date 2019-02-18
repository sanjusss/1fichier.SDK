using _1fichier.SDK.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace _1fichier.SDK.Result
{
    public struct FileFullInfo
    {
        /// <summary>
        /// 文件的访问控制信息。
        /// </summary>
        public AccessControlInfo? acl;
        /// <summary>
        /// 是否开启CDN优化
        /// </summary>
        [JsonConverter(typeof(BoolConverter))]
        public bool cdn;
        /// <summary>
        /// 文件大小
        /// </summary>
        public long size;
        /// <summary>
        /// 是否设置了密码
        /// </summary>
        [JsonConverter(typeof(BoolConverter))]
        public bool pass;
        /// <summary>
        /// 校验码
        /// </summary>
        public string checksum;
        /// <summary>
        /// 创建日期
        /// </summary>
        [JsonConverter(typeof(Json.DateTimeConverter))]
        public DateTime date;
        /// <summary>
        /// 文件名
        /// </summary>
        public string filename;
        /// <summary>
        /// 文件类型
        /// </summary>
        [JsonProperty("content-type")]
        public string contentType;
        /// <summary>
        /// 下载链接
        /// </summary>
        public string url;
        /// <summary>
        /// 所在文件夹的ID
        /// </summary>
        [JsonProperty("folder_id")]
        public int folderId;
        /// <summary>
        /// 所在文件夹路径，例如 /a/b/c.txt 的路径为 a/b
        /// </summary>
        public string path;
        /// <summary>
        /// 文件描述
        /// </summary>
        public string description;
        /// <summary>
        /// 是否直接在浏览器里显示内容
        /// </summary>
        [JsonConverter(typeof(BoolConverter))]
        public bool inline;
        /// <summary>
        /// 是否禁用ssl
        /// </summary>
        [JsonProperty("no_ssl")]
        [JsonConverter(typeof(BoolConverter))]
        public bool noSsl;
    }
}
