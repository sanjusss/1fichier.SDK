using Microsoft.VisualStudio.TestTools.UnitTesting;
using _1fichier.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using Newtonsoft.Json;
using System.Threading.Tasks;
using _1fichier.SDK.Result;
using System.Threading;
using System.Net.Http;
using System.Text;

namespace _1fichier.SDK.Test
{
    [TestClass]
    public class ClientTest
    {
        private const string _testPath = "/test/";
        private Client _client = null;
        private int _testPathId = -1;

        [TestInitialize]
        public async Task InitTest()
        {
            string apiKey;
            string configFile = AppDomain.CurrentDomain.BaseDirectory + "/Properties/config.json";
            if (File.Exists(configFile))
            {
                using (StreamReader stream = new StreamReader(configFile))
                {
                    string json = stream.ReadToEnd();
                    dynamic config = JsonConvert.DeserializeObject<dynamic>(json);
                    apiKey = config.APIKEY;
                }
            }
            else
            {
                apiKey = Environment.GetEnvironmentVariable("APIKEY");
            }

            _client = new Client(apiKey);
            _testPathId = await _client.GetFolderId(_testPath);
            if (_testPathId > 0)
            {
                await _client.RemoveFolder(_testPathId, true, true);
            }

            _testPathId = await _client.MakePath(_testPath);
        }

        [TestCleanup]
        public async Task CleanTestAsync()
        {
            await _client.RemoveFolder(_testPathId, true, true);
            _testPathId = -1;
            _client = null;
        }

        [TestMethod]
        public async Task UploadFilesTest()
        {
            Dictionary<string, Stream> files2Upload = new Dictionary<string, Stream>();
            DirectoryInfo di = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            var files = di.GetFiles();
            foreach (var i in files)
            {
                files2Upload[i.Name] = i.OpenRead();
            }
            
            string fileName = "1fichier.SDK.Test.dll";
            var results = await _client.UploadFiles(files2Upload, _testPathId);
            Assert.AreEqual(files2Upload.Count, (new List<UploadResult>(results)).Count);
            foreach (var i in results)
            {
                if (i.fileName == fileName)
                {
                    return;
                }
            }
            
            Assert.Fail("返回结果不包括当前程序的文件名。");
        }

        [TestMethod]
        public async Task GetTempDownloadLinkTest()
        {
            string text = "test content";
            Dictionary<string, Stream> files2Upload = new Dictionary<string, Stream>();
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(text));
            files2Upload["test.txt"] = ms;
            var results = await _client.UploadFiles(files2Upload, _testPathId);
            List<UploadResult> files = new List<UploadResult>(results);
            Assert.AreEqual(1, files.Count);
            string url = await _client.GetTempDownloadLink(files[0].downloadLink);
            using (HttpClient http = new HttpClient())
            {
                var content = await http.GetStringAsync(url);
                Assert.AreEqual(text, content);
            }
        }

        [TestMethod]
        public async Task OperationPathTest()
        {
            int idMake = await _client.MakePath(_testPath + "/1/2/3/4/5");
            int idGet = await _client.GetFolderId(_testPath + "/1/2/3/4/5");
            Assert.AreEqual(idMake, idGet);
        }
    }
}
