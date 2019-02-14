using _1fichier.SDK.Exception;
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

                await Task.Run(() =>
                {
                    Thread.Sleep(sleepTime);
                });
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
                throw new CommonException(result.message);
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
        public async Task<IEnumerable<UploadResult>> UploadFiles(Dictionary<string, Stream> files, int did = 0, int domain = 0)
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
                            sc.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
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
        /// <param name="floder">文件夹ID</param>
        /// <param name="listFiles">是否列出当前目录下的文件</param>
        /// <returns>文件夹信息。</returns>
        /// <exception cref="InvalidApiKeyException">非法的API Key。</exception>
        /// <exception cref="CommonException">服务器返回的错误。</exception>
        public async Task<FloderInfo> ListFloder(int floder, bool listFiles = false)
        {
            await WaitToOperation();
            using (var http = GetHttpClient(true))
            {
                var request = new
                {
                    folder_id = floder,
                    files = listFiles ? 1 : 0
                };
                var response = await http.PostAsync("https://api.1fichier.com/v1/folder/ls.cgi", GetJsonContent(request));
                string json = await response.Content.ReadAsStringAsync();
                CheckResponse(json);
                return JsonConvert.DeserializeObject<FloderInfo>(json);
            }
        }

        /// <summary>
        /// 创建文件夹。
        /// </summary>
        /// <param name="parent">父文件夹ID。根文件夹的ID为0</param>
        /// <param name="name">新文件夹名称</param>
        /// <param name="sharingUser">邮箱，新文件夹将被共享给该用户。</param>
        /// <returns>新文件夹ID</returns>
        /// <exception cref="InvalidApiKeyException">非法的API Key。</exception>
        /// <exception cref="CommonException">服务器返回的错误。</exception>
        public async Task<int> MakeFloder(string name, int parent = 0, string sharingUser = null)
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
        /// <param name="floder">目标文件夹ID</param>
        /// <param name="recursively">递归删除子文件夹和子文件，将耗费更多时间。</param>
        /// <exception cref="InvalidApiKeyException">非法的API Key。</exception>
        /// <exception cref="CommonException">服务器返回的错误。</exception>
        public async Task RemoveFloder(int floder, bool recursively = false)
        {
            if (recursively)
            {
                var info = await ListFloder(floder, true);
                if (info.sub_folders != null)
                {
                    foreach (var i in info.sub_folders)
                    {
                        await RemoveFloder(i.id, true);
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
                    }
                }
            }

            await WaitToOperation();
            using (var http = GetHttpClient(true))
            {
                var request = new
                {
                    folder_id = floder
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
    }
}
