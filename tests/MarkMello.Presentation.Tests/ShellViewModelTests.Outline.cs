using MarkMello.Domain;
using MarkMello.Presentation.ViewModels;

namespace MarkMello.Presentation.Tests;

public sealed partial class ShellViewModelTests
{
    [Fact]
    public void ToggleOutlineFlipsVisibilityAndKeepsSide()
    {
        var shell = CreateHarness().ViewModel;

        shell.ToggleOutlineCommand.Execute(null);

        Assert.True(shell.OutlinePreferences.IsVisible);
        Assert.Equal(OutlinePanelSide.Left, shell.OutlinePreferences.Side);

        shell.ToggleOutlineCommand.Execute(null);

        Assert.False(shell.OutlinePreferences.IsVisible);
    }

    [Fact]
    public async Task OutlineSideDecidesWhichColumnGetsContentAndMinimap()
    {
        var harness = CreateHarness();
        var shell = harness.ViewModel;
        var path = Path.Combine(Path.GetTempPath(), "MarkMello.Tests", "outline.md");
        harness.Loader.Sources[path] = CreateSource(path, "# Title\n\n## Section\n\ntext");
        await shell.OpenPathAsync(path);
        Assert.True(shell.IsViewer);
        shell.RenderedDocument = WithHeadings();

        shell.OutlinePreferences = OutlinePreferences.Default with { IsVisible = true, Side = OutlinePanelSide.Right };

        Assert.True(shell.HidesMinimapForOutline);
        Assert.NotNull(shell.OutlineRightContent);
        Assert.Null(shell.OutlineLeftContent);
        Assert.Equal(2, shell.Outline!.Items.Count);

        shell.OutlinePreferences = shell.OutlinePreferences with { Side = OutlinePanelSide.Left };

        Assert.False(shell.HidesMinimapForOutline);
        Assert.NotNull(shell.OutlineLeftContent);
        Assert.Null(shell.OutlineRightContent);
    }

    [Fact]
    public async Task HiddenOutlineIsNotCreatedWhileViewingDocument()
    {
        var harness = CreateHarness();
        var shell = harness.ViewModel;
        var path = Path.Combine(Path.GetTempPath(), "MarkMello.Tests", "hidden-outline.md");
        harness.Loader.Sources[path] = CreateSource(path, "# Title\n\ntext");

        await shell.OpenPathAsync(path);
        shell.RenderedDocument = WithHeadings();

        Assert.True(shell.IsViewer);
        Assert.False(shell.OutlinePreferences.IsVisible);
        Assert.Null(shell.Outline);
        Assert.Null(shell.OutlineLeftContent);
        Assert.Null(shell.OutlineRightContent);
        Assert.False(shell.HidesMinimapForOutline);
    }

    [Fact]
    public async Task FloatingChromeGroupFollowsMenuAndOutlineButtons()
    {
        var harness = CreateHarness();
        var shell = harness.ViewModel;
        var path = Path.Combine(Path.GetTempPath(), "MarkMello.Tests", "chrome-group.md");
        harness.Loader.Sources[path] = CreateSource(path, "# Title" + Environment.NewLine + Environment.NewLine + "text");
        var raised = new List<string?>();
        shell.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        await shell.OpenPathAsync(path);

        Assert.True(shell.ShowsFloatingAppMenuButton);
        Assert.True(shell.ShowsFloatingOutlineButton);
        Assert.True(shell.ShowsFloatingChromeGroup);
        Assert.Contains(nameof(ShellViewModel.ShowsFloatingChromeGroup), raised);

        raised.Clear();
        shell.ToggleEditModeCommand.Execute(null);

        Assert.False(shell.ShowsFloatingAppMenuButton);
        Assert.False(shell.ShowsFloatingOutlineButton);
        Assert.False(shell.ShowsFloatingChromeGroup);
        Assert.Contains(nameof(ShellViewModel.ShowsFloatingChromeGroup), raised);
    }

    [Fact]
    public void SetOutlineWidthNormalizes()
    {
        var shell = CreateHarness().ViewModel;

        shell.SetOutlineWidth(999);

        Assert.Equal(OutlinePreferences.MaxWidth, shell.OutlinePreferences.Width);
    }

    internal static async Task<ShellViewModel> CreateOutlineTestShellAsync(bool withHeadings, bool outlineVisible)
    {
        var harness = CreateHarness();
        var path = Path.Combine(Path.GetTempPath(), "MarkMello.Tests", "outline-view.md");
        harness.Loader.Sources[path] = CreateSource(path, "# One\n\n# Two\n\ntext");
        await harness.ViewModel.OpenPathAsync(path);
        harness.ViewModel.RenderedDocument = withHeadings ? WithHeadings() : new RenderedMarkdownDocument([]);
        harness.ViewModel.OutlinePreferences = OutlinePreferences.Default with { IsVisible = outlineVisible };
        return harness.ViewModel;
    }

    private static RenderedMarkdownDocument WithHeadings()
        => new(
        [
            new MarkdownHeadingBlock(1, [new MarkdownTextInline("One")]) { SourceSpan = new MarkdownSourceSpan(0) },
            new MarkdownHeadingBlock(1, [new MarkdownTextInline("Two")]) { SourceSpan = new MarkdownSourceSpan(4) }
        ]);

    [Fact]
    public void LevelSettingsChangeFontSizeAndMaxLevel()
    {
        var shell = CreateHarness().ViewModel;
        var h2 = shell.OutlineLevelSettings[1];
        var before = shell.OutlinePreferences.H2FontSize;

        h2.IncreaseFontSizeCommand.Execute(null);
        shell.OutlineLevelSettings[4].IsMaxLevel = true;

        Assert.Equal(before + 1, shell.OutlinePreferences.H2FontSize);
        Assert.Equal($"{before + 1} px", h2.FontSizeLabel);
        Assert.Equal(5, shell.OutlinePreferences.MaxLevel);
        Assert.True(shell.OutlineLevelSettings[4].IsWithinMaxLevel);
        Assert.False(shell.OutlineLevelSettings[5].IsWithinMaxLevel);
    }

    [Fact]
    public void LineHeightStepsAndSideSelection()
    {
        var shell = CreateHarness().ViewModel;
        var before = shell.OutlinePreferences.LineHeight;

        shell.IncreaseOutlineLineHeightCommand.Execute(null);
        shell.IsOutlineRightSelected = true;

        Assert.Equal(before + OutlinePreferences.LineHeightStep, shell.OutlinePreferences.LineHeight, 3);
        Assert.Equal(OutlinePanelSide.Right, shell.OutlinePreferences.Side);
        Assert.True(shell.IsOutlineRightSelected);
        Assert.False(shell.IsOutlineLeftSelected);
    }
}
