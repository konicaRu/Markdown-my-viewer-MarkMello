using MarkMello.Domain;
using MarkMello.Presentation.ViewModels;

namespace MarkMello.Presentation.Tests;

public sealed class OutlineViewModelTests
{
    private static MarkdownHeadingBlock H(int level, string text, int line)
        => new(level, [new MarkdownTextInline(text)]) { SourceSpan = new MarkdownSourceSpan(line) };

    private static readonly RenderedMarkdownDocument Guide = new(
    [
        H(1, "Guide", 0), H(2, "Install", 2), H(3, "Windows", 4), H(3, "Linux", 8), H(2, "Run", 12)
    ]);

    private static OutlineViewModel Create(OutlinePreferences? prefs = null)
    {
        var vm = new OutlineViewModel();
        vm.Update(Guide, prefs ?? OutlinePreferences.Default);
        return vm;
    }

    [Fact]
    public void UpdateBuildsTreeAndAppliesFontSizes()
    {
        var vm = Create();

        var root = Assert.Single(vm.Items);
        Assert.False(vm.IsEmpty);
        Assert.Equal(15, root.FontSize);
        Assert.Equal(13, root.Children[0].Children[0].FontSize);
        Assert.Equal(13 * 1.5, root.Children[0].Children[0].LineBoxHeight, 3);
    }

    [Fact]
    public void EmptyDocumentSetsIsEmpty()
    {
        var vm = new OutlineViewModel();
        vm.Update(RenderedMarkdownDocument.Empty, OutlinePreferences.Default);

        Assert.True(vm.IsEmpty);
        Assert.Empty(vm.Items);
    }

    [Fact]
    public void CurrentLineActivatesLastHeadingAboveIt()
    {
        var vm = Create();

        vm.SetCurrentSourceLine(9);

        var linux = vm.Items[0].Children[0].Children[1];
        Assert.True(linux.IsActive);
        Assert.Single(Flatten(vm.Items), i => i.IsActive);
    }

    [Fact]
    public void CollapsedAncestorIsActivatedInsteadOfHiddenChild()
    {
        var vm = Create();
        var install = vm.Items[0].Children[0];

        vm.SetCurrentSourceLine(9);
        install.IsExpanded = false;

        Assert.True(install.IsActive);
        Assert.False(install.Children[1].IsActive);
    }

    [Fact]
    public void LineBeforeFirstHeadingActivatesNothing()
    {
        var vm = new OutlineViewModel();
        vm.Update(new RenderedMarkdownDocument([H(1, "A", 5)]), OutlinePreferences.Default);

        vm.SetCurrentSourceLine(2);

        Assert.DoesNotContain(Flatten(vm.Items), i => i.IsActive);
    }

    [Fact]
    public void RebuildKeepsCollapsedStateForSameHeading()
    {
        var vm = Create();
        vm.Items[0].Children[0].IsExpanded = false;

        vm.Update(Guide, OutlinePreferences.Default);

        Assert.False(vm.Items[0].Children[0].IsExpanded);
        Assert.True(vm.Items[0].Children[1].IsExpanded);
    }

    [Fact]
    public void NavigateRaisesSourceLine()
    {
        var vm = Create();
        int? requested = null;
        vm.NavigateRequested += (_, line) => requested = line;

        vm.NavigateCommand.Execute(vm.Items[0].Children[1]);

        Assert.Equal(12, requested);
    }

    [Fact]
    public void ApplyTypographyUpdatesSizesWithoutRebuild()
    {
        var vm = Create();
        var root = vm.Items[0];

        vm.ApplyTypography(OutlinePreferences.Default with { H1FontSize = 19, LineHeight = 2.0 });

        Assert.Same(root, vm.Items[0]);
        Assert.Equal(19, root.FontSize);
        Assert.Equal(38, root.LineBoxHeight, 3);
    }

    private static IEnumerable<OutlineItemViewModel> Flatten(IEnumerable<OutlineItemViewModel> items)
        => items.SelectMany(i => new[] { i }.Concat(Flatten(i.Children)));
}
