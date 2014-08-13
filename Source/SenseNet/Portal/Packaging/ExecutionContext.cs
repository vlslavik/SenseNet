using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SenseNet.Packaging
{
    /// <summary>Contains package information for executing a step.</summary>
    public class ExecutionContext
    {
        private Dictionary<string, object> _variables = new Dictionary<string, object>();
        /// <summary>Returns a named value that was memorized in the current phase.</summary>
        public object GetVariable(string name)
        {
            object result;
            if (_variables.TryGetValue(name, out result))
                return result;
            return null;
        }
        /// <summary>Memorize a named value at the end of the current phase.</summary>
        public void SetVariable(string name, object value)
        {
            _variables[name] = value;
        }

        /// <summary>Fully qualified path of the executing extracted package.</summary>
        public string PackagePath { get; private set; }
        /// <summary>Fully qualified path of the executing extracted package.</summary>
        public string TargetPath { get; private set; }
        /// <summary>UNC paths of the related network server web directories.</summary>
        public string[] NetworkTargets { get; private set; }
        /// <summary>Fully qualified path of the phase executor assemblies directory.</summary>
        public string SandboxPath { get; private set; }
        /// <summary>Parsed manifest.</summary>
        public Manifest Manifest { get; private set; }
        /// <summary>Zero based index of the executing phase.</summary>
        public int CurrentPhase { get; private set; }
        /// <summary>Phase count of the currently executed package.</summary>
        public int CountOfPhases { get; private set; }
        /// <summary>Console out of the executing SnAdmin. Write here any information that you do not want to log.</summary>
        public TextWriter Console { get; private set; }

        internal ExecutionContext(string packagePath, string targetPath, string[] networkTargets, string sandboxPath, Manifest manifest, int currentPhase, int countOfPhases, TextWriter console)
        {
            this.PackagePath = packagePath;
            this.TargetPath = targetPath;
            this.NetworkTargets = networkTargets;
            this.SandboxPath = sandboxPath;
            this.Manifest = manifest;
            this.CurrentPhase = currentPhase;
            this.CountOfPhases = countOfPhases;
            this.Console = console;
        }

        /// <summary>True if the StartRepository step has already executed.</summary>
        public bool RepositoryStarted { get; internal set; }
    }
}
