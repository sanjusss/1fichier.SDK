using System;
using System.Collections.Generic;
using System.Text;

namespace _1fichier.SDK.Result
{
    /// <summary>
    /// 访问权限信息
    /// </summary>
    public struct AccessControlInfo
    {
        /// <summary>
        /// 允许访问的ip范围，用IP或CIDR表示。
        /// </summary>
        public IEnumerable<string> ip;
        /// <summary>
        /// 允许访问的国家代码。
        /// </summary>
        public IEnumerable<string> country;
        /// <summary>
        /// 允许访问的用户的邮箱。
        /// </summary>
        public IEnumerable<string> email;
        /// <summary>
        /// 是否只允许premium或access用户。
        /// </summary>
        public bool premium;
    }
}
