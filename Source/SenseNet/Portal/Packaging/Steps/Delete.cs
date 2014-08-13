﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage;
using System.IO;

namespace SenseNet.Packaging.Steps
{
    /// <summary>
    /// Deletes a content from the repository or a filesystem entry on the servers.
    /// </summary>
    public class Delete : Step
    {
        /// <summary>Repository or filesystem path of the content or filesystem entry to be deleted.</summary>
        [DefaultProperty]
        public string Path { get; set; }

        /// <summary>Method that called by the packaging framework.</summary>
        public override void Execute(ExecutionContext context)
        {
            // Deleting a repository content if the given path is repository path
            if (RepositoryPath.IsValidPath(Path) == RepositoryPath.PathResult.Correct)
            {
                // Deleting a repository content if it exists
                if (Node.Exists(Path))
                {
                    Logger.LogMessage("Deleting content: " + Path);
                    Node.ForceDelete(Path);
                }
                // Displaying a simple message.
                else
                {
                    Logger.LogMessage("Content was not found: " + Path);
                }
                return;
            }

            // Getting local and network paths in one array.
            var paths = ResolveAllTargets(Path, context);
            // Executing deletion on all servers.
            foreach (var path in paths)
                Execute(path);
        }

        /// <summary>Ensures that the file or directory - identified by the given absolute path - will be deleted.</summary>
        private void Execute(string path)
        {
            // Deleting the specified directory if it exists
            if (Directory.Exists(path))
            {
                Logger.LogMessage("Deleting directory: " + path);
                Directory.Delete(path, true);
                return;
            }
            // Deleting the specified file if it exists
            if (File.Exists(path))
            {
                Logger.LogMessage("Deleting file: " + path);
                File.Delete(path);
                return;
            }
            // This is not an error: displaying a simple message.
            Logger.LogMessage("Directory or file was not found: " + path);
        }
    }
}
