using Microsoft.VisualStudio.TestTools.UnitTesting;
using _1fichier.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Collections;

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

            _client.UploadFiles(files2Upload).Wait();
        }
    }
}
