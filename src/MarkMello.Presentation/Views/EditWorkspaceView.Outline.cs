using Avalonia;
using Avalonia.Controls;

namespace MarkMello.Presentation.Views;

// IOutlineNavigationHost for edit mode (ADR-0008 Rule 6).
public partial class EditWorkspaceView
{
    private int? _outlineCaretLine;

    public int? CurrentSourceLine => _outlineCaretLine;

    public event EventHandler? CurrentSourceLineChanged;

    /// <summary>
    /// Caret to the start of the heading's source line, line scrolled into the
    /// scroll-sync anchor position, focus into the editor. Lines past the end
    /// clamp to the end of the text. Returns false only when there is no editor;
    /// without layout the caret is still placed and the scroll is skipped.
    /// </summary>
    public bool TryNavigateToSourceLine(int sourceLine)
    {
        if (_editorTextBox is null)
        {
            return false;
        }

        var index = GetLineStartCharacterIndex(_editorTextBox.Text ?? string.Empty, sourceLine);
        _editorTextBox.SelectionStart = index;
        _editorTextBox.SelectionEnd = index;
        _editorTextBox.CaretIndex = index;
        ScrollEditorToOffset(index);
        FocusEditorAsync();
        return true;
    }

    private void AttachOutlineCaretTracking(TextBox textBox)
    {
        textBox.PropertyChanged += OnEditorTextBoxPropertyChangedForOutline;
        UpdateOutlineCaretLine(textBox);
    }

    private void DetachOutlineCaretTracking(TextBox textBox)
    {
        textBox.PropertyChanged -= OnEditorTextBoxPropertyChangedForOutline;
        _outlineCaretLine = null;
    }

    private void OnEditorTextBoxPropertyChangedForOutline(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextBox.CaretIndexProperty && sender is TextBox textBox)
        {
            UpdateOutlineCaretLine(textBox);
        }
    }

    private void UpdateOutlineCaretLine(TextBox textBox)
    {
        var line = GetSourceLineFromCharacterIndex(textBox.Text ?? string.Empty, textBox.CaretIndex);
        if (line == _outlineCaretLine)
        {
            return;
        }

        _outlineCaretLine = line;
        CurrentSourceLineChanged?.Invoke(this, EventArgs.Empty);
    }
}
