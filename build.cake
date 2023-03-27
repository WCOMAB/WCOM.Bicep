Setup(context => new BuildData(
    MakeAbsolute(Directory("./artifacts")),
    MakeAbsolute(File("./src/BRI.TestWeb/BRI.TestWeb.csproj"))
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

Task("Generate-Statiq-Web")
    .IsDependentOn("Inventory-Registry")
    .Does<BuildData>(
        static (context, data) => context.DotNetRun(
            data.ProjectPath.FullPath,
            new DotNetRunSettings {
                Configuration = "Release",
                WorkingDirectory = data.ArtifactsPath,
                ArgumentCustomization = args => args
                                                    .Append("--")
                                                    .AppendSwitchQuoted("--root", data.ArtifactsPath.FullPath)
                                                    .AppendSwitchQuoted("--input", data.InputPath.FullPath)
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
    DirectoryPath ArtifactsPath,
    FilePath ProjectPath
)
{
    public DirectoryPath InputPath { get; } = ArtifactsPath.Combine("input");
    public DirectoryPath OutputPath { get; } = ArtifactsPath.Combine("output");
    public DirectoryPath[] DirectoryPathsToClean { get; } = new []{
                                                                    ArtifactsPath,
                                                                    ArtifactsPath.Combine("input"),
                                                                    ArtifactsPath.Combine("output")
                                                                };
    public string AzureContainerRegistry { get; } = System.Environment.GetEnvironmentVariable("AZURE_CONTAINER_REGISTRY");
}