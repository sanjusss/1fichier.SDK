using _1fichier.SDK.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace _1fichier.SDK.Result
{
    public struct SubFloderInfo
    {
        public int id;
        public string name;
        [JsonConverter(typeof(BoolConverter))]
        public bool pass;
        [JsonConverter(typeof(Json.DateTimeConverter))]
        public DateTime create_date;
    }
}
