using System;
using System.Collections.Generic;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace EngineIo.Build
{
    [CheckBuildProjectConfigurations]
    [UnsetVisualStudioEnvironmentVariables]
    public class Build : NukeBuild
    {
        public static int Main() => Execute<Build>(x => x.Ci);

        [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
        private readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

        [Solution]
        private readonly Solution Solution;

        [GitRepository]
        private readonly GitRepository GitRepository;

        [GitVersion]
        private readonly GitVersion GitVersion;

        private AbsolutePath SourceDirectory => RootDirectory / "src";
        private AbsolutePath OutputDirectory => RootDirectory / "output";

        private string TestProject => Solution.GetProject("EngineIo.Tests");

        Target Cleanup => _ => _
            .Executes(() =>
            {
                SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
                EnsureCleanDirectory(OutputDirectory);
            });

        Target Restore => _ => _
            .DependsOn(Cleanup)
            .Executes(() =>
            {
                DotNetRestore(_ => _
                    .SetProjectFile(Solution)
                );
            });

        Target Compile => _ => _
            .DependsOn(Restore)
            .Executes(() =>
            {
                DotNetBuild(_ => _
                    .SetProjectFile(Solution)
                    .SetConfiguration(Configuration)
                    .SetAssemblyVersion(GitVersion.AssemblySemVer)
                    .SetFileVersion(GitVersion.AssemblySemFileVer)
                    .SetInformationalVersion(GitVersion.InformationalVersion)
                    .EnableNoRestore()
                );
            });

        Target Test => _ => _
            .DependsOn(Compile)
            .Executes(() =>
            {
                DotNetTest(_ => _
                    .SetProjectFile(TestProject)
                    .SetConfiguration(Configuration)
                    .EnableNoBuild()
                    .EnableNoRestore()
                    .ResetVerbosity()
                    .SetResultsDirectory(OutputDirectory)
                );
            });

        Target Pack => _ => _
            .DependsOn(Compile)
            .Produces(OutputDirectory / "*.nupkg")
            .Executes(() =>
            {
                DotNetPack(_ => _
                    .EnableNoBuild()
                    .EnableNoRestore()
                    .SetProject(Solution)
                    .SetConfiguration(Configuration)
                    .SetVersion(GitVersion.NuGetVersionV2)
                    .SetOutputDirectory(OutputDirectory)
                );
            });

        Target Ci => _ => _
            .DependsOn(Test, Pack);
    }
}
