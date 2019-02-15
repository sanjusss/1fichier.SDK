using _1fichier.SDK.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace _1fichier.SDK.Result
{
    /// <summary>
    /// 子文件夹信息
    /// </summary>
    public struct SubFolderInfo
    {
        /// <summary>
        /// 文件夹ID
        /// </summary>
        public int id;
        /// <summary>
        /// 文件夹名称
        /// </summary>
        public string name;
        /// <summary>
        /// 是否设置了密码
        /// </summary>
        [JsonConverter(typeof(BoolConverter))]
        public bool pass;
        /// <summary>
        /// 创建日期
        /// </summary>
        [JsonConverter(typeof(Json.DateTimeConverter))]
        [JsonProperty("create_date")]
        public DateTime createDate;
    }
}
