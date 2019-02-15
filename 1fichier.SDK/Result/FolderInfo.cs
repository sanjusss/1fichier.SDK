using _1fichier.SDK.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace _1fichier.SDK.Result
{
    /// <summary>
    /// 文件夹信息
    /// </summary>
    public struct FolderInfo
    {
        /// <summary>
        /// 文件夹状态
        /// </summary>
        public string status;
        /// <summary>
        /// 文件夹名称
        /// </summary>
        public string name;
        /// <summary>
        /// 创建日期
        /// </summary>
        [JsonConverter(typeof(Json.DateTimeConverter))]
        [JsonProperty("create_date")]
        public DateTime createDate;
        /// <summary>
        /// 是否设置了密码
        /// </summary>
        [JsonConverter(typeof(BoolConverter))]
        public bool pass;
        /// <summary>
        /// 文件夹大小，不包括子文件夹。
        /// </summary>
        public long? size;
        /// <summary>
        /// 用于共享的链接
        /// </summary>
        public string shared;
        /// <summary>
        /// 文件夹ID
        /// </summary>
        [JsonProperty("folder_id")]
        public int id;
        /// <summary>
        /// 子文件夹的集合
        /// </summary>
        [JsonProperty("sub_folders")]
        public IEnumerable<SubFolderInfo> subFolders;
        /// <summary>
        /// 当前目录下的文件数量
        /// </summary>
        public int files;
        /// <summary>
        /// 当前目录下的文件的集合
        /// </summary>
        public IEnumerable<FileSimpleInfo> items;
    }
}
