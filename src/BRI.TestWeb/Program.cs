using BRI.TestWeb;
using Markdig.Extensions.Bootstrap;
using Statiq.Markdown;
const string SiteTitle = nameof(SiteTitle);
const string Project = "WCOM.Bicep";
return await Bootstrapper
    .Factory
    .CreateDefault(args)
    .AddThemeFromUri(new Uri("https://github.com/statiqdev/CleanBlog/archive/8543531ff5acfb9db97b88ec7121c693f198f942.zip"))
    .AddWeb()
    .AddSetting(new KeyValuePair<string, object>(SiteTitle, Project))
    .AddSetting(new KeyValuePair<string, object>(WebKeys.Copyright, FormattableString.Invariant($"©️ WCOM AB {DateTime.Now.Year}")))
    .AddSetting(new KeyValuePair<string, object>(Keys.LinkRoot, $"/{Project}"))
    .AddSetting(new KeyValuePair<string, object> (WebKeys.ApplyDirectoryMetadata , true))
    .AddSetting(new KeyValuePair<string, object>(MarkdownKeys.MarkdownExtensions, new[]
    {
        nameof(DefaultAutoIdentifierExtension),
        nameof(BootstrapExtension),
        nameof(EnhancedBootstrapTable)
    }))
    .AddIncludeCodeShortCode()
    .RunAsync();
