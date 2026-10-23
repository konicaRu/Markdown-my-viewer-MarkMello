using MarkMello.Domain;
using MarkMello.Infrastructure.Markdown;

namespace MarkMello.Presentation.Tests;

/// <summary>
/// The outline is built from headings that the real Markdig renderer produced, so these
/// tests go through the renderer instead of hand-made blocks: a heading without a source
/// span would silently disappear from the panel.
/// </summary>
public sealed class DocumentOutlineRenderingTests
{
    [Fact]
    public void RealRendererGivesEveryHeadingItsSourceLine()
    {
        const string markdown = """
            # Guide

            Intro.

            ## Install

            Text.

            ### Windows

            Setext heading
            --------------

            Last paragraph.
            """;

        var document = new MarkdigMarkdownDocumentRenderer().Render(markdown);
        var outline = DocumentOutlineBuilder.Build(document, 6);

        var guide = Assert.Single(outline.Items);
        Assert.Equal("Guide", guide.Text);
        Assert.Equal(0, guide.SourceLine);
        Assert.Equal(["Install", "Setext heading"], guide.Children.Select(c => c.Text));
        Assert.Equal(4, guide.Children[0].SourceLine);
        Assert.Equal("Windows", Assert.Single(guide.Children[0].Children).Text);
        Assert.Equal(8, guide.Children[0].Children[0].SourceLine);
        Assert.Equal(10, guide.Children[1].SourceLine);
    }

    [Fact]
    public void HashLinesInsideCodeFenceAreNotHeadings()
    {
        const string markdown = """
            # Real

            ```bash
            # just a comment
            ## another comment
            ```
            """;

        var document = new MarkdigMarkdownDocumentRenderer().Render(markdown);
        var outline = DocumentOutlineBuilder.Build(document, 6);

        Assert.Equal("Real", Assert.Single(outline.Items).Text);
        Assert.Empty(outline.Items[0].Children);
    }
}
