#!/usr/bin/env dotnet
#:sdk Cake.Sdk@7.0.0

Setup(context =>
{
    InstallTool("dotnet:?package=bri&version=2026.11.11.1200");

    return new BuildData(
        context.HasArgument("preview"),
        MakeAbsolute(Directory("./artifacts")),
        MakeAbsolute(File("./src/BRI.TestWeb/BRI.TestWeb.csproj")),
        MakeAbsolute(Directory("./src/BRI.TestWeb/layout")),
        System.Environment.GetEnvironmentVariable("AZURE_CONTAINER_REGISTRY")
    );
});


Task("Clean")
    .Does<BuildData>(
        static (context, data) => context.CleanDirectories(data.DirectoryPathsToClean)
    );

Task("Inventory-Registry")
    .IsDependentOn("Clean")
    .Does<BuildData>(
        static async (context, data) => {
            var workload = data.ShouldPrepareAzureWorkloadIdentity()
                            ? await GitHubActions.Commands.AzurePrepareWorkloadIdentity(data.AzureTenantId!, data.AzureClientId!)
                            : null;

            context.Command(
                new CommandSettings {
                    ToolName = "bri",
                    ToolExecutableNames = new[] { "bri", "bri.exe" },
                    WorkingDirectory = data.ArtifactsPath,
                    EnvironmentVariables = workload is null
                        ? new Dictionary<string, string>()
                        : new Dictionary<string, string> {
                            ["AZURE_TENANT_ID"] = workload.TenantId,
                            ["AZURE_CLIENT_ID"] = workload.ClientId,
                            ["AZURE_FEDERATED_TOKEN_FILE"] = workload.FederatedTokenFile.FullPath
                        }
                },
                new ProcessArgumentBuilder()
                    .Append("inventory")
                    .AppendQuotedSecret(data.AzureContainerRegistry)
                    .AppendQuoted(data.InputPath.FullPath)
            );
        }
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
    private const string Input = "input";
    public DirectoryPath InputPath { get; } = ArtifactsPath.Combine(Input);
    public DirectoryPath StatiqInputPath => InputPath; //{ get; } = ArtifactsPath.Combine("input").Combine(AzureContainerRegistry);
    public DirectoryPath OutputPath { get; } = ArtifactsPath.Combine("output");
    public string? AzureTenantId { get; } = System.Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
    public string? AzureClientId { get; } = System.Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
    public bool ShouldPrepareAzureWorkloadIdentity() => GitHubActions.IsRunningOnGitHubActions
                                                        && !string.IsNullOrWhiteSpace(AzureTenantId)
                                                        && !string.IsNullOrWhiteSpace(AzureClientId);
    public DirectoryPath[] DirectoryPathsToClean { get; } = new []{
                                                                    ArtifactsPath,
                                                                    ArtifactsPath.Combine(Input),
                                                                    ArtifactsPath.Combine("output")
                                                                };
}
