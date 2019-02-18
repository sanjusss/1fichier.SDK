using _1fichier.SDK.Exception;
using _1fichier.SDK.Request;
using _1fichier.SDK.Result;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace _1fichier.SDK
{
    /// <summary>
    /// 1fichier客户端操作类。
    /// 本类中所有公有方法均有可能抛出HTTP网络连接相关异常，不会注释声明这些异常。
    /// </summary>
    public class Client
    {
        #region 内部变量
        /// <summary>
        /// API Key。
        /// </summary>
        private readonly string _apiKey = string.Empty;
        /// <summary>
        /// 默认代理
        /// </summary>
        private readonly WebProxy _proxy = null;
        /// <summary>
        /// 每秒最大操作数。
        /// </summary>
        private const int _maxOperation = 3;
        /// <summary>
        /// 操作统计间隔。
        /// 默认为1秒。
        /// </summary>
        private static readonly TimeSpan _operationInterval = new TimeSpan(0, 0, 1);
        /// <summary>
        /// 过去操作的时间队列。
        /// </summary>
        private static readonly List<DateTime> _operationTimes = new List<DateTime>();
        /// <summary>
        /// 操作队列的读写锁。
        /// </summary>
        private static readonly ReaderWriterLockSlim _operationLocker = new ReaderWriterLockSlim();
        /// <summary>
        /// 单次最大文件上传数量。
        /// </summary>
        private const int _maxUploadFilesCount = 100;
        /// <summary>
        /// 上一次读取全部文件的时间
        /// </summary>
        private static DateTime _lastTimeReadAll = DateTime.MinValue;
        /// <summary>
        /// 读取全部文件的最小间隔
        /// </summary>
        private static readonly TimeSpan _intervalReadAll = new TimeSpan(0, 10, 0);
        /// <summary>
        /// 上一次读取全部文件时间的操作锁
        /// </summary>
        private static readonly object _lastTimeReadAllLocker = new object();
        #endregion

        #region 公共属性
        /// <summary>
        /// 获取API Key的值。
        /// </summary>
        public string ApiKey => _apiKey;
        /// <summary>
        /// 获取默认代理。
        /// </summary>
        public WebProxy Proxy => _proxy;
        /// <summary>
        /// API Key是否有效。
        /// 仅判断是否为非空。
        /// </summary>
        public bool IsApiKeyVaild => string.IsNullOrEmpty(ApiKey) == false;
        #endregion

        #region 构造函数
        /// <summary>
        /// 初始化客户端类。
        /// </summary>
        /// <param name="apiKey">API Key</param>
        /// <param name="proxy">默认代理</param>
        public Client(string apiKey = "", WebProxy proxy = null)
        {
            _apiKey = apiKey;
            _proxy = proxy;
        }
        #endregion

        #region 内部函数
        /// <summary>
        /// 确保每秒操作数量不超过指定次数。
        /// </summary>
        protected static async Task WaitToOperation()
        {
            do
            {
                TimeSpan sleepTime;
                _operationLocker.EnterUpgradeableReadLock();
                try
                {
                    if (CanOperation())
                    {
                        _operationLocker.EnterWriteLock();
                        try
                        {
                            if (CanOperation())
                            {
                                _operationTimes.Add(DateTime.Now);
                                if (_operationTimes.Count > _maxOperation)
                                {
                                    _operationTimes.RemoveAt(0);
                                }

                                break;
                            }
                        }
                        finally
                        {
                            _operationLocker.ExitWriteLock();
                        }
                    }

                    sleepTime = _operationTimes.Count > 0 ? _operationInterval - (DateTime.Now - _operationTimes[0]) : _operationInterval;
                }
                finally
                {
                    _operationLocker.ExitUpgradeableReadLock();
                }

                await Task.Run(() => Thread.Sleep(sleepTime));
            } while (true);
        }

        /// <summary>
        /// 是否可以操作。
        /// </summary>
        /// <returns>是否可以操作。</returns>
        private static bool CanOperation()
        {
            return _operationTimes.Count < _maxOperation || DateTime.Now - _operationTimes[0] > _operationInterval;
        }

        /// <summary>
        /// 获取Http客户端。
        /// </summary>
        /// <param name="forceApiKey">是否强制使用API Key。</param>
        /// <returns>Http客户端</returns>
        /// <exception cref="InvalidApiKeyException">非法的API Key。</exception>
        protected HttpClient GetHttpClient(bool forceApiKey)
        {
            HttpClient http;
            if (Proxy == null)
            {
                http = new HttpClient();
            }
            else
            {
                HttpClientHandler handler = new HttpClientHandler()
                {
                    Proxy = Proxy
                };
                http = new HttpClient(handler);
            }

            if (IsApiKeyVaild)
            {
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
            }
            else if (forceApiKey)
            {
                throw new InvalidApiKeyException();
            }

            return http;
        }

        /// <summary>
        /// 将时间转换为字符串
        /// </summary>
        /// <param name="dt">时间</param>
        /// <returns>字符串，时区为+1</returns>
        protected static string ConvertDateTimeToString(DateTime dt)
        {
            return dt.ToUniversalTime().AddHours(1).ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// 获取Json格式的HttpContent。
        /// </summary>
        /// <param name="o">需要转变为Json的对象实例。</param>
        /// <returns>Json格式的HttpContent</returns>
        protected static HttpContent GetJsonContent(object o)
        {
            StringContent content = new StringContent(JsonConvert.SerializeObject(o));
            content.Headers.Remove("Content-Type");
            content.Headers.TryAddWithoutValidation("Content-Type", "application/json");
            return content;
        }

        /// <summary>
        /// 获取Json格式的HttpContent。
        /// </summary>
        /// <param name="json">json字符串。</param>
        /// <returns>Json格式的HttpContent</returns>
        protected static HttpContent GetJsonContent(string json)
        {
            StringContent content = new StringContent(json);
            content.Headers.Remove("Content-Type");
            content.Headers.TryAddWithoutValidation("Content-Type", "application/json");
            return content;
        }

        /// <summary>
        /// 检测返回结果是否存在异常。
        /// </summary>
        /// <param name="json">返回结果中的json字符串</param>
        /// <exception cref="CommonException">服务器返回的错误。</exception>
        protected static void CheckResponse(string json)
        {
            dynamic result = JsonConvert.DeserializeObject<dynamic>(json);
            if (result is JObject &&
                ((JObject)result).ContainsKey("status") &&
                result.status == "KO")
            {
                throw new CommonException(result.message as string);
            }
        }

        /// <summary>
        /// 获取上传节点信息。
        /// </summary>
        /// <returns>节点信息。</returns>
        /// <exception cref="NoIdException">获取操作ID时发生异常。</exception>
        protected async Task<Node> GetUploadNode()
        {
            try
            {
                await WaitToOperation();
                using (var http = GetHttpClient(false))
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.1fichier.com/v1/upload/get_upload_server.cgi");
                    request.Content = new StringContent("");
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    var response = await http.SendAsync(request);
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<Node>(json);
                }
            }
            catch (System.Exception e)
            {
                throw new NoIdException(e);
            }
        }
        #endregion

        #region 公共函数
        /// <summary>
        /// 上传多个文件。
        /// 每次最多上传100个，超过100个将自动拆分成多次上传。
        /// 如果API Key的值无效，did值也将无效。
        /// </summary>
        /// <param name="files">文件字典，Key是文件名，Value是文件流或字节流。文件名中的路径将被忽略。函数正常结束时，所有流将被关闭。</param>
        /// <param name="did">文件夹ID，0表示根文件夹。如果ID无效，将上传到根文件夹。</param>
        /// <param name="domain">上传目标域名，0表示1fichier.com。</param>
        /// <returns>上传结果的集合。</returns>
        /// <exception cref="NoIdException">获取操作ID时发生异常。</exception>
        /// <exception cref="UploadFailedException">上传失败。</exception>
        public async Task<IEnumerable<UploadResult>> UploadFiles(IReadOnlyDictionary<string, Stream> files, int did = 0, int domain = 0)
        {
            List<Dictionary<string, Stream>> nextFiles = new List<Dictionary<string, Stream>>();
            List<UploadResult> uploadResults = new List<UploadResult>();
            using (var http = GetHttpClient(false))
            {
                http.DefaultRequestHeaders.ConnectionClose = true;//尽管API文档里没说，此处的Connection值不能为Keep-Alive。
                string boundary = "------" + Guid.NewGuid().ToString("N");
                using (var content = new MultipartFormDataContent(boundary))
                {
                    //C#中boundary默认带引号，1fichier服务端不支持这种写法。
                    //所以我们要删除boundary中的引号。
                    content.Headers.Remove("Content-Type");
                    content.Headers.TryAddWithoutValidation("Content-Type", $"multipart/form-data; boundary={ boundary }");
                    int index = 0;
                    foreach (var i in files)
                    {
                        if (index < _maxUploadFilesCount)
                        {
                            StreamContent sc = new StreamContent(i.Value);
                            sc.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                            {
                                Name = "file[]",
                                FileName = $"\"{ i.Key }\"",//C# StreamContent中filename默认不带引号，1fichier服务端不支持这种写法。
                            };
                            sc.Headers.ContentType = new MediaTypeHeaderValue(MimeMapping.MimeUtility.GetMimeMapping(i.Key));
                            content.Add(sc);
                            ++index;
                        }
                        else
                        {
                            if (nextFiles.Count == 0 || nextFiles[0].Count >= _maxUploadFilesCount)
                            {
                                nextFiles.Insert(0, new Dictionary<string, Stream>());
                            }

                            nextFiles[0].Add(i.Key, i.Value);
                        }
                    }

                    //C# StringContent中name默认不带引号（为什么你在StreamContent中就带了？），1fichier服务端不支持这种写法。
                    //必须删除Content-Type头。
                    StringContent scdid = new StringContent(did.ToString());
                    scdid.Headers.Remove("Content-Type");
                    content.Add(scdid, "\"did\"");
                    StringContent scdomain = new StringContent(domain.ToString());
                    scdomain.Headers.Remove("Content-Type");
                    content.Add(scdomain, "\"domain\"");

                    var node = await GetUploadNode();
                    await WaitToOperation();
                    using (var response = await http.PostAsync($"https://{ node.url }/upload.cgi?id={ node.id }", content))
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        uploadResults.AddRange(UploadResult.Parse(json));
                    }
                }
            }

            if (nextFiles.Count != 0)
            {
                foreach (var i in nextFiles)
                {
                    uploadResults.AddRange(await UploadFiles(i, did, domain));
                }
            }

            return uploadResults;
        }

        /// <summary>
        /// 列出文件夹。
        /// </summary>
        /// <param name="folder">文件夹ID</param>
        /// <param name="listFiles">是否列出当前目录下的文件</param>
        /// <param name="sharingUser">如果该文件夹是其他用户共享的，共享来源用户的邮箱。</param>
        /// <returns>文件夹信息。</returns>
        /// <exception cref="InvalidApiKeyException">非法的API Key。</exception>
        /// <exception cref="CommonException">服务器返回的错误。</exception>
        public async Task<FolderInfo> ListFolder(int folder, bool listFiles = false, string sharingUser = null)
        {
            await WaitToOperation();
            using (var http = GetHttpClient(true))
            {
                var request = new
                {
                    folder_id = folder,
                    files = listFiles ? 1 : 0,
                    sharing_user = sharingUser
                };
                var response = await http.PostAsync("https://api.1fichier.com/v1/folder/ls.cgi", GetJsonContent(request));
                string json = await response.Content.ReadAsStringAsync();
                CheckResponse(json);
                return JsonConvert.DeserializeObject<FolderInfo>(json);
            }
        }

        /// <summary>
        /// 创建文件夹。
        /// </summary>
        /// <param name="parent">父文件夹ID。根文件夹的ID为0</param>
        /// <param name="name">新文件夹名称</param>
        /// <param name="sharingUser">如果该文件夹是其他用户共享的，共享来源用户的邮箱。一般只在文件夹id为0时使用。</param>
        /// <returns>新文件夹ID</returns>
        /// <exception cref="InvalidApiKeyException">非法的API Key。</exception>
        /// <exception cref="CommonException">服务器返回的错误。</exception>
        public async Task<int> MakeFolder(string name, int parent = 0, string sharingUser = null)
        {
            await WaitToOperation();
            using (var http = GetHttpClient(true))
            {
                var request = new
                {
                    name,
                    folder_id = parent,
                    sharing_user = sharingUser
                };
                var response = await http.PostAsync("https://api.1fichier.com/v1/folder/mkdir.cgi", GetJsonContent(request));
                string json = await response.Content.ReadAsStringAsync();
                CheckResponse(json);
                dynamic result = JsonConvert.DeserializeObject<dynamic>(json);
                return result.folder_id;
            }
        }

        /// <summary>
        /// 删除指定文件夹。
        /// </summary>
        /// <param name="folder">目标文件夹ID</param>
        /// <param name="recursively">递归删除子文件夹和子文件，将耗费更多时间。</param>
        /// <param name="waitForFileCached">等待，直到确认子文件删除，将耗费更多时间。</param>
        /// <exception cref="InvalidApiKeyException">非法的API Key。</exception>
        /// <exception cref="CommonException">服务器返回的错误。</exception>
        public async Task RemoveFolder(int folder, bool recursively = false, bool waitForFileCached = false)
        {
            if (recursively)
            {
                var info = await ListFolder(folder, true);
                if (info.subFolders != null)
                {
                    foreach (var i in info.subFolders)
                    {
                        await RemoveFolder(i.id, true, waitForFileCached);
                    }
                }

                if (info.items != null)
                {
                    List<string> urls = new List<string>();
                    foreach (var i in info.items)
                    {
                        urls.Add(i.url);
                    }

                    if (urls.Count > 0)
                    {
                        await RemoveFiles(urls);
                        while (waitForFileCached)
                        {
                            info = await ListFolder(folder, true);
                            if (info.items == null || info.files == 0)
                            {
                                break;
                            }

                            await Task.Run(() => Thread.Sleep(1000));

                            urls.Clear();
                            foreach (var i in info.items)
                            {
                                urls.Add(i.url);
                            }
                            
                            await RemoveFiles(urls);
                        }
                    }
                }
            }

            await WaitToOperation();
            using (var http = GetHttpClient(true))
            {
                var request = new
                {
                    folder_id = folder
                };
                var response = await http.PostAsync("https://api.1fichier.com/v1/folder/rm.cgi", GetJsonContent(request));
                string json = await response.Content.ReadAsStringAsync();
                CheckResponse(json);
            }
        }

        /// <summary>
        /// 删除指定文件。
        /// </summary>
        /// <param name="urls">文件url的集合。</param>
        /// <returns>删除的文件个数</returns>
        /// <exception cref="InvalidApiKeyException">非法的API Key。</exception>
        /// <exception cref="CommonException">服务器返回的错误。</exception>
        public async Task<int> RemoveFiles(IEnumerable<string> urls)
        {
            List<dynamic> singleRequests = new List<dynamic>();
            foreach (var i in urls)
            {
                singleRequests.Add(new
                {
                    url = i
                });
            }

            if (singleRequests.Count == 0)
            {
                return 0;
            }

            var request = new
            {
                files = singleRequests
            };

            await WaitToOperation();
            using (var http = GetHttpClient(true))
            {
                var response = await http.PostAsync("https://api.1fichier.com/v1/file/rm.cgi", GetJsonContent(request));
                string json = await response.Content.ReadAsStringAsync();
                CheckResponse(json);
                dynamic result = JsonConvert.DeserializeObject<dynamic>(json);
                return result.removed;
            }
        }

        /// <summary>
        /// 获取临时下载链接。该链接有效期5分钟。
        /// </summary>
        /// <param name="url">原始下载链接</param>
        /// <param name="pass">下载密码</param>
        /// <param name="inline">是否内联，即在浏览器里显示内容。</param>
        /// <param name="sharingUser">如果该文件夹是其他用户共享的，共享来源用户的邮箱。一般只在文件夹id为0时使用。</param>
        /// <param name="cdn">是否使用cdn</param>
        /// <param name="restrictIp">限制IP，仅当cdn为true时有效。0 (default): No restriction, 1: Prohibits IP changes, 2: Prohibits any sub-requests.0，不限制；1，限制IP变化；2，限制子请求。</param>
        /// <param name="noSsl">不使用SSL</param>
        /// <param name="folder">文件夹ID。仅在指定filName时有效。</param>
        /// <param name="fileName">文件名。指定此项时，将忽略url的值。</param>
        /// <returns>下载链接</returns>
        /// <exception cref="InvalidApiKeyException">非法的API Key。</exception>
        /// <exception cref="CommonException">服务器返回的错误。</exception>
        public async Task<string> GetTempDownloadLink(string url,
            string pass = null,
            bool inline = true,
            bool cdn = false,
            int restrictIp = 0,
            bool noSsl = false,
            string sharingUser = null,
            int folder = 0,
            string fileName = null)
        {
            await WaitToOperation();
            using (var http = GetHttpClient(true))
            {
                HttpContent content;
                if (string.IsNullOrEmpty(fileName))
                {
                    var request = new
                    {
                        url = url,
                        inline = inline ? 1 : 0,
                        cdn = cdn ? 1 : 0,
                        restrict_ip = restrictIp,
                        pass = pass,
                        no_ssl = noSsl ? 1 : 0,
                        sharing_user = sharingUser
                    };
                    content = GetJsonContent(request);
                }
                else
                {
                    var request = new
                    {
                        inline = inline ? 1 : 0,
                        cdn = cdn ? 1 : 0,
                        restrict_ip = restrictIp,
                        pass = pass,
                        no_ssl = noSsl ? 1 : 0,
                        sharing_user = sharingUser,
                        folder_id = folder,
                        filename = fileName
                    };
                    content = GetJsonContent(request);
                }

                var response = await http.PostAsync("https://api.1fichier.com/v1/download/get_token.cgi", content);
                string json = await response.Content.ReadAsStringAsync();
                CheckResponse(json);
                dynamic result = JsonConvert.DeserializeObject<dynamic>(json);
                return result.url;
            }
        }

        /// <summary>
        /// 获取文件夹ID。
        /// </summary>
        /// <param name="path">文件夹路径，例如 /doc/test/ 。</param>
        /// <returns>文件夹ID。</returns>
        /// <exception cref="InvalidApiKeyException">非法的API Key。</exception>
        /// <exception cref="CommonException">服务器返回的错误。</exception>
        /// <exception cref="FolderNotExistException">文件夹不存在。</exception>
        public async Task<int> GetFolderId(string path)
        {
            char[] separators = { '/', '\\' };
            string[] names = path.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            int parent = 0;
            foreach (var i in names)
            {
                var info = await ListFolder(parent);
                if (info.subFolders == null)
                {
                    throw new FolderNotExistException(path);
                }

                bool found = false;
                foreach (var j in info.subFolders)
                {
                    if (j.name == i)
                    {
                        parent = j.id;
                        found = true;
                        break;
                    }
                }

                if (found == false)
                {
                    throw new FolderNotExistException(path);
                }
            }

            return parent;
        }

        /// <summary>
        /// 创建路径，包括所有中间文件夹。
        /// </summary>
        /// <param name="path">文件夹路径，例如 /doc/test/ 。</param>
        /// <returns>文件夹ID。</returns>
        /// <exception cref="InvalidApiKeyException">非法的API Key。</exception>
        /// <exception cref="CommonException">服务器返回的错误。</exception>
        public async Task<int> MakePath(string path)
        {
            char[] separators = { '/', '\\' };
            string[] names = path.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            int parent = 0;
            foreach (var i in names)
            {
                var info = await ListFolder(parent);
                bool found = false;
                if (info.subFolders != null)
                {
                    foreach (var j in info.subFolders)
                    {
                        if (j.name == i)
                        {
                            parent = j.id;
                            found = true;
                            break;
                        }
                    }
                }

                if (found == false)
                {
                    parent = await MakeFolder(i, parent);
                }
            }

            return parent;
        }

        /// <summary>
        /// 列出指定文件夹内的所有文件。
        /// 建议使用ListFolder代替。
        /// </summary>
        /// <param name="folder">目标文件夹ID。-1表示所有文件。显示所有文件的频率必须小于10分钟1次。</param>
        /// <param name="sharingUser">如果该文件夹是其他用户共享的，共享来源用户的邮箱。一般只在文件夹id为0时使用。</param>
        /// <param name="start">文件的最小创建时间。</param>
        /// <param name="end">文件的最大创建时间。</param>
        /// <returns>文件信息集合</returns>
        /// <exception cref="InvalidApiKeyException">非法的API Key。</exception>
        /// <exception cref="CommonException">服务器返回的错误。</exception>
        /// <exception cref="AbuseException">显示所有文件的频率超过了10分钟1次。。</exception>
        public async Task<IEnumerable<FileSimpleInfo>> ListFiles(int folder, string sharingUser = null, DateTime? start = null, DateTime? end = null)
        {
            if (folder == -1)
            {
                lock (_lastTimeReadAllLocker)
                {
                    if (DateTime.Now - _lastTimeReadAll <= _intervalReadAll)
                    {
                        throw new AbuseException($"显示所有文件的频率必须小于10分钟1次。上次显示时间为{ _lastTimeReadAll }。");
                    }

                    _lastTimeReadAll = DateTime.Now;
                }
            }

            await WaitToOperation();
            using (var http = GetHttpClient(true))
            {
                var request = new
                {
                    folder_id = folder,
                    sharing_user = sharingUser,
                    sent_after = start.HasValue ? ConvertDateTimeToString(start.Value) : null,
                    sent_before = end.HasValue ? ConvertDateTimeToString(end.Value) : null
                };
                var response = await http.PostAsync("https://api.1fichier.com/v1/file/ls.cgi", GetJsonContent(request));
                string json = await response.Content.ReadAsStringAsync();
                CheckResponse(json);
                var result = JsonConvert.DeserializeObject<FileListResult>(json);
                return result.items;
            }
        }

        /// <summary>
        /// 获取文件简要信息。
        /// </summary>
        /// <param name="path">文件夹，例如 /doc/test.txt 。</param>
        /// <returns>文件简要信息。</returns>
        /// <exception cref="InvalidApiKeyException">非法的API Key。</exception>
        /// <exception cref="CommonException">服务器返回的错误。</exception>
        /// <exception cref="FileNotExistException">文件不存在。</exception>
        public async Task<FileSimpleInfo> GetFileSimpleInfo(string path)
        {
            char[] separators = { '/', '\\' };
            string[] names = path.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            int parent = 0;
            for (int i = 0; i < names.Length - 1; ++i)
            {
                var info = await ListFolder(parent);
                if (info.subFolders == null)
                {
                    throw new FileNotExistException(path);
                }

                bool found = false;
                foreach (var j in info.subFolders)
                {
                    if (j.name == names[i])
                    {
                        parent = j.id;
                        found = true;
                        break;
                    }
                }

                if (found == false)
                {
                    throw new FileNotExistException(path);
                }
            }

            var files = await ListFiles(parent);
            string target = names[names.Length - 1];
            foreach (var i in files)
            {
                if (i.filename == target)
                {
                    return i;
                }
            }

            throw new FileNotExistException(path);
        }

        /// <summary>
        /// 获取文件信息。
        /// </summary>
        /// <param name="url">文件下载链接</param>
        /// <param name="pass">文件的访问密码</param>
        /// <param name="folder">文件夹ID。仅在指定filName时有效。</param>
        /// <param name="fileName">文件名。指定此项时，将忽略url的值。</param>
        /// <param name="sharingUser">如果该文件夹是其他用户共享的，共享来源用户的邮箱。一般只在文件夹id为0时使用。</param>
        /// <returns>文件信息。</returns>
        /// <exception cref="InvalidApiKeyException">非法的API Key。</exception>
        /// <exception cref="CommonException">服务器返回的错误。</exception>
        public async Task<FileFullInfo> GetFileFullInfo(string url,
            string pass = null,
            string sharingUser = null,
            int folderId = 0,
            string fileName = null)
        {
            await WaitToOperation();
            using (var http = GetHttpClient(true))
            {
                HttpContent content;
                if (string.IsNullOrEmpty(fileName))
                {
                    var request = new
                    {
                        url = url,
                        pass = pass,
                        sharing_user = sharingUser
                    };
                    content = GetJsonContent(request);
                }
                else
                {
                    var request = new
                    {
                        pass = pass,
                        sharing_user = sharingUser,
                        folder_id = folderId,
                        filename = fileName
                    };
                    content = GetJsonContent(request);
                }

                var response = await http.PostAsync("https://api.1fichier.com/v1/file/info.cgi", content);
                string json = await response.Content.ReadAsStringAsync();
                CheckResponse(json);
                return JsonConvert.DeserializeObject<FileFullInfo>(json);
            }
        }

        /// <summary>
        /// 修改文件属性。
        /// </summary>
        /// <param name="filesAttributes">文件修改请求，详见FilesAttributesRequest注释。</param>
        /// <returns>被修改的文件个数</returns>
        /// <exception cref="InvalidApiKeyException">非法的API Key。</exception>
        /// <exception cref="CommonException">服务器返回的错误。</exception>
        public async Task<int> ChangeFilesAttributes(FilesAttributesRequest filesAttributes)
        {
            await WaitToOperation();
            using (var http = GetHttpClient(true))
            {
                string request = JsonConvert.SerializeObject(filesAttributes);
                HttpContent content = GetJsonContent(request);
                var response = await http.PostAsync("https://api.1fichier.com/v1/file/chattr.cgi", content);
                string json = await response.Content.ReadAsStringAsync();
                CheckResponse(json);
                var result = JsonConvert.DeserializeObject<dynamic>(json);
                return result.updated;
            }
        }

        /// <summary>
        /// 移动文件。
        /// </summary>
        /// <param name="urls">文件下载链接的集合。</param>
        /// <param name="destinationFolderId">目标文件夹ID。</param>
        /// <param name="sharingUser">如果目标文件夹是其他用户共享的，共享来源用户的邮箱。一般只在文件夹id为0时使用。</param>
        /// <returns>移动文件的结果。详见MoveFilesResult注释。</returns>
        /// <exception cref="InvalidApiKeyException">非法的API Key。</exception>
        /// <exception cref="CommonException">服务器返回的错误。</exception>
        public async Task<MoveFilesResult> MoveFiles(IEnumerable<string> urls, int destinationFolderId, string sharingUser = null)
        {
            await WaitToOperation();
            using (var http = GetHttpClient(true))
            {
                var request = new
                {
                    urls = urls,
                    destination_folder_id = destinationFolderId,
                    destination_user = sharingUser
                };

                var response = await http.PostAsync("https://api.1fichier.com/v1/file/mv.cgi", GetJsonContent(request));
                string json = await response.Content.ReadAsStringAsync();
                CheckResponse(json);
                return JsonConvert.DeserializeObject<MoveFilesResult>(json);
            }
        }

        /// <summary>
        /// 批量重命名文件。
        /// </summary>
        /// <param name="files">文件集合，Key是下载链接，Value是新文件名。</param>
        /// <returns>重命名文件的个数。</returns>
        /// <exception cref="InvalidApiKeyException">非法的API Key。</exception>
        /// <exception cref="CommonException">服务器返回的错误。</exception>
        public async Task<int> RenameFiles(IReadOnlyDictionary<string, string> files)
        {
            if (files.Count == 0)
            {
                return 0;
            }

            await WaitToOperation();
            using (var http = GetHttpClient(true))
            {
                List<dynamic> urls = new List<dynamic>();
                foreach (var i in files)
                {
                    urls.Add(new
                    {
                        url = i.Key,
                        filename = i.Value
                    });
                }

                var request = new
                {
                    urls = urls,
                    pretty = 0
                };

                var response = await http.PostAsync("https://api.1fichier.com/v1/file/rename.cgi", GetJsonContent(request));
                string json = await response.Content.ReadAsStringAsync();
                CheckResponse(json);
                var result = JsonConvert.DeserializeObject<dynamic>(json);
                return result.renamed;
            }
        }
        #endregion
    }
}
