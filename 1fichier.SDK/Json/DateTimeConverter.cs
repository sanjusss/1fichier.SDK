using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace _1fichier.SDK.Json
{
    public class DateTimeConverter : IsoDateTimeConverter
    {
        public override bool CanWrite => false;

        public DateTimeConverter()
        {
            DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
            DateTimeStyles = System.Globalization.DateTimeStyles.AssumeUniversal;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            object o = base.ReadJson(reader, objectType, existingValue, serializer);
            if (o is DateTime dt)
            {
                return dt.AddHours(-1);//默认返回时间居然是+1而不是UTC。
            }
            else
            {
                return o;
            }
        }
    }
}
