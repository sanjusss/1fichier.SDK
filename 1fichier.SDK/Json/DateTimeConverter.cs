using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace _1fichier.SDK.Json
{
    public class DateTimeConverter : IsoDateTimeConverter
    {
        public DateTimeConverter()
        {
            DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
            DateTimeStyles = System.Globalization.DateTimeStyles.AssumeUniversal;
        }
    }
}
