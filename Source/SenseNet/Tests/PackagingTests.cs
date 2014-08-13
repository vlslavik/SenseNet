using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.Packaging;
using System.Xml;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using System.IO;
using SenseNet.Packaging.Steps;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class PackagingTests : TestBase
    {
        private class PackagingTestLogger : IPackagingLogger
        {
            public LogLevel AcceptedLevel { get { return LogLevel.File; } }
            private StringBuilder _sb;
            public PackagingTestLogger(StringBuilder sb)
            {
                _sb = sb;
            }
            public string LogFilePath { get { return "[in memory]"; } }
            public void Initialize(LogLevel level, string logFilePath) { }
            public void WriteTitle(string title)
            {
                _sb.AppendLine("================================");
                _sb.AppendLine(title);
                _sb.AppendLine("================================");
            }
            public void WriteMessage(string message)
            {
                _sb.AppendLine(message);
            }
        }

        private class WrongStepThatThrowsAnException : Step
        {
            public override void Execute(ExecutionContext context)
            {
                throw new ApplicationException("WrongStepThatThrowsAnException called.");
            }
        }


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

        private static StringBuilder _log;
        [TestInitialize]
        public void PrepareTest()
        {
            // preparing logger
            _log = new StringBuilder();
            var loggers = new[] { new PackagingTestLogger(_log) };
            var loggerAcc = new PrivateType(typeof(Logger));
            loggerAcc.SetStaticField("_loggers", loggers);

            // cleaning packages table
            DataProvider.Current.DeletePackagesExceptFirst();
            RepositoryVersionInfo.Reset();
        }

        private void EnsureApplicationPackage(string name, Guid appId, string edition, PackageLevel packageLevel, Version version, ExecutionResult executionResult)
        {
            if (DataProvider.Current.IsPackageExist(appId.ToString(), PackageType.Application, packageLevel, version))
                return;

            DataProvider.Current.SavePackage(new Package
            {
                AppId = appId == Guid.Empty ? null : appId.ToString(),
                ApplicationVersion = version,
                Edition = edition,
                Name = name,
                PackageType = PackageType.Application,
                PackageLevel = packageLevel,
                ReleaseDate = DateTime.UtcNow.AddDays(-1),
                ExecutionDate = DateTime.UtcNow,
                ExecutionResult = executionResult,
                SenseNetVersion = RepositoryVersionInfo.Instance.OfficialSenseNetVersion.Version
            });
            RepositoryVersionInfo.Reset();
        }
        private Version GetNextVersion(Version v)
        {
            var rev = v.Revision + 1;
            if (rev < 0)
                rev = 1;
            var build = v.Build;
            if (build < 0)
                build = 0;
            var minor = v.Minor;
            if (minor < 0)
                minor = 0;
            return new Version(v.Major, minor, build, rev);
        }

        private void EnsureProductPackage(string name, string edition, PackageLevel packageLevel, Version version, ExecutionResult executionResult)
        {
            if (DataProvider.Current.IsPackageExist(null, PackageType.Product, packageLevel, version))
                return;

            DataProvider.Current.SavePackage(new Package
            {
                AppId =  null ,
                ApplicationVersion = null,
                Edition = edition,
                Name = name,
                PackageType = PackageType.Product,
                PackageLevel = packageLevel,
                ReleaseDate = DateTime.UtcNow.AddDays(-1),
                ExecutionDate = DateTime.UtcNow,
                ExecutionResult = executionResult,
                SenseNetVersion = version
            });
            RepositoryVersionInfo.Reset();
        }

        /*===========================================================================================*/

        [TestMethod]
        public void Packaging_ExecutionResultEnumRightOrder()
        {
            Assert.IsTrue(ExecutionResult.Successful < ExecutionResult.Faulty);
            Assert.IsTrue(ExecutionResult.Faulty < ExecutionResult.Unfinished);
        }

        [TestMethod]
        public void Packaging_InvalidRoot()
        {
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_App_Patch_MissingReleaseDate", appId, null, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<XmlElement type='Application' level='Patch'>
                            <Name>TestTool</Name>
                            <AppId>" + appId + @"</AppId>
                            <VersionControl target='1.1' />
                            <Steps><Trace>Tool is running.</Trace></Steps>                        </XmlElement>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            try
            {
                manifestAcc.InvokeStatic("Parse", xml, true);
                Assert.Fail("InvalidPackageException was not thrown.");
            }
            catch (InvalidPackageException e)
            {
                Assert.AreEqual(SenseNet.Packaging.SR.Errors.Manifest.WrongRootName, e.Message);
            }
        }

        [TestMethod]
        public void Packaging_MissingLevel()
        {
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_App_Patch_Valid", appId, null, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application'></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            try
            {
                manifestAcc.InvokeStatic("Parse", xml, true);
                Assert.Fail("InvalidPackageException was not thrown.");
            }
            catch (InvalidPackageException e)
            {
                Assert.AreEqual(SenseNet.Packaging.SR.Errors.Manifest.MissingLevel, e.Message);
            }
        }
        [TestMethod]
        public void Packaging_InvalidLevel()
        {
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_InvalidLevel", appId, null, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='invalid'></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            try
            {
                manifestAcc.InvokeStatic("Parse", xml, true);
                Assert.Fail("InvalidPackageException was not thrown.");
            }
            catch (InvalidPackageException e)
            {
                Assert.AreEqual(SenseNet.Packaging.SR.Errors.Manifest.InvalidLevel, e.Message);
            }
        }
        [TestMethod]
        public void Packaging_MissingType()
        {
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_MissingType", appId, null, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package level='Patch'></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            try
            {
                manifestAcc.InvokeStatic("Parse", xml, true);
                Assert.Fail("InvalidPackageException was not thrown.");
            }
            catch (InvalidPackageException e)
            {
                Assert.AreEqual(SenseNet.Packaging.SR.Errors.Manifest.MissingType, e.Message);
            }
        }
        [TestMethod]
        public void Packaging_InvalidType()
        {
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_App_Patch_Valid", appId, null, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='invalid' level='Patch'></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            try
            {
                manifestAcc.InvokeStatic("Parse", xml, true);
                Assert.Fail("InvalidPackageException was not thrown.");
            }
            catch (InvalidPackageException e)
            {
                Assert.AreEqual(SenseNet.Packaging.SR.Errors.Manifest.InvalidType, e.Message);
            }
        }
        [TestMethod]
        public void Packaging_MissingName()
        {
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_MissingName", appId, null, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Patch'>
                            <AppId>" + appId + @"</AppId>
                            <VersionControl target='1.1' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            try
            {
                manifestAcc.InvokeStatic("Parse", xml, true);
                Assert.Fail("InvalidPackageException was not thrown.");
            }
            catch (InvalidPackageException e)
            {
                Assert.AreEqual(SenseNet.Packaging.SR.Errors.Manifest.MissingName, e.Message);
            }
        }
        [TestMethod]
        public void Packaging_InvalidName()
        {
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_MissingName", appId, null, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Patch'>
                            <Name></Name>
                            <AppId>" + appId + @"</AppId>
                            <VersionControl target='1.1' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            try
            {
                manifestAcc.InvokeStatic("Parse", xml, true);
                Assert.Fail("InvalidPackageException was not thrown.");
            }
            catch (InvalidPackageException e)
            {
                Assert.AreEqual(SenseNet.Packaging.SR.Errors.Manifest.InvalidName, e.Message);
            }
        }

        [TestMethod]
        public void Packaging_InvalidEdition()
        {
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_Edition", appId, null, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Tool'>
                            <Name>Packaging_Edition</Name>
                            <Edition />
                            <AppId>" + appId + @"</AppId>
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            try
            {
                manifestAcc.InvokeStatic("Parse", xml, true);
                Assert.Fail("InvalidPackageException was not thrown.");
            }
            catch (InvalidPackageException e)
            {
                Assert.AreEqual(SenseNet.Packaging.SR.Errors.Manifest.InvalidEdition, e.Message);
            }
        }
        [TestMethod]
        public void Packaging_ValidEdition_Null()
        {
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_Edition", appId, null, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Tool'>
                            <Name>Packaging_Edition</Name>
                            <AppId>" + appId + @"</AppId>
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            var manifest = (Manifest)manifestAcc.InvokeStatic("Parse", xml, true);
            Assert.IsNull(manifest.Edition);
        }
        [TestMethod]
        public void Packaging_ValidEdition_NotNull()
        {
            var edition = "Enterprise";
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_Edition", appId, edition, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Tool'>
                            <Name>Packaging_Edition</Name>
                            <Edition>" + edition + @"</Edition>
                            <AppId>" + appId + @"</AppId>
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            var manifest = (Manifest)manifestAcc.InvokeStatic("Parse", xml, true);
            Assert.AreEqual(edition, manifest.Edition);
        }
        [TestMethod]
        public void Packaging_EditionMismatch_InstalledNull()
        {
            var edition = "Enterprise";
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_Edition", appId, null, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Tool'>
                            <Name>Packaging_Edition</Name>
                            <Edition>" + edition + @"</Edition>
                            <AppId>" + appId + @"</AppId>
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            try
            {
                manifestAcc.InvokeStatic("Parse", xml, true);
                Assert.Fail("InvalidPackageException was not thrown.");
            }
            catch (PackagePreconditionException  e)
            {
                Assert.AreEqual(String.Format(SenseNet.Packaging.SR.Errors.Precondition.EditionMismatch_2, "[empty]", edition), e.Message);
            }
        }
        [TestMethod]
        public void Packaging_EditionMismatch_InstalledNotNull()
        {
            var edition = "Enterprise";
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_Edition", appId, edition, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Tool'>
                            <Name>Packaging_Edition</Name>
                            <Edition>Demo</Edition>
                            <AppId>" + appId + @"</AppId>
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            try
            {
                manifestAcc.InvokeStatic("Parse", xml, true);
                Assert.Fail("InvalidPackageException was not thrown.");
            }
            catch (PackagePreconditionException e)
            {
                Assert.AreEqual(String.Format(SenseNet.Packaging.SR.Errors.Precondition.EditionMismatch_2, edition, "Demo"), e.Message);
            }
        }

        /*----------------------------------------------------------------------------*/

        [TestMethod]
        public void Packaging_App_Patch_MissingReleaseDate()
        {
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_App_Patch_MissingReleaseDate", appId, null, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Patch'>
                            <Name>TestTool</Name>
                            <AppId>" + appId + @"</AppId>
                            <VersionControl target='1.1' />
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            try
            {
                manifestAcc.InvokeStatic("Parse", xml, true);
                Assert.Fail("InvalidPackageException was not thrown.");
            }
            catch (InvalidPackageException e)
            {
                Assert.AreEqual(SenseNet.Packaging.SR.Errors.Manifest.MissingReleaseDate, e.Message);
            }
        }
        [TestMethod]
        public void Packaging_App_Patch_InvalidReleaseDate()
        {
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_App_Patch_InvalidReleaseDate", appId, null, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Patch'>
                            <Name>TestTool</Name>
                            <AppId>" + appId + @"</AppId>
                            <VersionControl target='1.1' />
                            <ReleaseDate />
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            try
            {
                manifestAcc.InvokeStatic("Parse", xml, true);
                Assert.Fail("InvalidPackageException was not thrown.");
            }
            catch (InvalidPackageException e)
            {
                Assert.AreEqual(SenseNet.Packaging.SR.Errors.Manifest.InvalidReleaseDate, e.Message);
            }
        }
        [TestMethod]
        public void Packaging_App_Patch_MissingAppId()
        {
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Patch'>
                            <Name>TestTool</Name>
                            <VersionControl target='1.1' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            try
            {
                manifestAcc.InvokeStatic("Parse", xml, true);
                Assert.Fail("InvalidPackageException was not thrown.");
            }
            catch (InvalidPackageException e)
            {
                Assert.AreEqual(SenseNet.Packaging.SR.Errors.Manifest.MissingAppId, e.Message);
            }
        }
        [TestMethod]
        public void Packaging_App_Patch_InvalidAppId()
        {
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Patch'>
                            <Name>TestTool</Name>
                            <AppId></AppId>
                            <VersionControl target='1.1' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            try
            {
                manifestAcc.InvokeStatic("Parse", xml, true);
                Assert.Fail("InvalidPackageException was not thrown.");
            }
            catch (InvalidPackageException e)
            {
                Assert.AreEqual(SenseNet.Packaging.SR.Errors.Manifest.InvalidAppId, e.Message);
            }
        }
        [TestMethod]
        public void Packaging_App_Patch_Valid()
        {
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_App_Patch_Valid", appId, null, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Patch'>
                            <Name>TestTool</Name>
                            <AppId>" + appId + @"</AppId>
                            <VersionControl target='1.1' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            var manifest = (Manifest)manifestAcc.InvokeStatic("Parse", xml, true);
        }

        [TestMethod]
        public void Packaging_App_ServicePack_MissingReleaseDate()
        {
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_App_ServicePack_MissingReleaseDate", appId, null, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='ServicePack'>
                            <Name>TestTool</Name>
                            <AppId>" + appId + @"</AppId>
                            <VersionControl target='1.1' />
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            try
            {
                manifestAcc.InvokeStatic("Parse", xml, true);
                Assert.Fail("InvalidPackageException was not thrown.");
            }
            catch (InvalidPackageException e)
            {
                Assert.AreEqual(SenseNet.Packaging.SR.Errors.Manifest.MissingReleaseDate, e.Message);
            }
        }
        [TestMethod]
        public void Packaging_App_ServicePack_InvalidReleaseDate()
        {
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_App_ServicePack_InvalidReleaseDate", appId, null, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='ServicePack'>
                            <Name>TestTool</Name>
                            <AppId>" + appId + @"</AppId>
                            <VersionControl target='1.1' />
                            <ReleaseDate />
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            try
            {
                manifestAcc.InvokeStatic("Parse", xml, true);
                Assert.Fail("InvalidPackageException was not thrown.");
            }
            catch (InvalidPackageException e)
            {
                Assert.AreEqual(SenseNet.Packaging.SR.Errors.Manifest.InvalidReleaseDate, e.Message);
            }
        }
        [TestMethod]
        public void Packaging_App_ServicePack_MissingAppId()
        {
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='ServicePack'>
                            <Name>TestTool</Name>
                            <VersionControl target='1.1' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");
            // private static Manifest Parse(XmlDocument xml, bool log
            var manifestAcc = new PrivateType(typeof(Manifest));
            try
            {
                manifestAcc.InvokeStatic("Parse", xml, true);
                Assert.Fail("InvalidPackageException was not thrown.");
            }
            catch (InvalidPackageException e)
            {
                Assert.AreEqual(SenseNet.Packaging.SR.Errors.Manifest.MissingAppId, e.Message);
            }
        }
        [TestMethod]
        public void Packaging_App_ServicePack_InvalidAppId()
        {
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='ServicePack'>
                            <Name>TestTool</Name>
                            <AppId></AppId>
                            <VersionControl target='1.1' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");
            // private static Manifest Parse(XmlDocument xml, bool log
            var manifestAcc = new PrivateType(typeof(Manifest));
            try
            {
                manifestAcc.InvokeStatic("Parse", xml, true);
                Assert.Fail("InvalidPackageException was not thrown.");
            }
            catch (InvalidPackageException e)
            {
                Assert.AreEqual(SenseNet.Packaging.SR.Errors.Manifest.InvalidAppId, e.Message);
            }
        }
        [TestMethod]
        public void Packaging_App_ServicePack_Valid()
        {
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_App_ServicePack_Valid", appId, null, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='ServicePack'>
                            <Name>TestTool</Name>
                            <AppId>" + appId + @"</AppId>
                            <VersionControl target='1.1' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            var manifest = (Manifest)manifestAcc.InvokeStatic("Parse", xml, true);
        }

        [TestMethod]
        public void Packaging_App_Upgrade_MissingReleaseDate()
        {
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_App_Upgrade_MissingReleaseDate", appId, null, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Upgrade'>
                            <Name>TestTool</Name>
                            <AppId>" + appId + @"</AppId>
                            <VersionControl target='1.1' />
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            try
            {
                manifestAcc.InvokeStatic("Parse", xml, true);
                Assert.Fail("InvalidPackageException was not thrown.");
            }
            catch (InvalidPackageException e)
            {
                Assert.AreEqual(SenseNet.Packaging.SR.Errors.Manifest.MissingReleaseDate, e.Message);
            }
        }
        [TestMethod]
        public void Packaging_App_Upgrade_InvalidReleaseDate()
        {
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_App_Upgrade_InvalidReleaseDate", appId, null, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Upgrade'>
                            <Name>TestTool</Name>
                            <AppId>" + appId + @"</AppId>
                            <VersionControl target='1.1' />
                            <ReleaseDate />
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            try
            {
                manifestAcc.InvokeStatic("Parse", xml, true);
                Assert.Fail("InvalidPackageException was not thrown.");
            }
            catch (InvalidPackageException e)
            {
                Assert.AreEqual(SenseNet.Packaging.SR.Errors.Manifest.InvalidReleaseDate, e.Message);
            }
        }
        [TestMethod]
        public void Packaging_App_Upgrade_MissingAppId()
        {
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Upgrade'>
                            <Name>TestTool</Name>
                            <VersionControl target='1.1' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");
            // private static Manifest Parse(XmlDocument xml, bool log
            var manifestAcc = new PrivateType(typeof(Manifest));
            try
            {
                manifestAcc.InvokeStatic("Parse", xml, true);
                Assert.Fail("InvalidPackageException was not thrown.");
            }
            catch (InvalidPackageException e)
            {
                Assert.AreEqual(SenseNet.Packaging.SR.Errors.Manifest.MissingAppId, e.Message);
            }
        }
        [TestMethod]
        public void Packaging_App_Upgrade_InvalidAppId()
        {
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Upgrade'>
                            <Name>TestTool</Name>
                            <AppId></AppId>
                            <VersionControl target='1.1' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            try
            {
                manifestAcc.InvokeStatic("Parse", xml, true);
                Assert.Fail("InvalidPackageException was not thrown.");
            }
            catch (InvalidPackageException e)
            {
                Assert.AreEqual(SenseNet.Packaging.SR.Errors.Manifest.InvalidAppId, e.Message);
            }
        }
        [TestMethod]
        public void Packaging_App_Upgrade_Valid()
        {
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_App_Upgrade_Valid", appId, null, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Upgrade'>
                            <Name>TestTool</Name>
                            <AppId>" + appId + @"</AppId>
                            <VersionControl target='1.1' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            var manifest = (Manifest)manifestAcc.InvokeStatic("Parse", xml, true);
        }

        [TestMethod]
        public void Packaging_CannotInstallInstalled()
        {
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_App_Upgrade_MissingReleaseDate", appId, null, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Install'>
                            <Name>TestTool</Name>
                            <AppId>" + appId + @"</AppId>
                            <VersionControl target='1.1' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            try
            {
                manifestAcc.InvokeStatic("Parse", xml, true);
                Assert.Fail("InvalidPackageException was not thrown.");
            }
            catch (PackagePreconditionException e)
            {
                Assert.AreEqual(SenseNet.Packaging.SR.Errors.Precondition.CannotInstallExistingApp, e.Message);
            }
        }
        [TestMethod]
        public void Packaging_CannotUpgradeNotInstalled()
        {
            var appId = Guid.NewGuid();
            //EnsureApplicationPackage("Packaging_App_Upgrade_MissingReleaseDate", appId, null, PackageLevel.Install, new Version(1, 0));
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Upgrade'>
                            <Name>TestTool</Name>
                            <AppId>" + appId + @"</AppId>
                            <VersionControl target='2.0' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");
            var manifestAcc = new PrivateType(typeof(Manifest));
            try
            {
                manifestAcc.InvokeStatic("Parse", xml, true);
                Assert.Fail("InvalidPackageException was not thrown.");
            }
            catch (PackagePreconditionException e)
            {
                Assert.AreEqual(SenseNet.Packaging.SR.Errors.Precondition.AppIdDoesNotMatch, e.Message);
            }
        }

        [TestMethod]
        public void Packaging_App_CannotUpgradeDifferentEdition()
        {
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_App_Upgrade_MissingReleaseDate", appId, "Demo", PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Upgrade'>
                            <Name>TestTool</Name>
                            <Edition>Enterprise</Edition>
                            <AppId>" + appId + @"</AppId>
                            <VersionControl target='2.0' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");
            var manifestAcc = new PrivateType(typeof(Manifest));
            try
            {
                manifestAcc.InvokeStatic("Parse", xml, true);
                Assert.Fail("InvalidPackageException was not thrown.");
            }
            catch (PackagePreconditionException e)
            {
                Assert.AreEqual(String.Format(SenseNet.Packaging.SR.Errors.Precondition.EditionMismatch_2, "Demo", "Enterprise"), e.Message);
            }
        }

        [TestMethod]
        public void Packaging_App_Versioning_UnexpectedTarget()
        {
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_App_Versioning_Unexpected", appId, null, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Tool'>
                            <Name>TestTool</Name>
                            <AppId>" + appId + @"</AppId>
                            <VersionControl target='1.1' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            try
            {
                manifestAcc.InvokeStatic("Parse", xml, true);
                Assert.Fail("InvalidPackageException was not thrown.");
            }
            catch (InvalidPackageException e)
            {
                Assert.AreEqual(SenseNet.Packaging.SR.Errors.Manifest.UnexpectedTarget, e.Message);
            }
        }
        [TestMethod]
        public void Packaging_App_Versioning_TargetTooSmall()
        {
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_App_Versioning_Unexpected", appId, null, PackageLevel.Install, new Version(1, 1), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Patch'>
                            <Name>TestTool</Name>
                            <AppId>" + appId + @"</AppId>
                            <VersionControl target='1.1' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            try
            {
                manifestAcc.InvokeStatic("Parse", xml, true);
                Assert.Fail("InvalidPackageException was not thrown.");
            }
            catch (PackagePreconditionException e)
            {
                // SenseNet.Packaging.PackagePreconditionException: Invalid manifest: the target version (1.1) must be greater than the current application version (2.0).
                Assert.AreEqual(String.Format(SenseNet.Packaging.SR.Errors.Precondition.TargetVersionTooSmall_3, "application", "1.1", "1.1"), e.Message);
            }
        }
        [TestMethod]
        public void Packaging_App_Versioning_UnexpectedExpected()
        {
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_App_Versioning_UnexpectedExpected", appId, null, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Patch'>
                            <Name>TestTool</Name>
                            <AppId>" + appId + @"</AppId>
                            <VersionControl target='1.1' expectedMin='1.0' expectedMax='1.0' expected='1.0' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            try
            {
                manifestAcc.InvokeStatic("Parse", xml, true);
                Assert.Fail("InvalidPackageException was not thrown.");
            }
            catch (InvalidPackageException e)
            {
                Assert.AreEqual(SenseNet.Packaging.SR.Errors.Manifest.UnexpectedExpectedVersion, e.Message);
            }
        }

        [TestMethod]
        public void Packaging_App_Versioning_Expected()
        {
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_App_Versioning_ExpectedMinMax", appId, null, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Patch'>
                            <Name>TestTool</Name>
                            <AppId>" + appId + @"</AppId>
                            <VersionControl target='1.1' expected='1.0' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            var manifest = (Manifest)manifestAcc.InvokeStatic("Parse", xml, true);

            Assert.AreEqual(Version.Parse("1.0"), manifest.VersionControl.ExpectedApplicationMinimum);
            Assert.AreEqual(Version.Parse("1.0"), manifest.VersionControl.ExpectedApplicationMaximum);
        }
        [TestMethod]
        public void Packaging_App_Versioning_ExpectedMinMax()
        {
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_App_Versioning_ExpectedMinMax", appId, null, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Patch'>
                            <Name>TestTool</Name>
                            <AppId>" + appId + @"</AppId>
                            <VersionControl target='1.1' expectedMin='1.0' expectedMax='1.2' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            var manifest = (Manifest)manifestAcc.InvokeStatic("Parse", xml, true);

            Assert.AreEqual(Version.Parse("1.0"), manifest.VersionControl.ExpectedApplicationMinimum);
            Assert.AreEqual(Version.Parse("1.2"), manifest.VersionControl.ExpectedApplicationMaximum);
        }
        [TestMethod]
        public void Packaging_App_Versioning_ExpectedMin()
        {
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_App_Versioning_ExpectedMinMax", appId, null, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Patch'>
                            <Name>TestTool</Name>
                            <AppId>" + appId + @"</AppId>
                            <VersionControl target='1.1' expectedMin='1.0' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            var manifest = (Manifest)manifestAcc.InvokeStatic("Parse", xml, true);

            Assert.AreEqual(Version.Parse("1.0"), manifest.VersionControl.ExpectedApplicationMinimum);
            Assert.AreEqual(null, manifest.VersionControl.ExpectedApplicationMaximum);
        }
        [TestMethod]
        public void Packaging_App_Versioning_ExpectedMax()
        {
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_App_Versioning_ExpectedMinMax", appId, null, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Patch'>
                            <Name>TestTool</Name>
                            <AppId>" + appId + @"</AppId>
                            <VersionControl target='1.1' expectedMax='1.2' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            var manifest = (Manifest)manifestAcc.InvokeStatic("Parse", xml, true);

            Assert.AreEqual(null, manifest.VersionControl.ExpectedApplicationMinimum);
            Assert.AreEqual(Version.Parse("1.2"), manifest.VersionControl.ExpectedApplicationMaximum);
        }
        [TestMethod]
        public void Packaging_App_Versioning_WithoutExpectations()
        {
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_App_Versioning_ExpectedMinMax", appId, null, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Patch'>
                            <Name>TestTool</Name>
                            <AppId>" + appId + @"</AppId>
                            <VersionControl target='1.1' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            var manifest = (Manifest)manifestAcc.InvokeStatic("Parse", xml, true);

            Assert.IsNull(manifest.VersionControl.ExpectedApplicationMinimum);
            Assert.IsNull(manifest.VersionControl.ExpectedApplicationMaximum);
        }

        [TestMethod]
        public void Packaging_App_Versioning_ExpectedMinTooBig()
        {
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_App_Versioning_ExpectedMinTooBig", appId, null, PackageLevel.Install, new Version(2, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Patch'>
                            <Name>TestTool</Name>
                            <AppId>" + appId + @"</AppId>
                            <VersionControl target='1.1' expectedMin='2.1' expectedMax='2.9' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");
            var manifestAcc = new PrivateType(typeof(Manifest));
            try
            {
                manifestAcc.InvokeStatic("Parse", xml, true);
                Assert.Fail("InvalidPackageException was not thrown.");
            }
            catch (PackagePreconditionException e)
            {
                Assert.AreEqual(String.Format(SenseNet.Packaging.SR.Errors.Precondition.MinimumVersion_1, "application"), e.Message);
            }
        }
        [TestMethod]
        public void Packaging_App_Versioning_ExpectedMaxTooSmall()
        {
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_App_Versioning_ExpectedMaxTooSmall", appId, null, PackageLevel.Install, new Version(2, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Patch'>
                            <Name>TestTool</Name>
                            <AppId>" + appId + @"</AppId>
                            <VersionControl target='1.1' expectedMin='1.0' expectedMax='1.9' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");
            var manifestAcc = new PrivateType(typeof(Manifest));
            try
            {
                manifestAcc.InvokeStatic("Parse", xml, true);
                Assert.Fail("InvalidPackageException was not thrown.");
            }
            catch (PackagePreconditionException e)
            {
                Assert.AreEqual(String.Format(SenseNet.Packaging.SR.Errors.Precondition.MaximumVersion_1, "application"), e.Message);
            }
        }

        /*----------------------------------------------------------------------------*/

        [TestMethod]
        public void Packaging_Prod_Tool_WithoutEdition()
        {
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Product' level='Tool'>
                            <Name>TestTool</Name>
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");
            var manifestAcc = new PrivateType(typeof(Manifest));
            manifestAcc.InvokeStatic("Parse", xml, true);
        }
        [TestMethod]
        public void Packaging_Prod_Patch_MissingEdition()
        {
            var verInfo = RepositoryVersionInfo.Instance.OfficialSenseNetVersion;
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Product' level='Patch'>
                            <Name>TestTool</Name>
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <VersionControl target='" + GetNextVersion(verInfo.Version) + @"' />
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");
            var manifestAcc = new PrivateType(typeof(Manifest));
            try
            {
                manifestAcc.InvokeStatic("Parse", xml, true);
                Assert.Fail("InvalidPackageException was not thrown.");
            }
            catch (InvalidPackageException e)
            {
                Assert.AreEqual(SenseNet.Packaging.SR.Errors.Manifest.MissingEdition, e.Message);
            }
        }
        [TestMethod]
        public void Packaging_Prod_UnexpectedAppId()
        {
            var verInfo = RepositoryVersionInfo.Instance.OfficialSenseNetVersion;
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Product' level='Tool'>
                            <Name>TestTool</Name>
                            <AppId>unexpected-identifier</AppId>
                            <Edition>" + verInfo.Edition + @"</Edition>
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");
            var manifestAcc = new PrivateType(typeof(Manifest));
            try
            {
                manifestAcc.InvokeStatic("Parse", xml, true);
                Assert.Fail("InvalidPackageException was not thrown.");
            }
            catch (InvalidPackageException e)
            {
                Assert.AreEqual(SenseNet.Packaging.SR.Errors.Manifest.UnexpectedAppId, e.Message);
            }
        }
        [TestMethod]
        public void Packaging_Prod_Valid_Tool()
        {
            var verInfo = RepositoryVersionInfo.Instance.OfficialSenseNetVersion;
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Product' level='Tool'>
                            <Name>TestTool</Name>
                            <Edition>" + verInfo.Edition + @"</Edition>
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            var manifest = (Manifest)manifestAcc.InvokeStatic("Parse", xml, true);
        }
        [TestMethod]
        public void Packaging_Prod_Valid_Patch()
        {
            var verInfo = RepositoryVersionInfo.Instance.OfficialSenseNetVersion;
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Product' level='Patch'>
                            <Name>TestTool</Name>
                            <Edition>" + verInfo.Edition + @"</Edition>
                            <VersionControl target='" + GetNextVersion(verInfo.Version) + @"' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            var manifest = (Manifest)manifestAcc.InvokeStatic("Parse", xml, true);
        }
        
        [TestMethod]
        public void Packaging_Prod_Versioning_UnexpectedTarget()
        {
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_App_Versioning_Unexpected", appId, null, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Product' level='Tool'>
                            <Name>TestTool</Name>

                            <VersionControl target='1.1' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            try
            {
                manifestAcc.InvokeStatic("Parse", xml, true);
                Assert.Fail("InvalidPackageException was not thrown.");
            }
            catch (InvalidPackageException e)
            {
                Assert.AreEqual(SenseNet.Packaging.SR.Errors.Manifest.UnexpectedTarget, e.Message);
            }
        }
        [TestMethod]
        public void Packaging_Prod_Versioning_Expected()
        {
            Debug.WriteLine("@#$Test> Product version: " + RepositoryVersionInfo.Instance.OfficialSenseNetVersion.Version); 
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Product' level='Patch'>
                            <Name>TestTool</Name>
                            <Edition>Unofficial</Edition>
                            <VersionControl target='6.3.2' expected='6.3.1' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            var manifest = (Manifest)manifestAcc.InvokeStatic("Parse", xml, true);

            Assert.AreEqual(Version.Parse("6.3.1"), manifest.VersionControl.ExpectedProductMinimum);
            Assert.AreEqual(Version.Parse("6.3.1"), manifest.VersionControl.ExpectedProductMaximum);
        }
        [TestMethod]
        public void Packaging_Prod_Versioning_ExpectedMinMax()
        {
            var appId = Guid.NewGuid();
            EnsureProductPackage("Packaging_App_Versioning_ExpectedMinMax", null, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Product' level='Patch'>
                            <Name>TestTool</Name>
                            <Edition>Unofficial</Edition>
                            <VersionControl target='6.3.2' expectedMin='6.2.0' expectedMax='6.3.1' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            var manifest = (Manifest)manifestAcc.InvokeStatic("Parse", xml, true);

            Assert.AreEqual(Version.Parse("6.2.0"), manifest.VersionControl.ExpectedProductMinimum);
            Assert.AreEqual(Version.Parse("6.3.1"), manifest.VersionControl.ExpectedProductMaximum);
        }
        [TestMethod]
        public void Packaging_Prod_Versioning_ExpectedMin()
        {
            var appId = Guid.NewGuid();
            EnsureProductPackage("Packaging_App_Versioning_ExpectedMinMax", null, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Product' level='Patch'>
                            <Name>TestTool</Name>
                            <Edition>Unofficial</Edition>
                            <VersionControl target='6.3.2' expectedMin='6.2.0' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            var manifest = (Manifest)manifestAcc.InvokeStatic("Parse", xml, true);

            Assert.AreEqual(Version.Parse("6.2.0"), manifest.VersionControl.ExpectedProductMinimum);
            Assert.AreEqual(null, manifest.VersionControl.ExpectedProductMaximum);
        }
        [TestMethod]
        public void Packaging_Prod_Versioning_ExpectedMax()
        {
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_App_Versioning_ExpectedMinMax", appId, null, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Product' level='Patch'>
                            <Name>TestTool</Name>
                            <Edition>Unofficial</Edition>
                            <VersionControl target='6.3.2' expectedMax='6.3.1' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            var manifest = (Manifest)manifestAcc.InvokeStatic("Parse", xml, true);

            Assert.AreEqual(null, manifest.VersionControl.ExpectedProductMinimum);
            Assert.AreEqual(Version.Parse("6.3.1"), manifest.VersionControl.ExpectedProductMaximum);
        }
        [TestMethod]
        public void Packaging_Prod_Versioning_WithoutExpectations()
        {
            var appId = Guid.NewGuid();
            EnsureApplicationPackage("Packaging_App_Versioning_ExpectedMinMax", appId, null, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Product' level='Patch'>
                            <Name>TestTool</Name>
                            <Edition>Unofficial</Edition>
                            <VersionControl target='6.3.2' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            var manifest = (Manifest)manifestAcc.InvokeStatic("Parse", xml, true);

            Assert.IsNull(manifest.VersionControl.ExpectedProductMinimum);
            Assert.IsNull(manifest.VersionControl.ExpectedProductMinimum);
        }

        /*----------------------------------------------------------------------------*/

        [TestMethod]
        public void Packaging_App_ValidFlow()
        {
            var appId = Guid.NewGuid();
            var verInfo = RepositoryVersionInfo.Instance;
            Assert.IsNull(verInfo.Applications.FirstOrDefault(a => a.AppId == appId.ToString()));

            var appName = "Packaging_App_ValidFlow";
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Install'>
                            <Name>" + appName + @"</Name>
                            <AppId>" + appId + @"</AppId>
                            <VersionControl target='1.1' expectedMin='1.0' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            var manifest = (Manifest)manifestAcc.InvokeStatic("Parse", xml, true);
            var console = new StringWriter();
            for (int phase = 0; phase < manifest.CountOfPhases; phase++)
			{
                var executionContext = new ExecutionContext("packagePath", "targetPath", new string[0], "sandboxPath", manifest, phase, manifest.CountOfPhases, console);
                var pkgManAcc = new PrivateType(typeof(PackageManager));
                var result = (PackagingResult)pkgManAcc.InvokeStatic("ExecuteCurrentPhase", manifest, executionContext);
			}

            var log = _log.ToString();
            Assert.IsTrue(log.Contains("Tool is running."));
            Assert.IsTrue(log.Contains("Errors: 0"));

            verInfo = RepositoryVersionInfo.Instance; // must refresh
            var appInfo = verInfo.Applications.FirstOrDefault(a => a.AppId == appId.ToString());

            Assert.IsNotNull(appInfo);
            Assert.AreEqual(Version.Parse("1.1"), appInfo.Version);
            Assert.AreEqual(appName, appInfo.Name);
        }

        /*===========================================================================================*/

        [TestMethod]
        public void Packaging_Step_ResolveNetworkTargets()
        {
            var stepAcc = new PrivateType(typeof(Step));
            var ctx = new ExecutionContext(@"C:\SnWeb_admin", @"C:\SnWeb", new[] { @"\\Server1\SnWeb", @"\\Server2\SnWeb" }, @"C:\SnWeb_admin\run", null, 0, 1, null);
            var paths = (string[])stepAcc.InvokeStatic("ResolveNetworkTargets", @"App_Data\Folder1\File1", ctx);

            Assert.AreEqual(@"\\Server1\SnWeb\App_Data\Folder1\File1", paths[0]);
            Assert.AreEqual(@"\\Server2\SnWeb\App_Data\Folder1\File1", paths[1]);
        }

        [TestMethod]
        public void Packaging_Parameters_RegexMatch()
        {
            Assert.IsTrue(Regex.Match("Phase12.Step12:asdf", StepParameter.ParameterRegex, RegexOptions.IgnoreCase).Success);
            Assert.IsTrue(Regex.Match("Step1:asdf", StepParameter.ParameterRegex, RegexOptions.IgnoreCase).Success);
            Assert.IsTrue(Regex.Match("Step1.Phase3:asdf", StepParameter.ParameterRegex, RegexOptions.IgnoreCase).Success);
            Assert.IsFalse(Regex.Match("Phase12:asdf", StepParameter.ParameterRegex, RegexOptions.IgnoreCase).Success);
            Assert.IsFalse(Regex.Match("Phase12s.Step1:asdf", StepParameter.ParameterRegex, RegexOptions.IgnoreCase).Success);
            Assert.IsFalse(Regex.Match("Phase12.Step1e:asdf", StepParameter.ParameterRegex, RegexOptions.IgnoreCase).Success);
            Assert.IsFalse(Regex.Match("Phare12.Step12:asdf", StepParameter.ParameterRegex, RegexOptions.IgnoreCase).Success);
            Assert.IsFalse(Regex.Match("Phase12.Slep12:asdf", StepParameter.ParameterRegex, RegexOptions.IgnoreCase).Success);
            Assert.IsTrue(Regex.Match("Phase12.Step12.qwer:asdf", StepParameter.ParameterRegex, RegexOptions.IgnoreCase).Success);
        }

        [TestMethod]
        public void Packaging_Parameters_Parse()
        {
            Assert.AreEqual("PHASE1.STEP42:asdf", StepParameterToString(StepParameter.Parse("Step42:asdf")));
            Assert.AreEqual("PHASE1.STEP3.Property1:asdf", StepParameterToString(StepParameter.Parse("Step3.Property1:asdf")));
            Assert.AreEqual("PHASE1.STEP4:asdf", StepParameterToString(StepParameter.Parse("Phase1.Step4:asdf")));
            Assert.AreEqual("PHASE2.STEP5:asdf", StepParameterToString(StepParameter.Parse("Phase2.Step5:asdf")));
            Assert.AreEqual("PHASE3.STEP6.Property2:asdf", StepParameterToString(StepParameter.Parse("Phase3.Step6.Property2:asdf")));
        }
        private string StepParameterToString(StepParameter prm)
        {
            if (prm.PropertyName.Length == 0)
                return String.Format("PHASE{0}.STEP{1}:{2}", prm.PhaseIndex + 1, prm.StepIndex + 1, prm.Value);
            return String.Format("PHASE{0}.STEP{1}.{2}:{3}", prm.PhaseIndex + 1, prm.StepIndex + 1, prm.PropertyName, prm.Value);
        }

        /*===========================================================================================*/


        [TestMethod]
        public void Packaging_App_UnfinishedResultOnFaulty()
        {
            var appId = Guid.NewGuid();
            var appIdStr = appId.ToString();
            var appName = "Packaging_App_UnfinishedResult";
            string ed = null;
            EnsureApplicationPackage(appName, appId, ed, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            EnsureApplicationPackage(appName, appId, ed, PackageLevel.Patch, new Version(1, 1), ExecutionResult.Successful);
            EnsureApplicationPackage(appName, appId, ed, PackageLevel.Patch, new Version(1, 2), ExecutionResult.Faulty);

            var verInfo = RepositoryVersionInfo.Instance;

            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Patch'>
                            <AppId>" + appIdStr + @"</AppId>
                            <Name>" + appName + @"</Name>
                            <VersionControl target='1.2' expected='1.1' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps>
                                <Phase><Trace>1st phase</Trace></Phase>
                                <Phase><Trace>2nd phase</Trace></Phase>
                                <Phase><Trace>3nd phase</Trace></Phase>
                            </Steps>
                          </Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            var manifest = (Manifest)manifestAcc.InvokeStatic("Parse", xml, true);

            var executionResults = new List<ExecutionResult>();
            var console = new StringWriter();
            for (int phase = 0; phase < manifest.CountOfPhases; phase++)
            {
                var executionContext = new ExecutionContext("packagePath", "targetPath", new string[0], "sandboxPath", manifest, phase, manifest.CountOfPhases, console);
                var pkgManAcc = new PrivateType(typeof(PackageManager));
                try
                {
                    var result = (PackagingResult)pkgManAcc.InvokeStatic("ExecuteCurrentPhase", manifest, executionContext);
                }
                catch { break; } // intentionally suppressed

                RepositoryVersionInfo.Reset(); // must refresh
                verInfo = RepositoryVersionInfo.Instance;
                executionResults.Add(verInfo.InstalledPackages.Where(p => p.AppId == appIdStr).OrderBy(p => p.Id).Last().ExecutionResult);
            }
            Assert.AreEqual("Unfinished, Unfinished, Successful", String.Join(", ", executionResults));

            var log = _log.ToString();

            var appInfo = verInfo.Applications.Where(a => a.AppId == appIdStr).First();

            Assert.AreEqual(Version.Parse("1.2"), appInfo.Version);
            Assert.AreEqual(Version.Parse("1.2"), appInfo.AcceptableVersion);

            var actual = String.Join(", ", verInfo.InstalledPackages
                .Where(p => p.AppId == appIdStr)
                .OrderBy(p => p.ApplicationVersion)
                .Select(p => String.Format("{0}: {1}", p.ApplicationVersion, p.ExecutionResult)));
            Assert.AreEqual("1.0: Successful, 1.1: Successful, 1.2: Faulty, 1.2: Successful", actual);
        }
        [TestMethod]
        public void Packaging_App_UnfinishedResultOnUnfinished()
        {
            var appId = Guid.NewGuid();
            var appIdStr = appId.ToString();
            var appName = "Packaging_App_UnfinishedResult";
            string ed = null;
            EnsureApplicationPackage(appName, appId, ed, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            EnsureApplicationPackage(appName, appId, ed, PackageLevel.Patch, new Version(1, 1), ExecutionResult.Successful);
            EnsureApplicationPackage(appName, appId, ed, PackageLevel.Patch, new Version(1, 2), ExecutionResult.Unfinished);

            var verInfo = RepositoryVersionInfo.Instance;

            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Patch'>
                            <AppId>" + appIdStr + @"</AppId>
                            <Name>" + appName + @"</Name>
                            <VersionControl target='1.2' expected='1.1' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps>
                                <Phase><Trace>1st phase</Trace></Phase>
                                <Phase><Trace>2nd phase</Trace></Phase>
                                <Phase><Trace>3nd phase</Trace></Phase>
                            </Steps>
                          </Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            var manifest = (Manifest)manifestAcc.InvokeStatic("Parse", xml, true);

            var executionResults = new List<ExecutionResult>();
            var console = new StringWriter();
            for (int phase = 0; phase < manifest.CountOfPhases; phase++)
            {
                var executionContext = new ExecutionContext("packagePath", "targetPath", new string[0], "sandboxPath", manifest, phase, manifest.CountOfPhases, console);
                var pkgManAcc = new PrivateType(typeof(PackageManager));
                try
                {
                    var result = (PackagingResult)pkgManAcc.InvokeStatic("ExecuteCurrentPhase", manifest, executionContext);
                }
                catch { break; } // intentionally suppressed

                RepositoryVersionInfo.Reset(); // must refresh
                verInfo = RepositoryVersionInfo.Instance;
                executionResults.Add(verInfo.InstalledPackages.Where(p => p.AppId == appIdStr).OrderBy(p => p.Id).Last().ExecutionResult);
            }
            Assert.AreEqual("Unfinished, Unfinished, Successful", String.Join(", ", executionResults));

            var log = _log.ToString();

            var appInfo = verInfo.Applications.Where(a => a.AppId == appIdStr).First();

            Assert.AreEqual(Version.Parse("1.2"), appInfo.Version);
            Assert.AreEqual(Version.Parse("1.2"), appInfo.AcceptableVersion);

            var actual = String.Join(", ", verInfo.InstalledPackages
                .Where(p => p.AppId == appIdStr)
                .OrderBy(p => p.ApplicationVersion)
                .Select(p => String.Format("{0}: {1}", p.ApplicationVersion, p.ExecutionResult)));
            Assert.AreEqual("1.0: Successful, 1.1: Successful, 1.2: Unfinished, 1.2: Successful", actual);
        }
        [TestMethod]
        public void Packaging_App_UnfinishedResultOnSuccessful()
        {
            var appId = Guid.NewGuid();
            var appIdStr = appId.ToString();
            var appName = "Packaging_App_UnfinishedResult";
            string ed = null;
            EnsureApplicationPackage(appName, appId, ed, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            EnsureApplicationPackage(appName, appId, ed, PackageLevel.Patch, new Version(1, 1), ExecutionResult.Successful);

            var verInfo = RepositoryVersionInfo.Instance;

            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Patch'>
                            <AppId>" + appIdStr + @"</AppId>
                            <Name>" + appName + @"</Name>
                            <VersionControl target='1.2' expected='1.1' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps>
                                <Phase><Trace>1st phase</Trace></Phase>
                                <Phase><Trace>2nd phase</Trace></Phase>
                                <Phase><Trace>3nd phase</Trace></Phase>
                            </Steps>
                          </Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            var manifest = (Manifest)manifestAcc.InvokeStatic("Parse", xml, true);

            var executionResults = new List<ExecutionResult>();
            var console = new StringWriter();
            for (int phase = 0; phase < manifest.CountOfPhases; phase++)
            {
                var executionContext = new ExecutionContext("packagePath", "targetPath", new string[0], "sandboxPath", manifest, phase, manifest.CountOfPhases, console);
                var pkgManAcc = new PrivateType(typeof(PackageManager));
                try
                {
                    var result = (PackagingResult)pkgManAcc.InvokeStatic("ExecuteCurrentPhase", manifest, executionContext);
                }
                catch { break; } // intentionally suppressed

                RepositoryVersionInfo.Reset(); // must refresh
                verInfo = RepositoryVersionInfo.Instance;
                executionResults.Add(verInfo.InstalledPackages.Where(p => p.AppId == appIdStr).OrderBy(p => p.Id).Last().ExecutionResult);
            }
            Assert.AreEqual("Unfinished, Unfinished, Successful", String.Join(", ", executionResults));

            var log = _log.ToString();

            var appInfo = verInfo.Applications.Where(a => a.AppId == appIdStr).First();

            Assert.AreEqual(Version.Parse("1.2"), appInfo.Version);
            Assert.AreEqual(Version.Parse("1.2"), appInfo.AcceptableVersion);

            var actual = String.Join(", ", verInfo.InstalledPackages
                .Where(p => p.AppId == appIdStr)
                .OrderBy(p => p.ApplicationVersion)
                .Select(p => String.Format("{0}: {1}", p.ApplicationVersion, p.ExecutionResult)));
            Assert.AreEqual("1.0: Successful, 1.1: Successful, 1.2: Successful", actual);
        }

        [TestMethod]
        public void Packaging_App_WrongStep_FaultyResult()
        {
            var appId = Guid.NewGuid();
            var appIdStr = appId.ToString();
            var appName = "Packaging_App_WrongStep_FaultyResult";
            string ed = null;
            EnsureApplicationPackage(appName, appId, ed, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            EnsureApplicationPackage(appName, appId, ed, PackageLevel.Patch, new Version(1, 1), ExecutionResult.Successful);
            EnsureApplicationPackage(appName, appId, ed, PackageLevel.Patch, new Version(1, 2), ExecutionResult.Successful);

            var verInfo = RepositoryVersionInfo.Instance; // must refresh

            var description = "Wrong step that throws an exception.";
            var forbiddenText = "#### Not executed phase ####";
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Patch'>
                            <AppId>" + appIdStr + @"</AppId>
                            <Name>" + appName + @"</Name>
                            <Description>" + description + @"</Description>
                            <VersionControl target='1.3' expected='1.2' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps>
                                <Phase><WrongStepThatThrowsAnException /></Phase>
                                <Phase><Trace>" + forbiddenText + @"</Trace></Phase>
                            </Steps>
                          </Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            var manifest = (Manifest)manifestAcc.InvokeStatic("Parse", xml, true);

            var console = new StringWriter();
            for (int phase = 0; phase < manifest.CountOfPhases; phase++)
            {
                var executionContext = new ExecutionContext("packagePath", "targetPath", new string[0], "sandboxPath", manifest, phase, manifest.CountOfPhases, console);
                var pkgManAcc = new PrivateType(typeof(PackageManager));
                try
                {
                    var result = (PackagingResult)pkgManAcc.InvokeStatic("ExecuteCurrentPhase", manifest, executionContext);
                }
                catch { break; } // intentionally suppressed
            }

            var log = _log.ToString();
            Assert.IsFalse(log.Contains(forbiddenText));

            verInfo = RepositoryVersionInfo.Instance; // must refresh
            var appInfo = verInfo.Applications.Where(a => a.AppId == appIdStr).First();

            Assert.AreEqual(Version.Parse("1.3"), appInfo.Version);
            Assert.AreEqual(Version.Parse("1.2"), appInfo.AcceptableVersion);

            var actual = String.Join(", ", verInfo.InstalledPackages
                .Where(p => p.AppId == appIdStr)
                .OrderBy(p => p.ApplicationVersion)
                .Select(p => String.Format("{0}: {1}", p.ApplicationVersion, p.ExecutionResult)));
            Assert.AreEqual("1.0: Successful, 1.1: Successful, 1.2: Successful, 1.3: Faulty", actual);
        }

        [TestMethod]
        public void Packaging_Prod_Parsing2ndIfLastIsWrong()
        {
            var appId = Guid.Empty;
            var appName = "Packaging_Prod_Parsing2ndIfLastIsWrong";
            EnsureProductPackage("Packaging_Prod_Parsing2ndIfLastIsWrong", null, PackageLevel.Patch, new Version(6, 4), ExecutionResult.Successful);
            EnsureProductPackage("Packaging_Prod_Parsing2ndIfLastIsWrong", null, PackageLevel.Patch, new Version(6, 5), ExecutionResult.Successful);
            EnsureProductPackage("Packaging_Prod_Parsing2ndIfLastIsWrong", null, PackageLevel.Patch, new Version(6, 6), ExecutionResult.Faulty);

            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Product' level='Patch'>
                            <Name>" + appName + @"</Name>
                            <Edition>Unofficial</Edition>
                            <VersionControl target='6.6' expectedMin='6.5' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            var manifest = (Manifest)manifestAcc.InvokeStatic("Parse", xml, true);
        }
        [TestMethod]
        public void Packaging_App_Parsing2ndIfLastIsWrong()
        {
            var appId = Guid.NewGuid();
            var appName = "Packaging_App_Parsing2ndIfLastIsWrong";
            EnsureApplicationPackage("Packaging_App_Versioning_ExpectedMinMax", appId, null, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            EnsureApplicationPackage("Packaging_App_Versioning_ExpectedMinMax", appId, null, PackageLevel.Patch, new Version(1, 1), ExecutionResult.Successful);
            EnsureApplicationPackage("Packaging_App_Versioning_ExpectedMinMax", appId, null, PackageLevel.Patch, new Version(1, 2), ExecutionResult.Faulty);

            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Patch'>
                            <Name>" + appName + @"</Name>
                            <AppId>" + appId + @"</AppId>
                            <VersionControl target='1.2' expectedMin='1.1' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            var manifest = (Manifest)manifestAcc.InvokeStatic("Parse", xml, true);
        }

        [TestMethod]
        public void Packaging_Prod_Executing2ndIfLastIsWrong()
        {
            var appId = Guid.Empty;
            var appName = "Packaging_Prod_Parsing2ndIfLastIsWrong";
            EnsureProductPackage("Packaging_Prod_Parsing2ndIfLastIsWrong", null, PackageLevel.Patch, new Version(6, 4), ExecutionResult.Successful);
            EnsureProductPackage("Packaging_Prod_Parsing2ndIfLastIsWrong", null, PackageLevel.Patch, new Version(6, 5), ExecutionResult.Successful);
            EnsureProductPackage("Packaging_Prod_Parsing2ndIfLastIsWrong", null, PackageLevel.Patch, new Version(6, 6), ExecutionResult.Faulty);

            var verInfo = RepositoryVersionInfo.Instance; // must refresh
            Assert.AreEqual(Version.Parse("6.6"), verInfo.OfficialSenseNetVersion.Version);

            var description = "Patch of patches.";
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Product' level='Patch'>
                            <Name>" + appName + @"</Name>
                            <Edition>Unofficial</Edition>
                            <Description>" + description + @"</Description>
                            <VersionControl target='6.6' expectedMin='6.5' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            var manifest = (Manifest)manifestAcc.InvokeStatic("Parse", xml, true);

            var console = new StringWriter();
            for (int phase = 0; phase < manifest.CountOfPhases; phase++)
            {
                var executionContext = new ExecutionContext("packagePath", "targetPath", new string[0], "sandboxPath", manifest, phase, manifest.CountOfPhases, console);
                var pkgManAcc = new PrivateType(typeof(PackageManager));
                var result = (PackagingResult)pkgManAcc.InvokeStatic("ExecuteCurrentPhase", manifest, executionContext);
            }

            var log = _log.ToString();
            Assert.IsTrue(log.Contains("Tool is running."));
            Assert.IsTrue(log.Contains("Errors: 0"));

            verInfo = RepositoryVersionInfo.Instance; // must refresh
            var appInfo = verInfo.OfficialSenseNetVersion;

            Assert.AreEqual(Version.Parse("6.6"), appInfo.Version);
            Assert.AreEqual(Version.Parse("6.6"), appInfo.AcceptableVersion);
            Assert.IsTrue(verInfo.InstalledPackages.Any(x => x.ExecutionResult == ExecutionResult.Faulty));

            var actual = String.Join(", ", verInfo.InstalledPackages
                .Where(p => p.AppId == null)
                .OrderBy(p => p.SenseNetVersion)
                .Select(p => String.Format("{0}: {1}", p.SenseNetVersion, p.ExecutionResult)));
            Assert.AreEqual("6.3.1: Successful, 6.4: Successful, 6.5: Successful, 6.6: Faulty, 6.6: Successful", actual);
        }
        [TestMethod]
        public void Packaging_App_Executing2ndIfLastIsWrong()
        {
            var appId = Guid.NewGuid();
            var appIdStr = appId.ToString();
            var appName = "Packaging_App_Executing2ndIfLastIsWrong";
            string ed = null;
            EnsureApplicationPackage(appName, appId, ed, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            EnsureApplicationPackage(appName, appId, ed, PackageLevel.Patch, new Version(1, 1), ExecutionResult.Successful);
            EnsureApplicationPackage(appName, appId, ed, PackageLevel.Patch, new Version(1, 2), ExecutionResult.Successful);
            EnsureApplicationPackage(appName, appId, ed, PackageLevel.Patch, new Version(1, 3), ExecutionResult.Faulty);

            var verInfo = RepositoryVersionInfo.Instance; // must refresh

            var description = "Patch of patches.";
            var xml = new XmlDocument();
            xml.LoadXml(@"<Package type='Application' level='Patch'>
                            <AppId>" + appIdStr + @"</AppId>
                            <Name>" + appName + @"</Name>
                            <Description>" + description + @"</Description>
                            <VersionControl target='1.3' expectedMin='1.2' />
                            <ReleaseDate>2014-04-01</ReleaseDate>
                            <Steps><Trace>Tool is running.</Trace></Steps></Package>");

            var manifestAcc = new PrivateType(typeof(Manifest));
            var manifest = (Manifest)manifestAcc.InvokeStatic("Parse", xml, true);

            var console = new StringWriter();
            for (int phase = 0; phase < manifest.CountOfPhases; phase++)
            {
                var executionContext = new ExecutionContext("packagePath", "targetPath", new string[0], "sandboxPath", manifest, phase, manifest.CountOfPhases, console);
                var pkgManAcc = new PrivateType(typeof(PackageManager));
                var result = (PackagingResult)pkgManAcc.InvokeStatic("ExecuteCurrentPhase", manifest, executionContext);
            }

            var log = _log.ToString();
            Assert.IsTrue(log.Contains("Tool is running."));
            Assert.IsTrue(log.Contains("Errors: 0"));

            verInfo = RepositoryVersionInfo.Instance; // must refresh
            var appInfo = verInfo.Applications.Where(a => a.AppId == appIdStr).First();

            Assert.AreEqual(Version.Parse("1.3"), appInfo.Version);
            Assert.AreEqual(Version.Parse("1.3"), appInfo.AcceptableVersion);
            Assert.IsTrue(verInfo.InstalledPackages.Any(x => x.ExecutionResult == ExecutionResult.Faulty));

            var actual = String.Join(", ", verInfo.InstalledPackages
                .Where(p => p.AppId == appIdStr)
                .OrderBy(p => p.ApplicationVersion)
                .Select(p => String.Format("{0}: {1}", p.ApplicationVersion, p.ExecutionResult)));
            Assert.AreEqual("1.0: Successful, 1.1: Successful, 1.2: Successful, 1.3: Faulty, 1.3: Successful", actual);
        }

        /*===========================================================================================*/


        [TestMethod]
        public void Packaging_RightAppInfo()
        {
            var app1Id = Guid.NewGuid();
            var app2Id = Guid.NewGuid();
            var app3Id = Guid.NewGuid();
            var app1IdStr = app1Id.ToString();
            var app2IdStr = app2Id.ToString();
            var app3IdStr = app3Id.ToString();
            var prodName = "Sense/Net ECM";
            var app1Name = "App1";
            var app2Name = "App2";
            var app3Name = "App3";
            string edition = null;

            EnsureProductPackage(prodName, null, PackageLevel.Patch, new Version(6, 4), ExecutionResult.Successful);
            EnsureProductPackage(prodName, null, PackageLevel.Patch, new Version(6, 5), ExecutionResult.Faulty);
            EnsureProductPackage(prodName, null, PackageLevel.Patch, new Version(6, 5), ExecutionResult.Faulty);
            EnsureProductPackage(prodName, null, PackageLevel.Patch, new Version(6, 5), ExecutionResult.Faulty);
            EnsureProductPackage(prodName, null, PackageLevel.Patch, new Version(6, 5), ExecutionResult.Successful);
            EnsureProductPackage(prodName, null, PackageLevel.Patch, new Version(6, 5, 1), ExecutionResult.Unfinished);
            EnsureApplicationPackage(app1Name, app1Id, edition, PackageLevel.Install, new Version(1, 0), ExecutionResult.Faulty);
            EnsureApplicationPackage(app1Name, app1Id, edition, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            EnsureApplicationPackage(app1Name, app1Id, edition, PackageLevel.Patch, new Version(1, 1), ExecutionResult.Faulty);
            EnsureApplicationPackage(app1Name, app1Id, edition, PackageLevel.Patch, new Version(1, 1), ExecutionResult.Faulty);
            EnsureApplicationPackage(app1Name, app1Id, edition, PackageLevel.Patch, new Version(1, 1), ExecutionResult.Successful);
            EnsureApplicationPackage(app1Name, app1Id, edition, PackageLevel.Patch, new Version(1, 2), ExecutionResult.Faulty);
            EnsureApplicationPackage(app1Name, app1Id, edition, PackageLevel.Patch, new Version(1, 2), ExecutionResult.Unfinished);
            EnsureApplicationPackage(app1Name, app1Id, edition, PackageLevel.Patch, new Version(1, 2), ExecutionResult.Faulty);
            EnsureApplicationPackage(app2Name, app2Id, edition, PackageLevel.Install, new Version(1, 0), ExecutionResult.Successful);
            EnsureApplicationPackage(app2Name, app2Id, edition, PackageLevel.Patch, new Version(1, 1), ExecutionResult.Successful);
            EnsureApplicationPackage(app2Name, app2Id, edition, PackageLevel.Patch, new Version(1, 2), ExecutionResult.Unfinished);
            EnsureApplicationPackage(app2Name, app2Id, edition, PackageLevel.Patch, new Version(1, 2), ExecutionResult.Faulty);
            EnsureApplicationPackage(app2Name, app2Id, edition, PackageLevel.Patch, new Version(1, 2), ExecutionResult.Successful);
            EnsureApplicationPackage(app2Name, app2Id, edition, PackageLevel.Patch, new Version(1, 3), ExecutionResult.Successful);
            EnsureApplicationPackage(app3Name, app3Id, edition, PackageLevel.Install, new Version(1, 0), ExecutionResult.Faulty);

            var verInfo = RepositoryVersionInfo.Instance; // must refresh

            var app1Info = verInfo.Applications.Where(a => a.AppId == app1IdStr).FirstOrDefault();
            var app2Info = verInfo.Applications.Where(a => a.AppId == app2IdStr).FirstOrDefault();
            var app3Info = verInfo.Applications.Where(a => a.AppId == app3IdStr).FirstOrDefault();

            //Assert.AreEqual(Version.Parse("1.3"), appInfo.Version);
            //Assert.AreEqual(Version.Parse("1.3"), appInfo.AcceptableVersion);
            //Assert.IsFalse(verInfo.InstalledPackages.Any(x => x.ExecutionResult == ExecutionResult.Faulty));

            //var actual = String.Join(", ", verInfo.InstalledPackages
            //    .Where(p => p.AppId == appIdStr)
            //    .OrderBy(p => p.ApplicationVersion)
            //    .Select(p => String.Format("{0}: {1}", p.ApplicationVersion, p.ExecutionResult)));
            //Assert.AreEqual("1.0: Successful, 1.1: Successful, 1.2: Successful, 1.3: Compensated", actual);
        }

    }
}
