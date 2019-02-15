using _1fichier.SDK.Request;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace _1fichier.SDK.Json
{
    /// <summary>
    /// 将FilesAttributesRequest转为Json字符串。
    /// </summary>
    public class FilesAttributesRequestConverter : JsonConverter
    {
        public override bool CanRead => false;

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(FilesAttributesRequest);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            FilesAttributesRequest attr = (FilesAttributesRequest)value;
            JObject o = new JObject();
            o["urls"] = JArray.FromObject(attr.urls);

            if (attr.fileName != null)
            {
                o["fileName"] = attr.fileName;
            }

            if (attr.description != null)
            {
                o["description"] = attr.description;
            }

            if (attr.pass != null)
            {
                o["pass"] = attr.pass;
            }

            if (attr.noSsl.HasValue)
            {
                o["no_ssl"] = attr.noSsl.Value ? 1 : 0;
            }

            if (attr.inline.HasValue)
            {
                o["inline"] = attr.inline.Value ? 1 : 0;
            }

            if (attr.cdn.HasValue)
            {
                o["cdn"] = attr.cdn.Value ? 1 : 0;
            }

            if (attr.acl.HasValue)
            {
                JObject acl = new JObject();
                var aclv = attr.acl.Value;

                if (aclv.ip != null)
                {
                    acl["ip"] = JArray.FromObject(aclv.ip);
                }

                if (aclv.country != null)
                {
                    acl["country"] = JArray.FromObject(aclv.country);
                }

                if (aclv.email != null)
                {
                    acl["email"] = JArray.FromObject(aclv.email);
                }

                if (aclv.premium.HasValue)
                {
                    acl["premium"] = aclv.premium.Value ? 1 : 0;
                }

                if (acl.HasValues)
                {
                    o["acl"] = acl;
                }
            }

            o.WriteTo(writer);
        }
    }
}
