namespace MarkMello.Presentation.Views;

public partial class ViewerView
{
    private int? _outlineCurrentLine;

    public int? CurrentSourceLine => _outlineCurrentLine;

    public event EventHandler? CurrentSourceLineChanged;

    public bool TryNavigateToSourceLine(int sourceLine)
        => _documentView?.TryScrollToHeadingSourceLine(sourceLine) == true;

    private void UpdateOutlineCurrentLine()
    {
        var line = _documentView?.GetCurrentHeadingSourceLine();
        if (line == _outlineCurrentLine)
        {
            return;
        }

        _outlineCurrentLine = line;
        CurrentSourceLineChanged?.Invoke(this, EventArgs.Empty);
    }
}
