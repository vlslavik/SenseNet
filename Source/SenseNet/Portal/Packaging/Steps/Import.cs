﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.Packaging;
using SenseNet.Packaging.Steps;
using IO = System.IO;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Packaging.Steps
{
    public class Import : ImportBase
    {
        public string Target { get; set; }

        public override void Execute(ExecutionContext context)
        {
            var sourcePath = ResolvePackagePath(Source, context);
            if (!IO.Directory.Exists(sourcePath) && !IO.File.Exists(sourcePath))
                throw new PackagingException(SR.Errors.Import.SourceNotFound);

            var checkResult = RepositoryPath.IsValidPath(Target);
            if (checkResult != RepositoryPath.PathResult.Correct)
                if (!Target.StartsWith("/root", StringComparison.OrdinalIgnoreCase))
                    throw new PackagingException(SR.Errors.Import.InvalidTarget, RepositoryPath.GetInvalidPathException(checkResult, Target));

            if (!Node.Exists(Target))
                throw new PackagingException(SR.Errors.Import.TargetNotFound);

            base.DoImport(null, sourcePath, Target);
        }

    }
}