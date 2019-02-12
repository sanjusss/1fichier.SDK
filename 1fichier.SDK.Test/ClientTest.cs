using Microsoft.VisualStudio.TestTools.UnitTesting;
using _1fichier.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Reflection;

namespace _1fichier.SDK.Test
{
    [TestClass]
    public class ClientTest
    {
        private Client _client = null;

        [TestInitialize]
        public void MethodInit()
        {
            string apiKey = Environment.GetEnvironmentVariable("APIKEY");
            _client = new Client(apiKey);
        }

        [TestCleanup]
        public void MethodClean()
        {
            _client = null;
        }

        [TestMethod]
        public void UploadFilesTest()
        {
            Dictionary<string, Stream> files2Upload = new Dictionary<string, Stream>();
            DirectoryInfo di = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            var files = di.GetFiles();
            foreach (var i in files)
            {
                files2Upload[i.Name] = i.OpenRead();
            }
            
            string fileName = "1fichier.SDK.Test.dll";
            var results = _client.UploadFiles(files2Upload).Result;
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
        public void ListFloderTest()
        {
            _client.ListFloder(0, true).Wait();
        }
    }
}
