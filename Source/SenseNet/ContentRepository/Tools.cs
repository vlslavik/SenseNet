using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SenseNet.ContentRepository.Storage;
using System.Web;
using SenseNet.ContentRepository.Versioning;
using System.Globalization;
using System.Linq;
using SenseNet.Diagnostics;
using System.Security.Cryptography;
using SenseNet.ApplicationModel;
using System.Web.Hosting;

namespace SenseNet.ContentRepository
{
    public static class Tools
    {
        public static string GetStreamString(Stream stream)
        {
            StreamReader sr = new StreamReader(stream);
            stream.Position = 0;
            return sr.ReadToEnd();
        }
        public static Stream GetStreamFromString(string textData)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);
            writer.Write(textData);
            writer.Flush();

            return stream;
        }

        public static CultureInfo GetUICultureByNameOrDefault(string cultureName)
        {
            CultureInfo cultureInfo = null;

            if (!String.IsNullOrEmpty(cultureName))
            {
                cultureInfo = (from c in CultureInfo.GetCultures(CultureTypes.AllCultures)
                               where c.Name == cultureName
                               select c).FirstOrDefault();
            }
            if (cultureInfo == null)
                cultureInfo = CultureInfo.CurrentUICulture;

            return cultureInfo;
        }

        public static string GetVersionString(Node node)
        {
            string extraText = string.Empty;
            switch (node.Version.Status)
            {
                case VersionStatus.Pending: extraText = HttpContext.GetGlobalResourceObject("Portal", "Approving") as string; break;
                case VersionStatus.Draft: extraText = HttpContext.GetGlobalResourceObject("Portal", "Draft") as string; break;
                case VersionStatus.Locked:
                    var lockedByName = node.Lock.LockedBy == null ? "" : node.Lock.LockedBy.Name;
                    extraText = string.Concat(HttpContext.GetGlobalResourceObject("Portal", "CheckedOutBy") as string, " ", lockedByName);
                    break;
                case VersionStatus.Approved: extraText = HttpContext.GetGlobalResourceObject("Portal", "Public") as string; break;
                case VersionStatus.Rejected: extraText = HttpContext.GetGlobalResourceObject("Portal", "Reject") as string; break;
            }

            var content = node as GenericContent;
            var vmode = VersioningType.None;
            if (content != null)
                vmode = content.VersioningMode;

            if (vmode == VersioningType.None)
                return extraText;
            if (vmode == VersioningType.MajorOnly)
                return string.Concat(node.Version.Major, " ", extraText);
            return string.Concat(node.Version.Major, ".", node.Version.Minor, " ", extraText);
        }

        public static string CalculateMD5(string s)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(s);

            using (var stream = new MemoryStream(bytes))
            {
                return CalculateMD5(stream, 64 * 1024);
            }
        }

        public static string CalculateMD5(Stream stream, int bufferSize)
        {
            MD5 md5Hasher = MD5.Create();

            byte[] buffer = new byte[bufferSize];
            int readBytes;

            while ((readBytes = stream.Read(buffer, 0, bufferSize)) > 0)
            {
                md5Hasher.TransformBlock(buffer, 0, readBytes, buffer, 0);
            }

            md5Hasher.TransformFinalBlock(new byte[0], 0, 0);

            var result = md5Hasher.Hash.Aggregate(string.Empty, (full, next) => full + next.ToString("x2"));
            return result;
        }

        private static readonly char[] _availableRandomChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();

        /// <summary>
        /// Generates a random string consisting of <paramref name="length">length</paramref> number of characters, using RNGCryptoServiceProvider.
        /// </summary>
        /// <param name="length">The length of the generated string.</param>
        /// <returns>A string consisting of random characters.</returns>
        public static string GetRandomString(int length)
        {
            return GetRandomString(length, _availableRandomChars);
        }

        /// <summary>
        /// Generates a random string consisting of <paramref name="length">length</paramref> number of characters, using RNGCryptoServiceProvider.
        /// </summary>
        /// <param name="length">The length of the generated string.</param>
        /// <param name="availableCharacters">Characters that can be used in the random string.</param>
        /// <returns>A string consisting of random characters.</returns>
        public static string GetRandomString(int length, char[] availableCharacters)
        {
            if (availableCharacters == null)
                throw new ArgumentNullException("availableCharacters");
            if (availableCharacters.Length == 0)
                throw new ArgumentException("Available characters array must contain at least one character.");

            var rng = new RNGCryptoServiceProvider();
            var random = new byte[length];
            rng.GetNonZeroBytes(random);

            var buffer = new char[length];
            var characterTableLength = availableCharacters.Length;

            for (var index = 0; index < length; index++)
            {
                buffer[index] = availableCharacters[random[index] % characterTableLength];
            }

            return new string(buffer);
        }

        /// <summary>
        /// Generates a random string using RNGCryptoServiceProvider. The length of the string will be bigger
        /// than <paramref name="byteLength">byteLength</paramref> because the result bytes will be converted to string using Base64 conversion.
        /// </summary>
        /// <param name="byteLength">The length of the randomly generated byte array that will be converted to string.</param>
        /// <returns>A string consisting of random characters.</returns>
        public static string GetRandomStringBase64(int byteLength)
        {
            var randomBytes = new byte[byteLength];
            var rng = new RNGCryptoServiceProvider();
            rng.GetNonZeroBytes(randomBytes);

            return Convert.ToBase64String(randomBytes);  
        }

        /// <summary>
        /// Converts the given datetime to a datetime in UTC format. If it is already in UTC, there will be 
        /// no conversion. Undefined datetime will be considered as UTC. A duplicate of this method exists 
        /// in the Storage layer.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        internal static DateTime ConvertToUtcDateTime(DateTime dateTime)
        {
            switch (dateTime.Kind)
            {
                case DateTimeKind.Local:
                    return dateTime.ToUniversalTime();
                case DateTimeKind.Utc:
                    return dateTime;
                case DateTimeKind.Unspecified:
                    return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                default:
                    throw new InvalidOperationException("Unknown datetime kind: " + dateTime.Kind);
            }
        }

        // Structure building ==================================================================

        public static Content CreateStructure(string path)
        {
            return CreateStructure(path, "Folder");
        }

        public static Content CreateStructure(string path, string containerTypeName)
        {
            //check path validity before calling the recursive method
            if (string.IsNullOrEmpty(path))
                return null;

            RepositoryPath.CheckValidPath(path);

            return EnsureContainer(path, containerTypeName);
        }

        private static Content EnsureContainer(string path, string containerTypeName)
        {
            if (Node.Exists(path))
                return null;

            var name = RepositoryPath.GetFileName(path);
            var parentPath = RepositoryPath.GetParentPath(path);

            //recursive call to create parent containers
            EnsureContainer(parentPath, containerTypeName);

            return CreateContent(parentPath, name, containerTypeName);
        }

        private static Content CreateContent(string parentPath, string name, string typeName)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");
            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentNullException("typeName");

            var parent = Node.LoadNode(parentPath);

            if (parent == null)
                throw new ApplicationException("Parent does not exist: " + parentPath);

            //don't use admin account here, that should be 
            //done in the calling 'client' code if needed
            var content = Content.CreateNew(typeName, parent, name);
            content.Save();
            
            return content;
        }

        // Diagnostics =========================================================================

        public static string CollectExceptionMessages(Exception ex)
        {
            var sb = new StringBuilder();
            var e = ex;
            while (e != null)
            {
                sb.AppendLine(e.Message).AppendLine(e.StackTrace).AppendLine("-----------------");
                e = e.InnerException;
            }
            return sb.ToString();
        }

        //======================================================================================

        /// <summary>
        /// Checks all IFolder objects in the repository and returns all paths where AllowedChildTypes is empty. Paths are categorized by content type names.
        /// This method is allowed to call as Generic OData Application.
        /// </summary>
        /// <param name="root">Subtree to check. Null means /Root content</param>
        /// <returns>Paths where AllowedChildTypes is empty categorized by content type names.</returns>
        [ODataFunction]
        public static Dictionary<string, List<string>> CheckAllowedChildTypesOfFolders(Content root)
        {
            var result = new Dictionary<string, List<string>>();
            var rootPath = root != null ? root.Path : Repository.Root.Path;
            foreach (var node in NodeEnumerator.GetNodes(rootPath))
            {
                if (!(node is IFolder))
                    continue;

                var gc = node as GenericContent;
                if(gc==null)
                    continue;

                var t = node.NodeType.Name;
                if (t == "SystemFolder" || t == "Folder" || t == "Page")
                    continue;

                if (gc.GetAllowedChildTypeNames().Count() > 0)
                    continue;

                if (!result.ContainsKey(t))
                    result.Add(t, new List<string> { gc.Path });
                else
                    result[t].Add(gc.Path);
            }
            return result;
        }

        /// <summary>
        /// Returns a path list containing items that have explicit security entry for Everyone group but does not have explicit security entry for Visitor user.
        /// </summary>
        /// <param name="root">Examination scope.</param>
        /// <returns></returns>
        [ODataFunction]
        public static IEnumerable<string> MissingExplicitEntriesOfVisitorComparedToEveryone(Content root)
        {
            var visitorId = User.Visitor.Id;
            var everyoneId = Group.Everyone.Id;
            var result = new List<string>();
            foreach (var node in NodeEnumerator.GetNodes(root.Path))
            {
                var hasEveryoneEntry = false;
                var hasVisitorEntry = false;
                foreach (var entry in node.Security.GetExplicitEntries())
                {
                    if (entry.PrincipalId == everyoneId)
                        hasEveryoneEntry = true;
                    if (entry.PrincipalId == visitorId)
                        hasVisitorEntry = true;
                }
                if (hasEveryoneEntry && !hasVisitorEntry)
                    result.Add(node.Path);
            }
            return result;
        }
        [ODataAction]
        public static string CopyExplicitEntriesOfEveryoneToVisitor(Content root, string[] exceptList)
        {
            var visitorId = User.Visitor.Id;
            var everyoneId = Group.Everyone.Id;
            var except = exceptList.Select(p => p.ToLower()).ToList();
            foreach (var path in MissingExplicitEntriesOfVisitorComparedToEveryone(root))
            {
                if (!except.Contains(path.ToLower()))
                {
                    var node = Node.LoadNode(path);
                    var entry = node.Security.GetExplicitEntries().Where(e => e.PrincipalId == everyoneId).FirstOrDefault();
                    if (entry == null)
                        continue;
                    node.Security.SetPermissions(visitorId, entry.Propagates, entry.PermissionValues);
                }
            }
            return "Ok";
        }

        /// <summary>
        /// Goes through the files in a directory (optionally also files in subdirectories) both in the file system and the repository.
        /// Returns true if the given path was a directory, false if it wasn't.
        /// </summary>
        public static bool RecurseFilesInVirtualPath(string path, bool includesubdirs, Action<string> action, bool skipRepo = false)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (action == null)
                throw new ArgumentNullException("action");
            if (path.StartsWith("http://") || path.StartsWith("https://"))
                return false;

            var nodeHead = NodeHead.Get(path);
            var isFolder = nodeHead != null && nodeHead.GetNodeType().IsInstaceOfOrDerivedFrom("Folder");

            var fsPath = HostingEnvironment.MapPath(path);

            // Take care of folders in the repository
            if (isFolder && !skipRepo)
            {
                // Find content items
                var contents = Content.All.DisableAutofilters()
                    .Where(c => (includesubdirs ? c.InTree(nodeHead.Path) : c.InFolder(nodeHead.Path)) && c.TypeIs(typeof(File).Name))
                    .OrderBy(c => c.Index);

                // Add paths
                foreach (var c in contents)
                    action(c.Path);
            }

            // Take care of folders in the file system
            if (System.IO.Directory.Exists(fsPath))
            {
                // Add files
                foreach (var f in System.IO.Directory.GetFiles(fsPath))
                {
                    var virtualPath = f.Replace(HostingEnvironment.ApplicationPhysicalPath, HostingEnvironment.ApplicationVirtualPath).Replace(@"\", "/");
                    action(virtualPath);
                }

                // Recurse subdirectories
                if (includesubdirs)
                    foreach (var d in System.IO.Directory.GetDirectories(fsPath))
                    {
                        var virtualPath = d.Replace(HostingEnvironment.ApplicationPhysicalPath, HostingEnvironment.ApplicationVirtualPath).Replace(@"\", "/");
                        RecurseFilesInVirtualPath(virtualPath, includesubdirs, action, true);
                    }

                isFolder = true;
            }

            return isFolder;
        }
    }
}
