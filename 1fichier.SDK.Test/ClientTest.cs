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

namespace _1fichier.SDK.Test
{
    [TestClass]
    public class ClientTest
    {
        private Client _client = null;

        [TestInitialize]
        public void MethodInit()
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
        }

        [TestCleanup]
        public void MethodClean()
        {
            _client = null;
        }

        [TestMethod]
        public async Task UploadFilesTest()
        {
            var root = await _client.ListFolder(0, true);
            if (root.sub_folders != null)
            {
                foreach (var i in root.sub_folders)
                {
                    if (i.name == "test")
                    {
                        await _client.RemoveFolder(i.id, true);
                    }
                }
            }

            int targetDir = await _client.MakeFolder("test");

            Dictionary<string, Stream> files2Upload = new Dictionary<string, Stream>();
            DirectoryInfo di = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            var files = di.GetFiles();
            foreach (var i in files)
            {
                files2Upload[i.Name] = i.OpenRead();
            }
            
            string fileName = "1fichier.SDK.Test.dll";
            var results = await _client.UploadFiles(files2Upload, targetDir);
            Assert.AreEqual(files2Upload.Count, (new List<UploadResult>(results)).Count);
            bool uploadSuccess = false;
            foreach (var i in results)
            {
                if (i.fileName == fileName)
                {
                    uploadSuccess = true;
                    break;
                }
            }

            if (uploadSuccess == false)
            {
                Assert.Fail("返回结果不包括当前程序的文件名。");
            }

            Thread.Sleep(20 * 1000);//一个文件夹被上传文件后数秒内不能被删除。
            await _client.RemoveFolder(targetDir, true);
        }

        [TestMethod]
        public async Task GetTempDownloadLinkTest()
        {
            string url = await _client.GetTempDownloadLink("https://1fichier.com/?ui2nl7stip2woqnl083w");
            using (HttpClient http = new HttpClient())
            {
                var content = await http.GetStringAsync(url);
                Assert.AreEqual("test content", content);
            }
        }
    }
}
