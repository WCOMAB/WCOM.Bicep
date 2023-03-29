Setup(context => new BuildData(
    context.HasArgument("preview"),
    MakeAbsolute(Directory("./artifacts")),
    MakeAbsolute(File("./src/BRI.TestWeb/BRI.TestWeb.csproj")),
    MakeAbsolute(Directory("./src/BRI.TestWeb/layout")),
    System.Environment.GetEnvironmentVariable("AZURE_CONTAINER_REGISTRY")
));


Task("Clean")
    .Does<BuildData>(
        static (context, data) => context.CleanDirectories(data.DirectoryPathsToClean)
    );

Task("Inventory-Registry")
    .IsDependentOn("Clean")
    .Does<BuildData>(
        static (context, data) => context.DotNetTool(
                "tool",
                new DotNetToolSettings {
                    ArgumentCustomization = args => args
                                                        .Append("run")
                                                        .Append("--")
                                                        .Append("bri")
                                                        .Append("inventory")
                                                        .AppendQuotedSecret(data.AzureContainerRegistry)
                                                        .AppendQuoted(data.InputPath.FullPath),
                    WorkingDirectory = data.ArtifactsPath
                }
            )
    );

Task("Prepare-Statiq-Web")
    .IsDependentOn("Inventory-Registry")
    .Does<BuildData>(
        static (context, data) => context.CopyDirectory(
            data.LayoutPath,
            data.StatiqInputPath
        )
    );

Task("Generate-Statiq-Web")
    .IsDependentOn("Prepare-Statiq-Web")
    .Does<BuildData>(
        static (context, data) => context.DotNetRun(
            data.ProjectPath.FullPath,
            new DotNetRunSettings {
                Configuration = "Release",
                WorkingDirectory = data.ArtifactsPath,
                ArgumentCustomization = args => args
                                                    .Append(data.Preview ? "-- preview --virtual-dir WCOM.Bicep" : "--")
                                                    .AppendSwitchQuoted("--root", data.ArtifactsPath.FullPath)
                                                    .AppendSwitchQuoted("--input", data.StatiqInputPath.FullPath)
                                                    .AppendSwitchQuoted("--output", data.OutputPath.FullPath)
            }
        )
    );


Task("Default")
    .IsDependentOn("Generate-Statiq-Web");

Task("GitHub-Actions")
    .IsDependentOn("Default");

RunTarget(Argument("target", "Default"));

public record BuildData(
    bool Preview,
    DirectoryPath ArtifactsPath,
    FilePath ProjectPath,
    DirectoryPath LayoutPath,
    string AzureContainerRegistry
)
{
    public DirectoryPath InputPath { get; } = ArtifactsPath.Combine("input");
    public DirectoryPath StatiqInputPath => InputPath; //{ get; } = ArtifactsPath.Combine("input").Combine(AzureContainerRegistry);
    public DirectoryPath OutputPath { get; } = ArtifactsPath.Combine("output");
    public DirectoryPath[] DirectoryPathsToClean { get; } = new []{
                                                                    ArtifactsPath,
                                                                    ArtifactsPath.Combine("input"),
                                                                    ArtifactsPath.Combine("output")
                                                                };
}