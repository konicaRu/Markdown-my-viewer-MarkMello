using MarkMello.Infrastructure.Settings;

namespace MarkMello.Presentation.Tests;

public sealed class JsonSettingsStoreCloseConfirmationTests
{
    [Fact]
    public async Task DisabledQuestionSurvivesARestart()
    {
        var root = Directory.CreateTempSubdirectory("mm-close-").FullName;
        try
        {
            await new JsonSettingsStore(root).SaveConfirmBeforeCloseAsync(false);

            Assert.False(await new JsonSettingsStore(root).LoadConfirmBeforeCloseAsync());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task SettingsFileWithoutTheFlagKeepsAskingByDefault()
    {
        var root = Directory.CreateTempSubdirectory("mm-close-").FullName;
        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(root, "settings.json"),
                """{ "theme": "Dark", "preferences": { "fontFamily": "Serif", "fontSize": 18, "lineHeight": 1.7, "contentWidth": 820 }, "language": "Russian" }""");

            Assert.True(await new JsonSettingsStore(root).LoadConfirmBeforeCloseAsync());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
