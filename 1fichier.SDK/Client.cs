using _1fichier.SDK.Exception;
using _1fichier.SDK.Result;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        /// <returns>Http客户端</returns>
        protected HttpClient GetHttpClient()
        {
            if (Proxy == null)
            {
                return new HttpClient();
            }
            else
            {
                HttpClientHandler handler = new HttpClientHandler()
                {
                    Proxy = Proxy
                };
                return new HttpClient(handler);
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
                using (var http = GetHttpClient())
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
        /// </summary>
        /// <param name="files">文件字典，Key是文件名，Value是文件流或字节流。</param>
        /// <param name="did">文件夹ID，0表示根文件夹。</param>
        /// <param name="domain">上传目标域名，0表示1fichier.com。</param>
        /// <exception cref="NoIdException">获取操作ID时发生异常。</exception>
        public async Task UploadFiles(Dictionary<string, Stream> files, int did = 0, int domain = 0)
        {
            List<Dictionary<string, Stream>> nextFiles = new List<Dictionary<string, Stream>>();
            using (var http = GetHttpClient())
            {
                if (IsApiKeyVaild)
                {
                    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
                }

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
                        string result = await response.Content.ReadAsStringAsync();
                        Console.WriteLine(result);
                    }
                }
            }

            if (nextFiles.Count != 0)
            {
                foreach (var i in nextFiles)
                {
                    await UploadFiles(i, did, domain);
                }
            }
        }
    }
}
