using _1fichier.SDK.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace _1fichier.SDK.Result
{
    public struct FloderFileInfo
    {
        /// <summary>
        /// 文件是否处于保护模式（限制IP、国家、用户或使用者）。
        /// </summary>
        [JsonConverter(typeof(BoolConverter))]
        public bool acl;
        [JsonConverter(typeof(BoolConverter))]
        public bool cdn;
        public long size;
        [JsonConverter(typeof(BoolConverter))]
        public bool pass;
        public string checksum;
        [JsonConverter(typeof(Json.DateTimeConverter))]
        public DateTime date;
        public string filename;
        [JsonProperty("content-type")]
        public string contentType;
        public string url;
    }
}
