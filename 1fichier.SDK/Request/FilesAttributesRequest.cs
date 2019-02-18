using _1fichier.SDK.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace _1fichier.SDK.Request
{
    /// <summary>
    /// 修改文件属性的请求。当某个属性值为null时，表示不修改该属性。
    /// </summary>
    [JsonConverter(typeof(FilesAttributesRequestConverter))]
    public struct FilesAttributesRequest
    {
        /// <summary>
        /// 文件的下载链接。作为文件唯一标识符，必须填写。
        /// </summary>
        public IReadOnlyCollection<string> urls;
        /// <summary>
        /// 文件名，只在urls数量为1时有效。
        /// </summary>
        public string fileName;
        /// <summary>
        /// 文件描述。
        /// </summary>
        public string description;
        /// <summary>
        /// 密码。为空表示取消密码。
        /// </summary>
        public string pass;
        /// <summary>
        /// 强制不使用SSL。
        /// </summary>
        public bool? noSsl;
        /// <summary>
        /// 是否内联下载，即直接下载不显示下载页面。
        /// </summary>
        public bool? inline;
        /// <summary>
        /// 是否使用CDN。
        /// </summary>
        public bool? cdn;
        /// <summary>
        /// 访问控制设置。
        /// </summary>
        public AccessControlRequest? acl;
    }
}
