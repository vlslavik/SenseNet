using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using System.Globalization;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.i18n;
using SenseNet.Search;
using SenseNet.Search.Indexing;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class GlobalizationTests : TestBase
    {
        #region test infrastructure
        private TestContext testContextInstance;

        public override TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }
        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion
        #endregion
        #region Playground
        private static string _testRootName = "_GlobalizationTests";
        private static string _testRootPath = String.Concat("/Root/", _testRootName);
        /// <summary>
        /// Do not use. Instead of TestRoot property
        /// </summary>
        private Node __testRoot;
        public Node TestRoot
        {
            get
            {
                if (__testRoot == null)
                {
                    __testRoot = Node.LoadNode(_testRootPath);
                    if (__testRoot == null)
                    {
                        Node node = NodeType.CreateInstance("SystemFolder", Node.LoadNode("/Root"));
                        node.Name = _testRootName;
                        node.Save();
                        __testRoot = Node.LoadNode(_testRootPath);
                    }
                }
                return __testRoot;
            }
        }
        [ClassInitialize]
        public static void CreatePlayground(TestContext testContext)
        {
        }
        [ClassCleanup]
        public static void DestroyPlayground()
        {
            if (Node.Exists(_testRootPath))
                Node.ForceDelete(_testRootPath);
        }
        #endregion

        Resource TheOneResource
        {
            get
            {
                var res = Node.Load<Resource>(RepositoryPath.Combine(TestRoot.Path, "TheOneResource"));
                if (res == null)
                {
                    res = new Resource(TestRoot) { Name = "TheOneResource" };
                    res.Save();
                    res.Reload();
                }
                return res;
            }
        }

        [TestMethod]
        public void Globalization_ResKeyParser()
        {
            string className, name, src;

            var ok = SenseNetResourceManager.ParseResourceKey(src = "$Resources:ClassName,Key", out className, out name);
            Assert.IsTrue(ok, "Failed to parse: " + src);
            Assert.IsTrue(className == "ClassName", String.Format("ClassName is '{0}', expected: 'ClassName', src: {1}", className, src));
            Assert.IsTrue(name == "Key", String.Format("Name is '{0}', expected: 'Key', src: {1}", name, src));

            ok = SenseNetResourceManager.ParseResourceKey(src = "$   Resources:   ClassName  ,  Key   ", out className, out name);
            Assert.IsTrue(ok, "Failed to parse: " + src);
            Assert.IsTrue(className == "ClassName", String.Format("ClassName is '{0}', expected: 'ClassName', src: {1}", className, src));
            Assert.IsTrue(name == "Key", String.Format("Name is '{0}', expected: 'Key', src: {1}", name, src));

            ok = SenseNetResourceManager.ParseResourceKey(src = "$ClassName,Key", out className, out name);
            Assert.IsTrue(ok, "Failed to parse: " + src);
            Assert.IsTrue(className == "ClassName", String.Format("ClassName is '{0}', expected: 'ClassName', src: {1}", className, src));
            Assert.IsTrue(name == "Key", String.Format("Name is '{0}', expected: 'Key', src: {1}", name, src));

            ok = SenseNetResourceManager.ParseResourceKey(src = "$      ClassName   ,   Key   ", out className, out name);
            Assert.IsTrue(ok, "Failed to parse: " + src);
            Assert.IsTrue(className == "ClassName", String.Format("ClassName is '{0}', expected: 'ClassName', src: {1}", className, src));
            Assert.IsTrue(name == "Key", String.Format("Name is '{0}', expected: 'Key', src: {1}", name, src));
        }
        [TestMethod]
        public void Globalization_LocalizedValue()
        {
            var en = CultureInfo.GetCultureInfo("en-US");
            var hu = CultureInfo.GetCultureInfo("hu-HU");
            AddResource(en, "TestResClass", "TestKey", "TestValue_en", TheOneResource);
            AddResource(hu, "TestResClass", "TestKey", "TestValue_hu", TheOneResource);

            var resDef = "$TestResClass,TestKey";
            var content = Content.CreateNew("Car", TestRoot, null);
            content["DisplayName"] = resDef;
            var field = content.Fields["DisplayName"];

            var stor = field.GetStoredValue();
            Assert.IsTrue(resDef == stor, String.Format("Stored value is '{0}', expected: '{1}'", stor, resDef));

            var loc = field.GetLocalizedValue(en);
            Assert.IsTrue("TestValue_en" == loc, String.Format("Localized value is '{0}', expected: '{1}'", loc, "TestValue_en"));

            var locHu = field.GetLocalizedValue(hu);
            Assert.IsTrue("TestValue_hu" == locHu, String.Format("Localized value is '{0}', expected: '{1}'", locHu, "TestValue_hu"));
        }

        [TestMethod]
        public void Globalization_IndexingLocalized()
        {
            var testResClass = "TestResClass";
            var testResKey = "TestDownload";
            var valueEn = "download now";
            var valueHu = "letöltés most";
            var valueDe = "jetzt herunterladen";
            var en = CultureInfo.GetCultureInfo("en-US");
            var hu = CultureInfo.GetCultureInfo("hu-HU");
            var de = CultureInfo.GetCultureInfo("de-DE");
            AddResource(en, testResClass, testResKey, valueEn, TheOneResource);
            AddResource(hu, testResClass, testResKey, valueHu, TheOneResource);
            AddResource(de, testResClass, testResKey, valueDe, TheOneResource);

            var resDef = String.Concat("$", testResClass, ",", testResKey);
            var content = Content.CreateNew("Car", TestRoot, null);
            content["DisplayName"] = resDef;
            content.Save();
            var versionId = content.ContentHandler.VersionId;

            var result = ContentQuery.Query(String.Format("DisplayName:{0} .AUTOFILTERS:OFF", resDef));
            Assert.IsTrue(result.Count == 0, String.Format("Resource key is found: {0}", resDef));

            result = ContentQuery.Query(String.Format("DisplayName:'{0}' .AUTOFILTERS:OFF", valueEn));
            Assert.IsTrue(result.Count == 1, String.Format("English value is not found: {0}", valueEn));

            result = ContentQuery.Query(String.Format("DisplayName:'{0}' .AUTOFILTERS:OFF", valueHu));
            Assert.IsTrue(result.Count == 1, String.Format("Hungarian value is not found: {0}", valueHu));

            result = ContentQuery.Query(String.Format("DisplayName:'{0}' .AUTOFILTERS:OFF", valueDe));
            Assert.IsTrue(result.Count == 1, String.Format("Deusch value is not found: {0}", valueDe));
        }

        [TestMethod]
        public void Globalization_IndexingLocalized_InvalidKey()
        {
            var testResClass = "TestResClass";
            var testResKey = "TestKey_Invalid";
            var valueEn = "value_en";
            var valueHu = "value_hu";
            var valueDe = "value_de";
            var en = CultureInfo.GetCultureInfo("en-US");
            var hu = CultureInfo.GetCultureInfo("hu-HU");
            var de = CultureInfo.GetCultureInfo("de-DE");
            AddResource(en, testResClass, testResKey, valueEn, TheOneResource);
            AddResource(hu, testResClass, testResKey, valueHu, TheOneResource);
            AddResource(de, testResClass, testResKey, valueDe, TheOneResource);

            var resDef = String.Concat("$", testResClass, ",xx,", testResKey);// invalid key
            var content = Content.CreateNew("Car", TestRoot, null);
            content["DisplayName"] = resDef;
            content.Save();
            var versionId = content.ContentHandler.VersionId;

            var result = ContentQuery.Query(String.Format("DisplayName:{0} .AUTOFILTERS:OFF", resDef));
            Assert.IsFalse(result.Count == 0, String.Format("Resource key is not found: {0}", resDef));

            result = ContentQuery.Query(String.Format("DisplayName:'{0}' .AUTOFILTERS:OFF", valueEn));
            Assert.IsFalse(result.Count == 1, String.Format("English value is found: {0}", valueEn));

            result = ContentQuery.Query(String.Format("DisplayName:'{0}' .AUTOFILTERS:OFF", valueHu));
            Assert.IsFalse(result.Count == 1, String.Format("Hungarian value is found: {0}", valueHu));

            result = ContentQuery.Query(String.Format("DisplayName:'{0}' .AUTOFILTERS:OFF", valueDe));
            Assert.IsFalse(result.Count == 1, String.Format("Deusch value is found: {0}", valueDe));
        }

        private void AddResource(CultureInfo cultureInfo, string className, string key, string value, Resource resourceContent)
        {
            var resManAcc = new PrivateObject(SenseNetResourceManager.Current);
            resManAcc.Invoke("AddItem", cultureInfo, className, key, value, resourceContent);
        }
    }
}
