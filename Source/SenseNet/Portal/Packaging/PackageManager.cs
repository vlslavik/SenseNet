using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Diagnostics;
using SenseNet.ContentRepository;
using System.Reflection;
using System.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.Packaging
{
    public class PackageManager
    {
        public const string SANDBOXDIRECTORYNAME = "run";

        public static PackagingResult Execute(string packagePath, string targetPath, int phase, string[] parameters, TextWriter console)
        {
            var phaseCount = 1;

            var files = Directory.GetFiles(packagePath);

            Manifest manifest = null;
            Exception manifestParsingException = null;
            if (files.Length == 1)
            {
                try
                {
                    manifest = Manifest.Parse(files[0], phase == 0);
                    phaseCount = manifest.CountOfPhases;
                }
                catch (Exception e)
                {
                    manifestParsingException = e;
                }
            }

            if (files.Length == 0)
                throw new InvalidPackageException(SR.Errors.ManifestNotFound);
            if (files.Length > 1)
                throw new InvalidPackageException(SR.Errors.PackageCanContainOnlyOneFileInTheRoot);
            if (manifestParsingException != null)
                throw new PackagingException("Manifest parsing error. See inner exception.", manifestParsingException);
            if (manifest == null)
                throw new PackagingException("Manifest was not found.");

            SubstituteParameters(manifest, parameters);
            var sandboxPath = Path.Combine(Path.GetDirectoryName(packagePath), SANDBOXDIRECTORYNAME);

            Logger.LogTitle(String.Format("Executing phase {0}/{1}", phase + 1, phaseCount));

            var configEntry = ConfigurationManager.AppSettings["NetworkTargets"];
            var networkTargets = string.IsNullOrEmpty(configEntry) ? new string[0] : configEntry.Split(',', ';').Select(x => x.Trim()).ToArray();

            var executionContext = new ExecutionContext(packagePath, targetPath, networkTargets, sandboxPath, manifest, phase, manifest.CountOfPhases, console);
            var result = ExecuteCurrentPhase(manifest, executionContext);

            if (Repository.Started())
            {
                console.WriteLine("-------------------------------------------------------------");
                console.Write("Stopping repository ... ");
                Repository.Shutdown();
                console.WriteLine("Ok.");
            }

            return result;
        }

        private static void SubstituteParameters(Manifest manifest, string[] parameters)
        {
            foreach (var parameter in parameters)
            {
                var prm = StepParameter.Parse(parameter);
                if (prm.PhaseIndex < 0 || prm.PhaseIndex > manifest.CountOfPhases - 1)
                    throw new InvalidStepParameterException("Invalid phase number: " + prm.PhaseIndex + 1);
                var phase = manifest.GetPhase(prm.PhaseIndex);
                if (prm.StepIndex < 0 || prm.StepIndex > phase.Length - 1)
                    throw new InvalidStepParameterException(String.Format("Invalid step number: {1} (phase: {0})", prm.PhaseIndex + 1, prm.StepIndex + 1));
                var step = phase[prm.StepIndex];
                step.SetProperty(prm.PropertyName, prm.Value);
            }
        }


        private static PackagingResult ExecuteCurrentPhase(Manifest manifest, ExecutionContext executionContext)
        {
            if (executionContext.CurrentPhase == 0)
                SaveInitialPackage(manifest);

            var steps = manifest.GetPhase(executionContext.CurrentPhase);

            var stopper = Stopwatch.StartNew();
            Logger.LogMessage("Executing steps");

            Exception phaseException = null;
            var successful = false;
            try
            {
                var maxStepId = steps.Count();
                foreach (var step in steps)
                {
                    var stepStopper = Stopwatch.StartNew();
                    Logger.LogStep(step, maxStepId);
                    step.Execute(executionContext);
                    stepStopper.Stop();
                    Logger.LogMessage("-------------------------------------------------------------");
                    Logger.LogMessage("Time: " + stepStopper.Elapsed);
                }
                stopper.Stop();
                Logger.LogMessage("=============================================================");
                Logger.LogMessage("All steps were executed.");
                Logger.LogMessage("Aggregated time: " + stopper.Elapsed);
                Logger.LogMessage("Errors: " + Logger.Errors);
                successful = true;
            }
            catch (Exception e)
            {
                phaseException = e;
            }

            if (successful && (executionContext.CurrentPhase < manifest.CountOfPhases - 1))
                return new PackagingResult { NeedRestart = true, Successful = true, Errors = Logger.Errors };

            try
            {
                if (Logger.Level <= LogLevel.Default)
                    SavePackage(manifest, executionContext, successful, phaseException);
            }
            finally
            {
                RepositoryVersionInfo.Reset();

                //we need to shut down messaging, because the line above uses it
                DistributedApplication.ClusterChannel.ShutDown();
            }
            if (!successful)
                throw new ApplicationException(String.Format(SR.Errors.PhaseFinishedWithError_1, phaseException.Message), phaseException); 

            return new PackagingResult { NeedRestart = false, Successful = true, Errors = Logger.Errors };
        }

        private static void SaveInitialPackage(Manifest manifest)
        {
            var newPack = CreatePackage(manifest, ExecutionResult.Unfinished, null);
            DataProvider.Current.SavePackage(newPack);
        }
        private static void SavePackage(Manifest manifest, ExecutionContext executionContext, bool successful, Exception execError)
        {
            var executionResult = successful ? ExecutionResult.Successful : ExecutionResult.Faulty;
            var isAppPack = manifest.Type== PackageType.Application;

            RepositoryVersionInfo.Reset();
            var oldPacks = isAppPack
                ? RepositoryVersionInfo.Instance.InstalledPackages
                    .Where(p => p.AppId == manifest.AppId && p.ApplicationVersion == manifest.VersionControl.Target)
                    .OrderBy(p => p.ExecutionDate).ToList()
                : RepositoryVersionInfo.Instance.InstalledPackages
                    .Where(p => p.AppId == manifest.AppId && p.SenseNetVersion == manifest.VersionControl.Target)
                    .OrderBy(p => p.ExecutionDate).ToList();

            var oldPack = oldPacks.LastOrDefault();
            if (oldPack == null)
            {
                var newPack = CreatePackage(manifest, executionResult, execError);
                DataProvider.Current.SavePackage(newPack);
            }
            else
            {
                UpdatePackage(oldPack, manifest, executionResult, execError);
                DataProvider.Current.UpdatePackage(oldPack);
            }
        }
        private static Package CreatePackage(Manifest manifest, ExecutionResult result, Exception execError)
        {
            var snInfo = RepositoryVersionInfo.Instance.OfficialSenseNetVersion;
            var prodVer = (snInfo == null) ? new Version(42, 42, 42, 42) : snInfo.Version;

            Version appVer = null;

            if (manifest.Level != ContentRepository.Storage.PackageLevel.Tool)
            {
                if (manifest.Type == ContentRepository.Storage.PackageType.Product)
                    prodVer = manifest.VersionControl.Target;
                else
                    appVer = manifest.VersionControl.Target;
            }

            return new ContentRepository.Storage.Package
            {
                Name = manifest.Name,
                Edition = manifest.Edition,
                Description = manifest.Description,
                ReleaseDate = manifest.ReleaseDate,
                PackageLevel = manifest.Level,
                PackageType = manifest.Type,
                AppId = manifest.AppId,
                ExecutionDate = DateTime.UtcNow,
                ExecutionResult = result,
                ApplicationVersion = appVer,
                SenseNetVersion = prodVer,
                ExecutionError = execError
            };
        }
        private static void UpdatePackage(Package package, Manifest manifest, ExecutionResult result, Exception execError)
        {
            var snInfo = RepositoryVersionInfo.Instance.OfficialSenseNetVersion;
            var prodVer = (snInfo == null) ? new Version(42, 42, 42, 42) : snInfo.Version;

            Version appVer = null;

            if (manifest.Level != ContentRepository.Storage.PackageLevel.Tool)
            {
                if (manifest.Type == ContentRepository.Storage.PackageType.Product)
                    prodVer = manifest.VersionControl.Target;
                else
                    appVer = manifest.VersionControl.Target;
            }

            package.Name = manifest.Name;
            package.Edition = manifest.Edition;
            package.Description = manifest.Description;
            package.ReleaseDate = manifest.ReleaseDate;
            package.PackageLevel = manifest.Level;
            package.PackageType = manifest.Type;
            package.AppId = manifest.AppId;
            package.ExecutionDate = DateTime.UtcNow;
            package.ExecutionResult = result;
            package.ExecutionError = execError;
            package.ApplicationVersion = appVer;
            package.SenseNetVersion = prodVer;
        }

        public static string GetHelp()
        {
            var memory = new List<string>();

            var sb = new StringBuilder();
            sb.AppendLine("Available step types and parameters");
            sb.AppendLine("-----------------------------------");
            foreach (var item in SenseNet.Packaging.Steps.Step.StepTypes)
            {
                var stepType = item.Value;
                if (memory.Contains(stepType.FullName))
                    continue;
                memory.Add(stepType.FullName);

                var step = (SenseNet.Packaging.Steps.Step)Activator.CreateInstance(stepType);
                sb.AppendLine(step.ElementName + " (" + stepType.FullName + ")");
                foreach (var property in stepType.GetProperties())
                {
                    if (property.Name == "StepId" || property.Name == "ElementName")
                        continue;
                    var isDefault = property.GetCustomAttributes(true).Any(x => x is SenseNet.Packaging.Steps.DefaultPropertyAttribute);
                    sb.AppendFormat("  {0} : {1} {2}", property.Name, property.PropertyType.Name, isDefault ? "(Default)" : "");
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }
    }
}
