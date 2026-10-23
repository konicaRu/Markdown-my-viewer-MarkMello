using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarkMello.Domain;

namespace MarkMello.Presentation.ViewModels;

/// <summary>
/// Дерево оглавления активного документа. Не знает, viewer сейчас или редактор:
/// навигация уходит наружу событием со строкой исходника (ADR-0008, Presentation).
/// </summary>
public sealed partial class OutlineViewModel : ObservableObject
{
    private int? _currentSourceLine;

    public ObservableCollection<OutlineItemViewModel> Items { get; } = [];

    [ObservableProperty]
    private bool _isEmpty = true;

    public event EventHandler<int>? NavigateRequested;

    public void Update(RenderedMarkdownDocument document, OutlinePreferences preferences)
    {
        var collapsed = Flatten(Items).Where(static i => !i.IsExpanded).Select(static i => i.Key).ToHashSet();

        Items.Clear();
        foreach (var item in DocumentOutlineBuilder.Build(document, preferences.MaxLevel).Items)
        {
            Items.Add(new OutlineItemViewModel(item, parent: null, RefreshActive));
        }

        foreach (var item in Flatten(Items))
        {
            item.IsExpanded = !collapsed.Contains(item.Key);
        }

        ApplyTypography(preferences);
        IsEmpty = Items.Count == 0;
        RefreshActive();
    }

    public void ApplyTypography(OutlinePreferences preferences)
    {
        foreach (var item in Flatten(Items))
        {
            item.FontSize = preferences.GetFontSize(item.Level);
            item.LineBoxHeight = item.FontSize * preferences.LineHeight;
        }
    }

    public void SetCurrentSourceLine(int? sourceLine)
    {
        _currentSourceLine = sourceLine;
        RefreshActive();
    }

    [RelayCommand]
    private void Navigate(OutlineItemViewModel? item)
    {
        if (item is not null)
        {
            NavigateRequested?.Invoke(this, item.SourceLine);
        }
    }

    private void RefreshActive()
    {
        var target = FindVisibleTarget();
        foreach (var item in Flatten(Items))
        {
            item.IsActive = ReferenceEquals(item, target);
        }
    }

    /// <summary>
    /// Последний заголовок не ниже текущей строки; если он внутри свёрнутой ветки —
    /// её самый верхний свёрнутый предок, то есть ближайший видимый (Rule 7).
    /// </summary>
    private OutlineItemViewModel? FindVisibleTarget()
    {
        if (_currentSourceLine is not { } line)
        {
            return null;
        }

        var current = Flatten(Items).LastOrDefault(i => i.SourceLine <= line);
        if (current is null)
        {
            return null;
        }

        var path = new List<OutlineItemViewModel>();
        for (var node = current; node is not null; node = node.Parent)
        {
            path.Insert(0, node);
        }

        return path.FirstOrDefault(static node => !node.IsExpanded && node.HasChildren) is { } collapsed
            && !ReferenceEquals(collapsed, current)
                ? collapsed
                : current;
    }

    private static IEnumerable<OutlineItemViewModel> Flatten(IEnumerable<OutlineItemViewModel> items)
        => items.SelectMany(static i => Enumerable.Concat([i], Flatten(i.Children)));
}
