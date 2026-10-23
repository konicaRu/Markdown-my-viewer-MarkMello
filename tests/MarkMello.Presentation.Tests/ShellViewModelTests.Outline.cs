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
    public void SetOutlineWidthNormalizes()
    {
        var shell = CreateHarness().ViewModel;

        shell.SetOutlineWidth(999);

        Assert.Equal(OutlinePreferences.MaxWidth, shell.OutlinePreferences.Width);
    }

    private static RenderedMarkdownDocument WithHeadings()
        => new(
        [
            new MarkdownHeadingBlock(1, [new MarkdownTextInline("One")]) { SourceSpan = new MarkdownSourceSpan(0) },
            new MarkdownHeadingBlock(1, [new MarkdownTextInline("Two")]) { SourceSpan = new MarkdownSourceSpan(4) }
        ]);
}
