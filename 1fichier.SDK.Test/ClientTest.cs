﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
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
using _1fichier.SDK.Exception;

namespace _1fichier.SDK.Test
{
    [TestClass]
    public class ClientTest
    {
        private const string _testPath = "/test/";
        private Client _client = null;
        private int _testPathId = -1;
        private const string _testContent = "test content";
        private const string _testFileName = "test.txt";

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
            try
            {
                _testPathId = await _client.GetFolderId(_testPath);
                await _client.RemoveFolder(_testPathId, true, true);
            }
            catch
            {
                _testPathId = -1;
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

        private async Task<string> UploadATestFile()
        {
            Dictionary<string, Stream> files2Upload = new Dictionary<string, Stream>();
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(_testContent));
            files2Upload[_testFileName] = ms;
            var results = new List<UploadResult>(await _client.UploadFiles(files2Upload, _testPathId));
            return results[0].downloadLink;
        }

        [TestMethod]
        public async Task UploadFilesTest()
        {
            Dictionary<string, Stream> files2Upload = new Dictionary<string, Stream>();
            DirectoryInfo di = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            var files = di.GetFiles();
            List<string> fileNamesSource = new List<string>();
            foreach (var i in files)
            {
                files2Upload[i.Name] = i.OpenRead();
                fileNamesSource.Add(i.Name);
            }
            
            var results = await _client.UploadFiles(files2Upload, _testPathId);
            List<string> fileNames = new List<string>();
            foreach (var i in results)
            {
                fileNames.Add(i.fileName);
            }

            CollectionAssert.AreEquivalent(fileNamesSource, fileNames);
        }

        [TestMethod]
        public async Task GetTempDownloadLinkTest()
        {
            string fixUrl = await UploadATestFile();
            string url = await _client.GetTempDownloadLink(fixUrl);
            using (HttpClient http = new HttpClient())
            {
                var content = await http.GetStringAsync(url);
                Assert.AreEqual(_testContent, content);
            }
        }

        [TestMethod]
        public async Task ListFilesTest()
        {
            await UploadATestFile();
            List<FileSimpleInfo> files = new List<FileSimpleInfo>(await _client.ListFiles(_testPathId));
            Assert.AreEqual(1, files.Count);
            Assert.AreEqual(_testFileName, files[0].filename);
        }

        [TestMethod]
        public async Task OperationPathTest()
        {
            int idMake = await _client.MakePath(_testPath + "/1/2/3/4/5");
            int idGet = await _client.GetFolderId(_testPath + "/1/2/3/4/5");
            Assert.AreEqual(idMake, idGet);
        }

        [TestMethod]
        public async Task GetFolderIdFailedTest()
        {
            try
            {
                await _client.GetFolderId(_testPath + "/1/2/3/4/5");
                Assert.Fail("应该找不到文件夹。");
            }
            catch (FolderNotExistException)
            {

            }
        }

        [TestMethod]
        public async Task GetFileSimpleInfoTest()
        {
            await UploadATestFile();
            var fi = await _client.GetFileSimpleInfo(_testPath + _testFileName);
            Assert.AreEqual(_testFileName, fi.filename);
        }

        [TestMethod]
        public async Task GetFileSimpleInfoFailedTest()
        {
            try
            {
                await _client.GetFileSimpleInfo(_testPath + _testFileName);
                Assert.Fail("应该找不到文件。");
            }
            catch (FileNotExistException)
            {
                
            }
        }

        [TestMethod]
        public async Task GetFileFullInfoTest()
        {
            string url = await UploadATestFile();
            var info = await _client.GetFileFullInfo(url);
            Assert.AreEqual(url, info.url);
        }
    }
}
