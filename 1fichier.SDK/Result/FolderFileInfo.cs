using _1fichier.SDK.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace _1fichier.SDK.Result
{
    /// <summary>
    /// 文件夹下的文件信息
    /// </summary>
    public struct FolderFileInfo
    {
        /// <summary>
        /// 文件是否处于保护模式（限制IP、国家、用户或使用者）。
        /// </summary>
        [JsonConverter(typeof(BoolConverter))]
        public bool acl;
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
    }
}
