using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using SenseNet.Packaging.Steps;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository;

namespace SenseNet.Packaging
{
    public class Manifest
    {
        public PackageLevel Level { get; private set; }
        public PackageType Type { get; private set; }
        public string Name { get; private set; }
        public string Edition { get; private set; }
        public string AppId { get; private set; }
        public string Description { get; private set; }
        public DateTime ReleaseDate { get; private set; }
        public VersionControl VersionControl { get; private set; }

        private List<List<Step>> _phases;
        public int CountOfPhases { get { return _phases.Count; } }

        internal static Manifest Parse(string path, bool log)
        {
            var xml = new XmlDocument();
            try
            {
                xml.Load(path);
            }
            catch (Exception e)
            {
                throw new PackagingException("Manifest parse error", e);
            }
            return Parse(xml, log);
        }
        /// <summary>
        /// Test entry
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        private static Manifest Parse(XmlDocument xml, bool log)
        {
            var manifest = new Manifest();

            ParseHead(xml, manifest);
            manifest.CheckPrerequisits(log);
            ParseSteps(xml, manifest);

            return manifest;
        }

        private static void ParseHead(XmlDocument xml, Manifest manifest)
        {
            XmlElement e;
            XmlAttribute attr;

            // root element inspection (required element name)
            e = xml.DocumentElement;
            if(e.Name != "Package")
                throw new InvalidPackageException(SR.Errors.Manifest.WrongRootName);

            // parsing type (required, product or application)
            attr = e.Attributes["type"];
            if (attr == null)
                attr = e.Attributes["Type"];
            if (attr == null)
                throw new InvalidPackageException(SR.Errors.Manifest.MissingType);
            PackageType type;
            if (!Enum.TryParse<PackageType>(attr.Value, true, out type))
                throw new InvalidPackageException(SR.Errors.Manifest.InvalidType);
            manifest.Type = type;

            // parsing level (required, one of the tool, patch, servicepack or upgrade)
            attr = e.Attributes["level"];
            if (attr == null)
                attr = e.Attributes["Level"];
            if (attr == null)
                throw new InvalidPackageException(SR.Errors.Manifest.MissingLevel);
            PackageLevel level;
            if(!Enum.TryParse<PackageLevel>(attr.Value, true, out level))
                throw new InvalidPackageException(SR.Errors.Manifest.InvalidLevel);
            manifest.Level = level;

            // parsing application name (required if the "type" is "application")
            e = (XmlElement)xml.DocumentElement.SelectSingleNode("AppId");
            if (e != null)
            {
                if (e.InnerText.Length == 0)
                    throw new InvalidPackageException(SR.Errors.Manifest.InvalidAppId);
                else
                    manifest.AppId = e.InnerText;
            }
            else
            {
                if(type == PackageType.Application)
                    throw new InvalidPackageException(SR.Errors.Manifest.MissingAppId);
            }

            // parsing name (required)
            e = (XmlElement)xml.DocumentElement.SelectSingleNode("Name");
            if (e == null)
                throw new InvalidPackageException(SR.Errors.Manifest.MissingName);
            manifest.Name = e.InnerText;
            if (String.IsNullOrEmpty(manifest.Name))
                throw new InvalidPackageException(SR.Errors.Manifest.InvalidName);

            // parsing description (optional)
            e = (XmlElement)xml.DocumentElement.SelectSingleNode("Description");
            if (e != null)
                manifest.Description = e.InnerText;

            // parsing edition (optional)
            e = (XmlElement)xml.DocumentElement.SelectSingleNode("Edition");
            if (e != null)
                manifest.Edition = e.InnerText;

            // parsing version control
            e = (XmlElement)xml.DocumentElement.SelectSingleNode("VersionControl");
            if (level != PackageLevel.Tool && e == null)
                throw new InvalidPackageException(SR.Errors.Manifest.MissingVersionControl);
            manifest.VersionControl = VersionControl.Initialize(e, level, type);

            // parsing release date (required)
            e = (XmlElement)xml.DocumentElement.SelectSingleNode("ReleaseDate");
            if (e == null)
                throw new InvalidPackageException(SR.Errors.Manifest.MissingReleaseDate);
            DateTime releaseDate;
            if (!DateTime.TryParse(e.InnerText, out releaseDate))
                throw new InvalidPackageException(SR.Errors.Manifest.InvalidReleaseDate);
            if(releaseDate > DateTime.UtcNow)
                throw new InvalidPackageException(SR.Errors.Manifest.InvalidReleaseDate);
            manifest.ReleaseDate = releaseDate;
        }

        private static void ParseSteps(XmlDocument xml, Manifest manifest)
        {
            var stepsElement = (XmlElement)xml.DocumentElement.SelectSingleNode("Steps");
            var phases = new List<List<Step>>();
            if (stepsElement != null)
            {
                var explicitPhases = stepsElement.SelectNodes("Phase");
                if (explicitPhases.Count == 0)
                    phases.Add(ParsePhase(stepsElement));
                else
                    foreach (XmlElement phaseElement in explicitPhases)
                        phases.Add(ParsePhase(phaseElement));
            }
            if (phases.Count == 0)
                phases.Add(new List<Step>());
            manifest._phases = phases;
        }
        private static List<Step> ParsePhase(XmlElement phaseElement)
        {
            var steps = new List<Step>();
            var index = 1;
            foreach (XmlElement stepElement in phaseElement.SelectNodes("*"))
                steps.Add(ParseStep(stepElement, index++));
            return steps;
        }
        private static Step ParseStep(XmlElement stepElement, int index)
        {
            var parameters = new Dictionary<string, string>();

            // attribute model
            foreach (XmlAttribute attr in stepElement.Attributes)
                parameters.Add(ToPascalCase(attr.Name), attr.Value);

            var children = stepElement.SelectNodes("*");
            if (children.Count == 0 && stepElement.InnerXml != null && stepElement.InnerXml.Trim().Length > 0)
            {
                // default property model
                parameters.Add("", stepElement.InnerText);
            }
            else
            {
                // element model
                foreach (XmlElement childElement in children)
                {
                    var name = childElement.Name;
                    if (parameters.ContainsKey(name))
                        throw new InvalidPackageException(String.Format(SR.Errors.StepParsing.AttributeAndElementNameCollision_2, stepElement.Name, name));
                    parameters.Add(name, childElement.InnerXml);
                }
            }

            return Step.BuildStep(index, stepElement.Name, parameters);
        }

        internal static string ToPascalCase(string propertyName)
        {
            if (Char.IsLower(propertyName[0]))
            {
                var rewrittenName = Char.ToUpper(propertyName[0]).ToString();
                if (propertyName.Length > 1)
                    rewrittenName += propertyName.Substring(1);
                propertyName = rewrittenName;
            }
            return propertyName;
        }

        public Step[] GetPhase(int index)
        {
            if (index < 0 || index > _phases.Count)
                throw new PackagingException(String.Format(SR.Errors.InvalidPhaseIndex_2, _phases.Count, index));
            return _phases[index].ToArray();
        }

        private void CheckPrerequisits(bool log)
        {
            if (log)
            {
                Logger.LogMessage("Name:    " + this.Name);
                Logger.LogMessage("Edition: " + this.Edition);
                Logger.LogMessage("Type:    " + this.Type);
                Logger.LogMessage("Level:   " + this.Level);
                if (this.Level != PackageLevel.Tool)
                    Logger.LogMessage("Package version: " + this.VersionControl.Target);
                if (this.Type == PackageType.Application)
                    Logger.LogMessage("AppId: {0}", this.AppId);
            }

            if (Level == PackageLevel.Install)
                CheckInstall(RepositoryVersionInfo.Instance, log);
            else
                CheckUpdate(RepositoryVersionInfo.Instance, log);
        }
        private void CheckInstall(RepositoryVersionInfo versionInfo, bool log)
        {
            if (versionInfo.Applications.FirstOrDefault(a => a.AppId == AppId) != null)
                throw new PackagePreconditionException(SR.Errors.Precondition.CannotInstallExistingApp);
        }
        private void CheckUpdate(RepositoryVersionInfo versionInfo, bool log)
        {
            Version current = null;
            Version min = null;
            Version max = null;
            switch (this.Type)
            {
                case PackageType.Product:
                    if (null != this.AppId)
                        throw new InvalidPackageException(SR.Errors.Manifest.UnexpectedAppId);
                    CheckEdition(versionInfo.OfficialSenseNetVersion);
                    current = versionInfo.OfficialSenseNetVersion.AcceptableVersion;
                    min = VersionControl.ExpectedProductMinimum;
                    max = VersionControl.ExpectedProductMaximum;
                    break;
                case PackageType.Application:
                    var existingApplication = versionInfo.Applications.FirstOrDefault(a => a.AppId == this.AppId);
                    if (existingApplication == null)
                        throw new PackagePreconditionException(SR.Errors.Precondition.AppIdDoesNotMatch);
                    CheckEdition(existingApplication);
                    current = existingApplication.AcceptableVersion;
                    min = VersionControl.ExpectedApplicationMinimum;
                    max = VersionControl.ExpectedApplicationMaximum;
                    break;
                default:
                    throw new NotImplementedException("Unknown PackageType: " + this.Type);
            }

            if (log)
            {
                Logger.LogMessage("Current version: {0}", current);
                if (min != null && min == max)
                {
                    Logger.LogMessage("Expected version: {0}", min);
                }
                else
                {
                    if (min != null)
                        Logger.LogMessage("Expected minimum version: {0}", min);
                    if (max != null)
                        Logger.LogMessage("Expected maximum version: {0}", max);
                }
            }

            if (min != null && min > current)
                throw new PackagePreconditionException(String.Format(SR.Errors.Precondition.MinimumVersion_1, this.Type.ToString().ToLower()));
            if (max != null && max < current)
                throw new PackagePreconditionException(String.Format(SR.Errors.Precondition.MaximumVersion_1, this.Type.ToString().ToLower()));

            if(Level != PackageLevel.Tool)
                if (current >= VersionControl.Target)
                    throw new PackagePreconditionException(String.Format(SR.Errors.Precondition.TargetVersionTooSmall_3, this.Type.ToString().ToLower(), VersionControl.Target, current));
        }

        private void CheckEdition(ApplicationInfo appInfo)
        {
            if (this.Edition != null && this.Edition.Length == 0)
                throw new InvalidPackageException(SR.Errors.Manifest.InvalidEdition);

            if (this.Edition == null && this.Type == PackageType.Product && this.Level == PackageLevel.Install)
                throw new InvalidPackageException(SR.Errors.Manifest.MissingEdition);

            if (this.Level != PackageLevel.Tool)
            {
                if (appInfo.AppId == null && this.Edition == null)
                    throw new InvalidPackageException(SR.Errors.Manifest.MissingEdition);
                if (appInfo.Edition != this.Edition)
                    throw new PackagePreconditionException(String.Format(SR.Errors.Precondition.EditionMismatch_2, appInfo.Edition ?? "[empty]", this.Edition ?? "[empty]"));
            }
            else
            {
                if (this.Edition != null)
                {
                    if (appInfo.Edition != this.Edition)
                        throw new PackagePreconditionException(String.Format(SR.Errors.Precondition.EditionMismatch_2, appInfo.Edition ?? "[empty]", this.Edition ?? "[empty]"));
                }
            }
        }
    }
}
