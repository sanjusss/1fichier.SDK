using _1fichier.SDK.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace _1fichier.SDK.Result
{
    public struct FloderInfo
    {
        public string status;
        public string name;
        [JsonConverter(typeof(Json.DateTimeConverter))]
        public DateTime create_date;
        [JsonConverter(typeof(BoolConverter))]
        public bool pass;
        public long size;
        public string shared;
        public int folder_id;
        public IEnumerable<SubFloderInfo> sub_folders;
        public int files;
        public IEnumerable<FloderFileInfo> items;
    }
}
