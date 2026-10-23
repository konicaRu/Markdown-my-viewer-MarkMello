# Document Outline Panel Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Панель оглавления текущего документа (ADR-0008): дерево заголовков слева или справа, переходы по клику в просмотре и в редакторе, подсветка текущего раздела, настройки в «Расширенных настройках».

**Architecture:** Модель оглавления и настройки живут в Domain (чистые функции, тестируются без UI). Сохранение идёт через существующий `ISettingsStore`/`JsonSettingsStore`. В Presentation появляются `OutlineViewModel` (дерево, свёрнутость, активный пункт), partial `ShellViewModel.Outline.cs` (видимость, сторона, ширина, настройки), `OutlinePanelView` и `OutlineSettingsView`. Навигация идёт через интерфейс `IOutlineNavigationHost`, его реализуют `ViewerView` и `EditWorkspaceView`. `MainWindow` находит активный хост так же, как сейчас находит `IFindHost`. Пункт оглавления идентифицируется **номером строки исходника** (`MarkdownSourceSpan.StartLine`), а не якорем: это работает одинаково в просмотре и в редакторе и не требует слагификации в Domain.

**Tech Stack:** .NET 10, Avalonia 11, CommunityToolkit.Mvvm, xUnit, Avalonia Headless (тесты Presentation).

**Spec:** `docs/adr/adr_0008_document_outline_panel.md`. **Макет:** https://claude.ai/artifact/EvNF92wcCdCyW8RmehWXGS

**Правила проекта (AGENTS.md):**
- файл не длиннее примерно 300 строк, компонент не длиннее примерно 180;
- тексты интерфейса только из ADR и макета;
- коммиты в формате `:emoji: type(scope): description` по-английски, в конце строка `Co-Authored-By`;
- запускать только тесты затронутого кода;
- `TreatWarningsAsErrors=true`, поэтому любое предупреждение ломает сборку.

---

## Карта файлов

| Файл | Действие | Ответственность |
|---|---|---|
| `src/MarkMello.Domain/Outline/OutlinePanelSide.cs` | create | enum стороны |
| `src/MarkMello.Domain/Outline/OutlinePreferences.cs` | create | настройки панели + нормализация |
| `src/MarkMello.Domain/Outline/OutlineItem.cs` | create | `OutlineItem`, `DocumentOutline` |
| `src/MarkMello.Domain/Outline/MarkdownInlinePlainText.cs` | create | inlines → plain text |
| `src/MarkMello.Domain/Outline/DocumentOutlineBuilder.cs` | create | документ → дерево |
| `tests/MarkMello.Domain.Tests/OutlinePreferencesTests.cs` | create | тесты нормализации |
| `tests/MarkMello.Domain.Tests/DocumentOutlineBuilderTests.cs` | create | тесты построения |
| `src/MarkMello.Application/Abstractions/ISettingsStore.cs` | modify | +Load/SaveOutlinePreferencesAsync |
| `src/MarkMello.Infrastructure/Settings/SettingsFileModel.cs` | modify | +`Outline` |
| `src/MarkMello.Infrastructure/Settings/JsonSettingsStore.cs` | modify | хранение outline |
| `src/MarkMello.Infrastructure/Serialization/MarkMelloJsonSerializerContext.cs` | modify | AOT-типы |
| `tests/MarkMello.Presentation.Tests/TestDoubles.cs` | modify | `InMemorySettingsStore` |
| `tests/MarkMello.Presentation.Tests/JsonSettingsStoreOutlineTests.cs` | create | round-trip, старый файл |
| `src/MarkMello.Presentation/ViewModels/OutlineItemViewModel.cs` | create | пункт дерева |
| `src/MarkMello.Presentation/ViewModels/OutlineViewModel.cs` | create | дерево, активный пункт, навигация |
| `src/MarkMello.Presentation/ViewModels/OutlineLevelSetting.cs` | create | строка настройки уровня |
| `src/MarkMello.Presentation/ViewModels/ShellViewModel.Outline.cs` | create | видимость/сторона/ширина/источник |
| `src/MarkMello.Presentation/ViewModels/ShellViewModel.OutlineSettings.cs` | create | свойства блока настроек |
| `tests/MarkMello.Presentation.Tests/OutlineViewModelTests.cs` | create | тесты VM |
| `tests/MarkMello.Presentation.Tests/ShellViewModelTests.Outline.cs` | create | toggle, сторона, настройки |
| `src/MarkMello.Presentation/Views/IOutlineNavigationHost.cs` | create | контракт навигации |
| `src/MarkMello.Presentation/Views/MarkdownDocumentView.Outline.cs` | create | прокрутка к строке, текущий заголовок |
| `src/MarkMello.Presentation/Views/ViewerView.Outline.cs` | create | хост для просмотра |
| `src/MarkMello.Presentation/Views/EditWorkspaceView.Outline.cs` | create | хост для редактора |
| `src/MarkMello.Presentation/Views/OutlinePanelView.axaml(.cs)` | create | панель |
| `src/MarkMello.Presentation/Views/OutlineSettingsView.axaml(.cs)` | create | блок настроек |
| `src/MarkMello.Presentation/Views/MainWindow.Outline.cs` | create | колонки, splitter'ы, связь с хостом |
| `src/MarkMello.Presentation/Views/MainWindow.axaml` | modify | колонки, кнопка, горячая клавиша |
| `src/MarkMello.Presentation/Views/AdvancedReadingSettingsWindow.axaml` | modify | +`OutlineSettingsView` |
| `src/MarkMello.Presentation/Localization/LocalizationService.Outline.cs` | create | строки RU/EN |

---

### Task 0: Окружение и базовая сборка

**Files:** нет

- [ ] **Step 1: Установить .NET 10 SDK**

Run: `winget install --id Microsoft.DotNet.SDK.10 -e`
Затем: `dotnet --list-sdks`
Expected: в списке есть `10.0.x`.

- [ ] **Step 2: Убедиться, что ветка чистая и собирается**

Run (из корня репозитория):
```bash
git switch feat/document-outline
git status --short
dotnet build MarkMello.sln -c Debug
```
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`.

- [ ] **Step 3: Базовый прогон доменных тестов**

Run: `dotnet test tests/MarkMello.Domain.Tests`
Expected: все тесты PASS. Если нет, остановиться и сообщить: это проблема окружения, а не фичи.

---

### Task 1: `OutlinePanelSide` и `OutlinePreferences`

**Files:**
- Create: `src/MarkMello.Domain/Outline/OutlinePanelSide.cs`
- Create: `src/MarkMello.Domain/Outline/OutlinePreferences.cs`
- Test: `tests/MarkMello.Domain.Tests/OutlinePreferencesTests.cs`

- [ ] **Step 1: Написать падающие тесты**

```csharp
using MarkMello.Domain;

namespace MarkMello.Domain.Tests;

public sealed class OutlinePreferencesTests
{
    [Fact]
    public void DefaultMatchesAdr()
    {
        var d = OutlinePreferences.Default;

        Assert.False(d.IsVisible);
        Assert.Equal(OutlinePanelSide.Left, d.Side);
        Assert.Equal(3, d.MaxLevel);
        Assert.Equal(1.5, d.LineHeight);
        Assert.Equal(260, d.Width);
        Assert.Equal(new[] { 15, 14, 13, 13, 13, 13 }, Enumerable.Range(1, 6).Select(d.GetFontSize));
    }

    [Fact]
    public void NormalizeReturnsDefaultForNull()
        => Assert.Equal(OutlinePreferences.Default, OutlinePreferences.Normalize(null));

    [Fact]
    public void NormalizeClampsEveryValue()
    {
        var candidate = new OutlinePreferences(
            IsVisible: true,
            Side: (OutlinePanelSide)42,
            MaxLevel: 9,
            LineHeight: 5,
            Width: 10,
            H1FontSize: 99,
            H2FontSize: 1);

        var n = OutlinePreferences.Normalize(candidate);

        Assert.True(n.IsVisible);
        Assert.Equal(OutlinePanelSide.Left, n.Side);
        Assert.Equal(6, n.MaxLevel);
        Assert.Equal(2.0, n.LineHeight);
        Assert.Equal(220, n.Width);
        Assert.Equal(20, n.H1FontSize);
        Assert.Equal(11, n.H2FontSize);
    }

    [Fact]
    public void NormalizeRoundsLineHeightToStep()
    {
        var n = OutlinePreferences.Normalize(OutlinePreferences.Default with { LineHeight = 1.56 });

        Assert.Equal(1.6, n.LineHeight);
    }

    [Fact]
    public void NormalizeReplacesNonFiniteNumbersWithDefaults()
    {
        var n = OutlinePreferences.Normalize(
            OutlinePreferences.Default with { LineHeight = double.NaN, Width = double.PositiveInfinity });

        Assert.Equal(OutlinePreferences.DefaultLineHeight, n.LineHeight);
        Assert.Equal(OutlinePreferences.DefaultWidth, n.Width);
    }

    [Theory]
    [InlineData(1, 18)]
    [InlineData(4, 12)]
    [InlineData(6, 20)]
    public void WithFontSizeChangesOnlyThatLevel(int level, int size)
    {
        var changed = OutlinePreferences.Default.WithFontSize(level, size);

        Assert.Equal(size, changed.GetFontSize(level));
        foreach (var other in Enumerable.Range(1, 6).Where(l => l != level))
        {
            Assert.Equal(OutlinePreferences.Default.GetFontSize(other), changed.GetFontSize(other));
        }
    }
}
```

- [ ] **Step 2: Убедиться, что тесты падают**

Run: `dotnet test tests/MarkMello.Domain.Tests --filter FullyQualifiedName~OutlinePreferencesTests`
Expected: FAIL компиляции: `OutlinePreferences` не найден.

- [ ] **Step 3: Реализация**

`src/MarkMello.Domain/Outline/OutlinePanelSide.cs`:
```csharp
namespace MarkMello.Domain;

/// <summary>Сторона окна, у которой стоит панель оглавления (ADR-0008 Rule 3).</summary>
public enum OutlinePanelSide
{
    Left = 0,
    Right = 1
}
```

`src/MarkMello.Domain/Outline/OutlinePreferences.cs`:
```csharp
namespace MarkMello.Domain;

/// <summary>
/// Настройки панели оглавления (ADR-0008 Rule 11-12). Размеры шрифта заданы в пикселях
/// и не зависят от <see cref="ReadingPreferences.InterfaceFontSize"/>.
/// </summary>
public sealed record OutlinePreferences(
    bool IsVisible = false,
    OutlinePanelSide Side = OutlinePanelSide.Left,
    int MaxLevel = OutlinePreferences.DefaultMaxLevel,
    double LineHeight = OutlinePreferences.DefaultLineHeight,
    double Width = OutlinePreferences.DefaultWidth,
    int H1FontSize = 15,
    int H2FontSize = 14,
    int H3FontSize = 13,
    int H4FontSize = 13,
    int H5FontSize = 13,
    int H6FontSize = 13)
{
    public const int MinLevel = 1;
    public const int MaxLevelLimit = 6;
    public const int DefaultMaxLevel = 3;
    public const double MinLineHeight = 1.2;
    public const double MaxLineHeight = 2.0;
    public const double LineHeightStep = 0.1;
    public const double DefaultLineHeight = 1.5;
    public const int MinFontSize = 11;
    public const int MaxFontSize = 20;
    public const double MinWidth = 220;
    public const double MaxWidth = 340;
    public const double DefaultWidth = 260;

    public static OutlinePreferences Default { get; } = new();

    public static OutlinePreferences Normalize(OutlinePreferences? preferences)
    {
        if (preferences is null)
        {
            return Default;
        }

        return new OutlinePreferences(
            preferences.IsVisible,
            Enum.IsDefined(preferences.Side) ? preferences.Side : Default.Side,
            Math.Clamp(preferences.MaxLevel, MinLevel, MaxLevelLimit),
            NormalizeLineHeight(preferences.LineHeight),
            NormalizeWidth(preferences.Width),
            ClampFont(preferences.H1FontSize),
            ClampFont(preferences.H2FontSize),
            ClampFont(preferences.H3FontSize),
            ClampFont(preferences.H4FontSize),
            ClampFont(preferences.H5FontSize),
            ClampFont(preferences.H6FontSize));
    }

    public OutlinePreferences Normalize() => Normalize(this);

    public int GetFontSize(int level) => level switch
    {
        1 => H1FontSize,
        2 => H2FontSize,
        3 => H3FontSize,
        4 => H4FontSize,
        5 => H5FontSize,
        _ => H6FontSize
    };

    public OutlinePreferences WithFontSize(int level, int size) => level switch
    {
        1 => this with { H1FontSize = size },
        2 => this with { H2FontSize = size },
        3 => this with { H3FontSize = size },
        4 => this with { H4FontSize = size },
        5 => this with { H5FontSize = size },
        _ => this with { H6FontSize = size }
    };

    public static double NormalizeWidth(double value)
        => double.IsFinite(value) ? Math.Clamp(value, MinWidth, MaxWidth) : DefaultWidth;

    private static int ClampFont(int value) => Math.Clamp(value, MinFontSize, MaxFontSize);

    private static double NormalizeLineHeight(double value)
    {
        if (!double.IsFinite(value))
        {
            return DefaultLineHeight;
        }

        var clamped = Math.Clamp(value, MinLineHeight, MaxLineHeight);
        var rounded = Math.Round(clamped / LineHeightStep, MidpointRounding.AwayFromZero) * LineHeightStep;
        return Math.Round(rounded, 1, MidpointRounding.AwayFromZero);
    }
}
```

- [ ] **Step 4: Тесты проходят**

Run: `dotnet test tests/MarkMello.Domain.Tests --filter FullyQualifiedName~OutlinePreferencesTests`
Expected: PASS (6 тестов, из них 3 случая Theory).

- [ ] **Step 5: Commit**

```bash
git add src/MarkMello.Domain/Outline tests/MarkMello.Domain.Tests/OutlinePreferencesTests.cs
git commit -m ":sparkles: feat(outline): add outline preferences model" -m "Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>"
```

---

### Task 2: Модель оглавления и построитель

**Files:**
- Create: `src/MarkMello.Domain/Outline/OutlineItem.cs`
- Create: `src/MarkMello.Domain/Outline/MarkdownInlinePlainText.cs`
- Create: `src/MarkMello.Domain/Outline/DocumentOutlineBuilder.cs`
- Test: `tests/MarkMello.Domain.Tests/DocumentOutlineBuilderTests.cs`

- [ ] **Step 1: Падающие тесты**

```csharp
using MarkMello.Domain;

namespace MarkMello.Domain.Tests;

public sealed class DocumentOutlineBuilderTests
{
    private static MarkdownHeadingBlock H(int level, string text, int line)
        => new(level, [new MarkdownTextInline(text)]) { SourceSpan = new MarkdownSourceSpan(line) };

    private static RenderedMarkdownDocument Doc(params MarkdownBlock[] blocks) => new(blocks);

    [Fact]
    public void EmptyDocumentGivesEmptyOutline()
        => Assert.True(DocumentOutlineBuilder.Build(RenderedMarkdownDocument.Empty, 6).IsEmpty);

    [Fact]
    public void NestsHeadingsByLevel()
    {
        var outline = DocumentOutlineBuilder.Build(
            Doc(H(1, "Guide", 0), H(2, "Install", 2), H(3, "Windows", 4), H(2, "Run", 6)), 6);

        var root = Assert.Single(outline.Items);
        Assert.Equal("Guide", root.Text);
        Assert.Equal(new[] { "Install", "Run" }, root.Children.Select(c => c.Text));
        Assert.Equal("Windows", Assert.Single(root.Children[0].Children).Text);
        Assert.Equal(4, root.Children[0].Children[0].SourceLine);
    }

    [Fact]
    public void SkippedLevelAttachesToNearestShallowerHeading()
    {
        var outline = DocumentOutlineBuilder.Build(Doc(H(1, "A", 0), H(3, "C", 1)), 6);

        Assert.Equal("C", Assert.Single(Assert.Single(outline.Items).Children).Text);
    }

    [Fact]
    public void HeadingsDeeperThanMaxLevelAreDropped()
    {
        var outline = DocumentOutlineBuilder.Build(Doc(H(1, "A", 0), H(2, "B", 1), H(3, "C", 2)), 2);

        Assert.Empty(Assert.Single(Assert.Single(outline.Items).Children).Children);
    }

    [Fact]
    public void FirstHeadingDeeperThanRootIsStillRoot()
    {
        var outline = DocumentOutlineBuilder.Build(Doc(H(2, "B", 0), H(1, "A", 3)), 6);

        Assert.Equal(new[] { "B", "A" }, outline.Items.Select(i => i.Text));
    }

    [Fact]
    public void HeadingWithoutSourceSpanIsSkipped()
    {
        var noSpan = new MarkdownHeadingBlock(1, [new MarkdownTextInline("X")]);

        Assert.True(DocumentOutlineBuilder.Build(Doc(noSpan), 6).IsEmpty);
    }

    [Fact]
    public void InlineMarkupBecomesPlainText()
    {
        var heading = new MarkdownHeadingBlock(1,
        [
            new MarkdownStrongInline([new MarkdownTextInline("Bold")]),
            new MarkdownTextInline("  and "),
            new MarkdownCodeInline("code"),
            new MarkdownLinkInline([new MarkdownTextInline(" link")], "#x", null)
        ]) { SourceSpan = new MarkdownSourceSpan(0) };

        Assert.Equal("Bold and code link", Assert.Single(DocumentOutlineBuilder.Build(Doc(heading), 6).Items).Text);
    }

    [Fact]
    public void ParagraphsAreIgnored()
    {
        var outline = DocumentOutlineBuilder.Build(
            Doc(new MarkdownParagraphBlock([new MarkdownTextInline("# not a heading")])), 6);

        Assert.True(outline.IsEmpty);
    }
}
```

- [ ] **Step 2: Тесты падают**

Run: `dotnet test tests/MarkMello.Domain.Tests --filter FullyQualifiedName~DocumentOutlineBuilderTests`
Expected: FAIL компиляции: `DocumentOutlineBuilder` не найден.

- [ ] **Step 3: Реализация**

`OutlineItem.cs`:
```csharp
namespace MarkMello.Domain;

/// <summary>
/// Пункт оглавления. Идентифицируется строкой исходника: она одинаково понятна
/// viewer'у (прокрутка к заголовку) и редактору (курсор на строку).
/// </summary>
public sealed record OutlineItem(int Level, string Text, int SourceLine, IReadOnlyList<OutlineItem> Children);

public sealed record DocumentOutline(IReadOnlyList<OutlineItem> Items)
{
    public static DocumentOutline Empty { get; } = new(Array.Empty<OutlineItem>());

    public bool IsEmpty => Items.Count == 0;
}
```

`MarkdownInlinePlainText.cs`:
```csharp
using System.Text;

namespace MarkMello.Domain;

/// <summary>Plain text заголовка для оглавления: разметка отбрасывается, пробелы схлопываются.</summary>
public static class MarkdownInlinePlainText
{
    public static string From(IReadOnlyList<MarkdownInline> inlines)
    {
        var builder = new StringBuilder();
        Append(builder, inlines);
        return string.Join(' ', builder.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static void Append(StringBuilder builder, IReadOnlyList<MarkdownInline> inlines)
    {
        foreach (var inline in inlines)
        {
            switch (inline)
            {
                case MarkdownTextInline text:
                    builder.Append(text.Text);
                    break;
                case MarkdownCodeInline code:
                    builder.Append(code.Code);
                    break;
                case MarkdownStrongInline strong:
                    Append(builder, strong.Inlines);
                    break;
                case MarkdownEmphasisInline emphasis:
                    Append(builder, emphasis.Inlines);
                    break;
                case MarkdownLinkInline link:
                    Append(builder, link.Inlines);
                    break;
                case MarkdownImageInline image:
                    builder.Append(image.AltText);
                    break;
                case MarkdownLineBreakInline:
                    builder.Append(' ');
                    break;
            }
        }
    }
}
```

`DocumentOutlineBuilder.cs`:
```csharp
namespace MarkMello.Domain;

/// <summary>
/// Строит оглавление из уже разобранного документа (ADR-0008 Rule 10): только
/// заголовки верхнего уровня документа, без повторного парсинга исходника.
/// Пропущенный уровень не создаёт пустых узлов (Rule 5).
/// </summary>
public static class DocumentOutlineBuilder
{
    public static DocumentOutline Build(RenderedMarkdownDocument document, int maxLevel)
    {
        var roots = new List<Node>();
        var stack = new Stack<Node>();

        foreach (var block in document.Blocks)
        {
            if (block is not MarkdownHeadingBlock { SourceSpan: { } span } heading || heading.Level > maxLevel)
            {
                continue;
            }

            var node = new Node(heading.Level, MarkdownInlinePlainText.From(heading.Inlines), span.StartLine);
            while (stack.Count > 0 && stack.Peek().Level >= node.Level)
            {
                stack.Pop();
            }

            if (stack.Count == 0)
            {
                roots.Add(node);
            }
            else
            {
                stack.Peek().Children.Add(node);
            }

            stack.Push(node);
        }

        return roots.Count == 0 ? DocumentOutline.Empty : new DocumentOutline(roots.Select(Freeze).ToArray());
    }

    private static OutlineItem Freeze(Node node)
        => new(node.Level, node.Text, node.SourceLine, node.Children.Select(Freeze).ToArray());

    private sealed record Node(int Level, string Text, int SourceLine)
    {
        public List<Node> Children { get; } = [];
    }
}
```

- [ ] **Step 4: Тесты проходят**

Run: `dotnet test tests/MarkMello.Domain.Tests --filter FullyQualifiedName~DocumentOutlineBuilderTests`
Expected: PASS (8).

- [ ] **Step 5: Commit**

```bash
git add src/MarkMello.Domain/Outline tests/MarkMello.Domain.Tests/DocumentOutlineBuilderTests.cs
git commit -m ":sparkles: feat(outline): build document outline from rendered headings" -m "Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>"
```

---

### Task 3: Сохранение настроек

**Files:**
- Modify: `src/MarkMello.Application/Abstractions/ISettingsStore.cs`
- Modify: `src/MarkMello.Infrastructure/Settings/SettingsFileModel.cs`
- Modify: `src/MarkMello.Infrastructure/Settings/JsonSettingsStore.cs`
- Modify: `src/MarkMello.Infrastructure/Serialization/MarkMelloJsonSerializerContext.cs`
- Modify: `tests/MarkMello.Presentation.Tests/TestDoubles.cs` (класс `InMemorySettingsStore`, строка ~100)
- Test: `tests/MarkMello.Presentation.Tests/JsonSettingsStoreOutlineTests.cs`

- [ ] **Step 1: Падающие тесты**

```csharp
using MarkMello.Domain;
using MarkMello.Infrastructure.Settings;

namespace MarkMello.Presentation.Tests;

public sealed class JsonSettingsStoreOutlineTests
{
    [Fact]
    public async Task OutlinePreferencesRoundTrip()
    {
        var root = Directory.CreateTempSubdirectory("mm-outline-").FullName;
        try
        {
            var expected = OutlinePreferences.Default with
            {
                IsVisible = true,
                Side = OutlinePanelSide.Right,
                MaxLevel = 4,
                LineHeight = 1.8,
                Width = 300,
                H2FontSize = 17
            };

            await new JsonSettingsStore(root).SaveOutlinePreferencesAsync(expected);
            var actual = await new JsonSettingsStore(root).LoadOutlinePreferencesAsync();

            Assert.Equal(expected, actual);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task SettingsFileWithoutOutlineLoadsDefaults()
    {
        var root = Directory.CreateTempSubdirectory("mm-outline-").FullName;
        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(root, "settings.json"),
                """{ "theme": "Dark", "preferences": { "fontFamily": "Serif", "fontSize": 18, "lineHeight": 1.7, "contentWidth": 820 }, "language": "Russian" }""");

            var store = new JsonSettingsStore(root);

            Assert.Equal(OutlinePreferences.Default, await store.LoadOutlinePreferencesAsync());
            Assert.Equal(ThemeMode.Dark, await store.LoadThemeAsync());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
```

- [ ] **Step 2: Тесты падают**

Run: `dotnet test tests/MarkMello.Presentation.Tests --filter FullyQualifiedName~JsonSettingsStoreOutlineTests`
Expected: FAIL компиляции: нет `SaveOutlinePreferencesAsync`.

- [ ] **Step 3: Интерфейс.** В `ISettingsStore` после пары `Load/SaveSidebarWidthAsync` добавить:

```csharp
    ValueTask<OutlinePreferences> LoadOutlinePreferencesAsync(CancellationToken cancellationToken = default);
    ValueTask SaveOutlinePreferencesAsync(OutlinePreferences preferences, CancellationToken cancellationToken = default);
```

- [ ] **Step 4: Файл настроек.** `SettingsFileModel` получает последний параметр:

```csharp
internal sealed record SettingsFileModel(
    ThemeMode Theme,
    ReadingPreferences Preferences,
    AppLanguage Language,
    WindowPlacement? WindowPlacement,
    WindowBorderMode WindowBorder = WindowBorderMode.Auto,
    double? SidebarWidth = null,
    WorkspaceSessionState? Session = null,
    OutlinePreferences? Outline = null);
```

В `MarkMelloJsonSerializerContext` добавить атрибуты:
```csharp
[JsonSerializable(typeof(OutlinePreferences))]
[JsonSerializable(typeof(OutlinePanelSide))]
```

- [ ] **Step 5: `JsonSettingsStore`.**
- Поле рядом с `_sidebarWidth`: `private OutlinePreferences _outline = OutlinePreferences.Default;`.
- Методы после `SaveSidebarWidthAsync`:

```csharp
    public ValueTask<OutlinePreferences> LoadOutlinePreferencesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            EnsureLoadedCore();
            return ValueTask.FromResult(_outline);
        }
    }

    public ValueTask SaveOutlinePreferencesAsync(OutlinePreferences preferences, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            EnsureLoadedCore();
            _outline = OutlinePreferences.Normalize(preferences);
            PersistCore();
        }

        return ValueTask.CompletedTask;
    }
```

- В `EnsureLoadedCore`, внутри `if (fileModel is not null)`, после `_session = ...`: `_outline = OutlinePreferences.Normalize(fileModel.Outline);`. В `catch`, после `_session = WorkspaceSessionState.Empty;`: `_outline = OutlinePreferences.Default;`.
- В `PersistCore` последним аргументом `new SettingsFileModel(...)` передать `_outline`.

- [ ] **Step 6: Тестовый двойник.** В `InMemorySettingsStore` (`tests/MarkMello.Presentation.Tests/TestDoubles.cs`) добавить:

```csharp
    public OutlinePreferences OutlinePreferences { get; set; } = OutlinePreferences.Default;

    public ValueTask<OutlinePreferences> LoadOutlinePreferencesAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(OutlinePreferences);

    public ValueTask SaveOutlinePreferencesAsync(OutlinePreferences preferences, CancellationToken cancellationToken = default)
    {
        OutlinePreferences = preferences;
        return ValueTask.CompletedTask;
    }
```

- [ ] **Step 7: Тесты проходят и ничего не сломано**

Run: `dotnet test tests/MarkMello.Presentation.Tests --filter "FullyQualifiedName~JsonSettingsStore"`
Expected: PASS: и новые тесты, и существующие `JsonSettingsStoreTests`.

- [ ] **Step 8: Commit**

```bash
git add src/MarkMello.Application src/MarkMello.Infrastructure tests/MarkMello.Presentation.Tests/TestDoubles.cs tests/MarkMello.Presentation.Tests/JsonSettingsStoreOutlineTests.cs
git commit -m ":sparkles: feat(settings): persist outline preferences" -m "Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>"
```

---

### Task 4: `OutlineViewModel` и `OutlineItemViewModel`

**Files:**
- Create: `src/MarkMello.Presentation/ViewModels/OutlineItemViewModel.cs`
- Create: `src/MarkMello.Presentation/ViewModels/OutlineViewModel.cs`
- Test: `tests/MarkMello.Presentation.Tests/OutlineViewModelTests.cs`

- [ ] **Step 1: Падающие тесты**

```csharp
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
```

- [ ] **Step 2: Тесты падают**

Run: `dotnet test tests/MarkMello.Presentation.Tests --filter FullyQualifiedName~OutlineViewModelTests`
Expected: FAIL компиляции.

- [ ] **Step 3: `OutlineItemViewModel`**

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MarkMello.Domain;

namespace MarkMello.Presentation.ViewModels;

/// <summary>Пункт дерева оглавления. Свёрнутость живёт, пока открыт документ (ADR-0008 Rule 8).</summary>
public sealed partial class OutlineItemViewModel : ObservableObject
{
    private readonly Action _expansionChanged;

    internal OutlineItemViewModel(OutlineItem item, OutlineItemViewModel? parent, Action expansionChanged)
    {
        Level = item.Level;
        Text = item.Text;
        SourceLine = item.SourceLine;
        Parent = parent;
        _expansionChanged = expansionChanged;
        Children = new ObservableCollection<OutlineItemViewModel>(
            item.Children.Select(child => new OutlineItemViewModel(child, this, expansionChanged)));
    }

    public int Level { get; }

    public string Text { get; }

    public int SourceLine { get; }

    public OutlineItemViewModel? Parent { get; }

    public ObservableCollection<OutlineItemViewModel> Children { get; }

    public bool HasChildren => Children.Count > 0;

    public bool IsTopLevel => Level == 1;

    internal string Key => $"{Level}:{Text}";

    [ObservableProperty]
    private bool _isExpanded = true;

    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private double _fontSize;

    /// <summary>Высота строки текста: размер × межстрочный интервал (Rule 11).</summary>
    [ObservableProperty]
    private double _lineBoxHeight;

    partial void OnIsExpandedChanged(bool value) => _expansionChanged();
}
```

- [ ] **Step 4: `OutlineViewModel`**

```csharp
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
    private OutlinePreferences _preferences = OutlinePreferences.Default;
    private int? _currentSourceLine;

    public ObservableCollection<OutlineItemViewModel> Items { get; } = [];

    [ObservableProperty]
    private bool _isEmpty = true;

    public event EventHandler<int>? NavigateRequested;

    public void Update(RenderedMarkdownDocument document, OutlinePreferences preferences)
    {
        var collapsed = Flatten(Items).Where(static i => !i.IsExpanded).Select(static i => i.Key).ToHashSet();
        _preferences = preferences;

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
        _preferences = preferences;
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
        => items.SelectMany(static i => new[] { i }.Concat(Flatten(i.Children)));
}
```

- [ ] **Step 5: Тесты проходят**

Run: `dotnet test tests/MarkMello.Presentation.Tests --filter FullyQualifiedName~OutlineViewModelTests`
Expected: PASS (8).

- [ ] **Step 6: Commit**

```bash
git add src/MarkMello.Presentation/ViewModels/OutlineItemViewModel.cs src/MarkMello.Presentation/ViewModels/OutlineViewModel.cs tests/MarkMello.Presentation.Tests/OutlineViewModelTests.cs
git commit -m ":sparkles: feat(outline): add outline tree view model" -m "Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>"
```

---

### Task 5: `ShellViewModel`: видимость, сторона, ширина, источник документа

**Files:**
- Create: `src/MarkMello.Presentation/ViewModels/ShellViewModel.Outline.cs`
- Modify: `src/MarkMello.Presentation/ViewModels/ShellViewModel.cs`: `InitializeAsync()` (строка ~461) и конструктор
- Test: `tests/MarkMello.Presentation.Tests/ShellViewModelTests.Outline.cs`

- [ ] **Step 1: Падающие тесты**

```csharp
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
    public void OutlineSideDecidesWhichColumnGetsContentAndMinimap()
    {
        var shell = CreateHarness().ViewModel;
        shell.OutlinePreferences = OutlinePreferences.Default with { IsVisible = true, Side = OutlinePanelSide.Right };

        Assert.Equal(shell.ShowsOutline, shell.HidesMinimapForOutline);
        Assert.Null(shell.OutlineLeftContent);
    }

    [Fact]
    public void SetOutlineWidthNormalizes()
    {
        var shell = CreateHarness().ViewModel;

        shell.SetOutlineWidth(999);

        Assert.Equal(OutlinePreferences.MaxWidth, shell.OutlinePreferences.Width);
    }
}
```

> Класс `ShellViewModelTests` уже partial (см. `ShellViewModelTests.AdvancedReading.cs`), `CreateHarness()` существует. Если класс в репозитории не partial, добавить модификатор `partial` в основной файл.

- [ ] **Step 2: Тесты падают** (`dotnet test tests/MarkMello.Presentation.Tests --filter FullyQualifiedName~ShellViewModelTests`, FAIL компиляции).

- [ ] **Step 3: `ShellViewModel.Outline.cs`**

```csharp
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarkMello.Domain;

namespace MarkMello.Presentation.ViewModels;

/// <summary>
/// Панель оглавления (ADR-0008). Пока панель выключена, <see cref="Outline"/> не создаётся
/// и документ не обходится (Rule 1).
/// </summary>
public partial class ShellViewModel
{
    private OutlineViewModel? _outline;
    private EditorSessionViewModel? _outlineObservedEditor;

    [ObservableProperty]
    private OutlinePreferences _outlinePreferences = OutlinePreferences.Default;

    public OutlineViewModel? Outline => ShowsOutline ? _outline ??= new OutlineViewModel() : null;

    public bool ShowsOutline => OutlinePreferences.IsVisible && IsViewer;

    public object? OutlineLeftContent => ShowsOutline && OutlinePreferences.Side == OutlinePanelSide.Left ? Outline : null;

    public object? OutlineRightContent => ShowsOutline && OutlinePreferences.Side == OutlinePanelSide.Right ? Outline : null;

    public bool HidesMinimapForOutline => ShowsOutline && OutlinePreferences.Side == OutlinePanelSide.Right;

    /// <summary>Кнопка живёт в плавающей группе; в edit mode группа скрыта автором, там остаётся горячая клавиша.</summary>
    public bool ShowsFloatingOutlineButton => IsViewer && !IsEditMode;

    public bool IsOutlineToggleChecked => OutlinePreferences.IsVisible;

    public string OutlineToggleTooltip => _localization["OutlineToggleTooltip"];

    [RelayCommand]
    private void ToggleOutline()
        => ApplyOutlinePreferences(OutlinePreferences with { IsVisible = !OutlinePreferences.IsVisible });

    public void SetOutlineWidth(double width)
        => ApplyOutlinePreferences(OutlinePreferences with { Width = OutlinePreferences.NormalizeWidth(width) });

    internal async Task LoadOutlinePreferencesAsync()
        => OutlinePreferences = OutlinePreferences.Normalize(
            await _settings.LoadOutlinePreferencesAsync().ConfigureAwait(true));

    private void InitializeOutline() => PropertyChanged += OnOutlineSourcePropertyChanged;

    private void ApplyOutlinePreferences(OutlinePreferences preferences)
    {
        var normalized = OutlinePreferences.Normalize(preferences);
        if (normalized == OutlinePreferences)
        {
            return;
        }

        OutlinePreferences = normalized;
        _ = Task.Run(async () =>
        {
            try
            {
                await _settings.SaveOutlinePreferencesAsync(normalized).ConfigureAwait(false);
            }
            catch
            {
                // Как и настройки чтения: сохранение best-effort, панель работает и без него.
            }
        });
    }

    partial void OnOutlinePreferencesChanged(OutlinePreferences oldValue, OutlinePreferences newValue)
    {
        RaiseOutlineLayoutProperties();
        RefreshOutlineSettingsProperties();

        if (_outline is null)
        {
            RefreshOutlineSource();
            return;
        }

        if (oldValue.MaxLevel != newValue.MaxLevel)
        {
            RefreshOutlineSource();
        }
        else
        {
            _outline.ApplyTypography(newValue);
        }
    }

    private void OnOutlineSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(IsViewer):
                RaiseOutlineLayoutProperties();
                RefreshOutlineSource();
                break;
            case nameof(IsEditMode):
            case nameof(EditorSession):
                OnPropertyChanged(nameof(ShowsFloatingOutlineButton));
                ObserveEditorForOutline();
                RefreshOutlineSource();
                break;
            case nameof(RenderedDocument):
                RefreshOutlineSource();
                break;
        }
    }

    private void ObserveEditorForOutline()
    {
        var editor = IsEditMode ? EditorSession : null;
        if (ReferenceEquals(editor, _outlineObservedEditor))
        {
            return;
        }

        if (_outlineObservedEditor is not null)
        {
            _outlineObservedEditor.PropertyChanged -= OnOutlineEditorPropertyChanged;
        }

        _outlineObservedEditor = editor;
        if (editor is not null)
        {
            editor.PropertyChanged += OnOutlineEditorPropertyChanged;
        }
    }

    private void OnOutlineEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditorSessionViewModel.RenderedPreview))
        {
            RefreshOutlineSource();
        }
    }

    private void RefreshOutlineSource()
    {
        if (Outline is not { } outline)
        {
            return;
        }

        var document = IsEditMode && EditorSession is not null ? EditorSession.RenderedPreview : RenderedDocument;
        outline.Update(document, OutlinePreferences);
    }

    private void RaiseOutlineLayoutProperties()
    {
        OnPropertyChanged(nameof(ShowsOutline));
        OnPropertyChanged(nameof(Outline));
        OnPropertyChanged(nameof(OutlineLeftContent));
        OnPropertyChanged(nameof(OutlineRightContent));
        OnPropertyChanged(nameof(HidesMinimapForOutline));
        OnPropertyChanged(nameof(IsOutlineToggleChecked));
    }
}
```

- [ ] **Step 4: Подключить к `ShellViewModel.cs`**
- В конце конструктора `ShellViewModel` добавить вызов `InitializeOutline();`.
- В `InitializeAsync()` сразу после `ReadingPreferences = await _settings.LoadPreferencesAsync()...;` добавить `await LoadOutlinePreferencesAsync().ConfigureAwait(true);`.
- Проверить: `grep -n "partial void OnRenderedDocumentChanged\|partial void OnIsEditModeChanged" src/MarkMello.Presentation/ViewModels/*.cs`. Эти методы уже есть или могут быть. Мы их **не** используем, подписка идёт через `PropertyChanged`, чтобы не вмешиваться в существующие партиалы.

- [ ] **Step 5: Заглушка для Task 6.** `RefreshOutlineSettingsProperties()` вызывается выше, но определяется в Task 6. Чтобы сборка шла уже сейчас, создать `ShellViewModel.OutlineSettings.cs` с минимумом:

```csharp
namespace MarkMello.Presentation.ViewModels;

public partial class ShellViewModel
{
    private void RefreshOutlineSettingsProperties()
    {
    }
}
```

- [ ] **Step 6: Тесты проходят**

Run: `dotnet test tests/MarkMello.Presentation.Tests --filter "FullyQualifiedName~ShellViewModelTests"`
Expected: PASS, включая существующие тесты shell'а.

- [ ] **Step 7: Commit**

```bash
git add src/MarkMello.Presentation/ViewModels tests/MarkMello.Presentation.Tests/ShellViewModelTests.Outline.cs
git commit -m ":sparkles: feat(outline): wire outline state into shell view model" -m "Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>"
```

---

### Task 6: Свойства блока настроек и локализация

**Files:**
- Create: `src/MarkMello.Presentation/ViewModels/OutlineLevelSetting.cs`
- Modify: `src/MarkMello.Presentation/ViewModels/ShellViewModel.OutlineSettings.cs` (заменить заглушку)
- Create: `src/MarkMello.Presentation/Localization/LocalizationService.Outline.cs`
- Modify: место, где вызывается `AddAdvancedReadingStrings()` (найти: `grep -rn "AddAdvancedReadingStrings()" src/MarkMello.Presentation/Localization`), добавить рядом `AddOutlineStrings();`
- Modify: `src/MarkMello.Presentation/ViewModels/ShellViewModel.LocalizedRefresh.cs`, добавить вызов `RefreshOutlineSettingsProperties();` туда же, где обновляются остальные локализованные свойства
- Test: дописать в `tests/MarkMello.Presentation.Tests/ShellViewModelTests.Outline.cs`

- [ ] **Step 1: Падающие тесты**

```csharp
    [Fact]
    public void LevelSettingsChangeFontSizeAndMaxLevel()
    {
        var shell = CreateHarness().ViewModel;
        var h2 = shell.OutlineLevelSettings[1];

        h2.IncreaseFontSizeCommand.Execute(null);
        shell.OutlineLevelSettings[4].IsMaxLevel = true;

        Assert.Equal(15, shell.OutlinePreferences.H2FontSize);
        Assert.Equal(5, shell.OutlinePreferences.MaxLevel);
        Assert.False(shell.OutlineLevelSettings[5].IsWithinMaxLevel);
        Assert.Equal("15 px", h2.FontSizeLabel);
    }

    [Fact]
    public void LineHeightStepsAndSideSelection()
    {
        var shell = CreateHarness().ViewModel;

        shell.IncreaseOutlineLineHeightCommand.Execute(null);
        shell.IsOutlineRightSelected = true;

        Assert.Equal(1.6, shell.OutlinePreferences.LineHeight);
        Assert.Equal(OutlinePanelSide.Right, shell.OutlinePreferences.Side);
        Assert.False(shell.IsOutlineLeftSelected);
    }

    [Fact]
    public void OutlineStringsExistInBothLanguages()
    {
        foreach (var key in new[]
                 {
                     "OutlineHeader", "OutlineEmpty", "OutlineToggleTooltip", "OutlineSettingsHeader",
                     "OutlineSideLabel", "OutlineSideHint", "OutlineSideLeft", "OutlineSideRight",
                     "OutlineMaxLevelLabel", "OutlineMaxLevelHint", "OutlineLineHeightLabel",
                     "OutlineLineHeightHint", "OutlineLevelSizesLabel", "OutlineLevelSizesHint"
                 })
        {
            Assert.True(MarkMello.Presentation.Localization.LocalizationService.HasKeyInAllLanguages(key), key);
        }
    }
```

> Если в `LocalizationService` нет метода проверки ключей, найти, как это делает `tests/MarkMello.Presentation.Tests/LocalizationServiceTests.cs`, и использовать тот же способ вместо `HasKeyInAllLanguages`.

- [ ] **Step 2: Тесты падают** (FAIL компиляции).

- [ ] **Step 3: `OutlineLevelSetting.cs`** (по образцу `DocumentFontSetting`):

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarkMello.Domain;

namespace MarkMello.Presentation.ViewModels;

/// <summary>Строка уровня в блоке «Оглавление»: сегмент глубины и размер шрифта.</summary>
public sealed partial class OutlineLevelSetting : ObservableObject
{
    private readonly Func<OutlinePreferences> _get;
    private readonly Action<OutlinePreferences> _apply;

    public OutlineLevelSetting(int level, Func<OutlinePreferences> get, Action<OutlinePreferences> apply)
    {
        Level = level;
        _get = get;
        _apply = apply;
    }

    public int Level { get; }

    public string LevelLabel => $"H{Level}";

    public int FontSize => _get().GetFontSize(Level);

    public string FontSizeLabel => $"{FontSize} px";

    public bool IsWithinMaxLevel => Level <= _get().MaxLevel;

    public bool IsMaxLevel
    {
        get => _get().MaxLevel == Level;
        set
        {
            if (value)
            {
                _apply(_get() with { MaxLevel = Level });
            }
        }
    }

    [RelayCommand]
    private void IncreaseFontSize() => _apply(_get().WithFontSize(Level, FontSize + 1));

    [RelayCommand]
    private void DecreaseFontSize() => _apply(_get().WithFontSize(Level, FontSize - 1));

    public void Refresh()
    {
        OnPropertyChanged(nameof(FontSize));
        OnPropertyChanged(nameof(FontSizeLabel));
        OnPropertyChanged(nameof(IsWithinMaxLevel));
        OnPropertyChanged(nameof(IsMaxLevel));
    }
}
```

- [ ] **Step 4: `ShellViewModel.OutlineSettings.cs`** (полностью заменить заглушку):

```csharp
using CommunityToolkit.Mvvm.Input;
using MarkMello.Domain;

namespace MarkMello.Presentation.ViewModels;

public partial class ShellViewModel
{
    private IReadOnlyList<OutlineLevelSetting>? _outlineLevelSettings;

    public IReadOnlyList<OutlineLevelSetting> OutlineLevelSettings => _outlineLevelSettings ??=
        Enumerable.Range(OutlinePreferences.MinLevel, OutlinePreferences.MaxLevelLimit)
            .Select(level => new OutlineLevelSetting(level, () => OutlinePreferences, ApplyOutlinePreferences))
            .ToArray();

    public string OutlineHeader => _localization["OutlineHeader"];
    public string OutlineEmpty => _localization["OutlineEmpty"];
    public string OutlineSettingsHeader => _localization["OutlineSettingsHeader"];
    public string OutlineSideLabel => _localization["OutlineSideLabel"];
    public string OutlineSideHint => _localization["OutlineSideHint"];
    public string OutlineSideLeft => _localization["OutlineSideLeft"];
    public string OutlineSideRight => _localization["OutlineSideRight"];
    public string OutlineMaxLevelLabel => _localization["OutlineMaxLevelLabel"];
    public string OutlineMaxLevelHint => _localization["OutlineMaxLevelHint"];
    public string OutlineLineHeightLabel => _localization["OutlineLineHeightLabel"];
    public string OutlineLineHeightHint => _localization["OutlineLineHeightHint"];
    public string OutlineLevelSizesLabel => _localization["OutlineLevelSizesLabel"];
    public string OutlineLevelSizesHint => _localization["OutlineLevelSizesHint"];

    public string OutlineLineHeightValueLabel => OutlinePreferences.LineHeight.ToString("0.0", _localization.Culture);

    public bool IsOutlineLeftSelected
    {
        get => OutlinePreferences.Side == OutlinePanelSide.Left;
        set
        {
            if (value)
            {
                ApplyOutlinePreferences(OutlinePreferences with { Side = OutlinePanelSide.Left });
            }
        }
    }

    public bool IsOutlineRightSelected
    {
        get => OutlinePreferences.Side == OutlinePanelSide.Right;
        set
        {
            if (value)
            {
                ApplyOutlinePreferences(OutlinePreferences with { Side = OutlinePanelSide.Right });
            }
        }
    }

    [RelayCommand]
    private void IncreaseOutlineLineHeight()
        => ApplyOutlinePreferences(OutlinePreferences with
        {
            LineHeight = OutlinePreferences.LineHeight + OutlinePreferences.LineHeightStep
        });

    [RelayCommand]
    private void DecreaseOutlineLineHeight()
        => ApplyOutlinePreferences(OutlinePreferences with
        {
            LineHeight = OutlinePreferences.LineHeight - OutlinePreferences.LineHeightStep
        });

    private void RefreshOutlineSettingsProperties()
    {
        OnPropertyChanged(nameof(OutlineHeader));
        OnPropertyChanged(nameof(OutlineEmpty));
        OnPropertyChanged(nameof(OutlineToggleTooltip));
        OnPropertyChanged(nameof(OutlineSettingsHeader));
        OnPropertyChanged(nameof(OutlineSideLabel));
        OnPropertyChanged(nameof(OutlineSideHint));
        OnPropertyChanged(nameof(OutlineSideLeft));
        OnPropertyChanged(nameof(OutlineSideRight));
        OnPropertyChanged(nameof(OutlineMaxLevelLabel));
        OnPropertyChanged(nameof(OutlineMaxLevelHint));
        OnPropertyChanged(nameof(OutlineLineHeightLabel));
        OnPropertyChanged(nameof(OutlineLineHeightHint));
        OnPropertyChanged(nameof(OutlineLevelSizesLabel));
        OnPropertyChanged(nameof(OutlineLevelSizesHint));
        OnPropertyChanged(nameof(OutlineLineHeightValueLabel));
        OnPropertyChanged(nameof(IsOutlineLeftSelected));
        OnPropertyChanged(nameof(IsOutlineRightSelected));
        if (_outlineLevelSettings is not null)
        {
            foreach (var setting in _outlineLevelSettings)
            {
                setting.Refresh();
            }
        }
    }
}
```

- [ ] **Step 5: `LocalizationService.Outline.cs`.** Тексты взяты из ADR и макета:

```csharp
namespace MarkMello.Presentation.Localization;

public sealed partial class LocalizationService
{
    private static void AddOutlineStrings()
    {
        English["OutlineHeader"] = "Outline";
        English["OutlineEmpty"] = "No headings in this document";
        English["OutlineToggleTooltip"] = "Outline (Ctrl+Shift+B)";
        English["OutlineSettingsHeader"] = "OUTLINE";
        English["OutlineSideLabel"] = "Panel side";
        English["OutlineSideHint"] = "Where to show the outline";
        English["OutlineSideLeft"] = "Left";
        English["OutlineSideRight"] = "Right";
        English["OutlineMaxLevelLabel"] = "Show up to level";
        English["OutlineMaxLevelHint"] = "Deeper headings are hidden";
        English["OutlineLineHeightLabel"] = "Line spacing";
        English["OutlineLineHeightHint"] = "Space between items";
        English["OutlineLevelSizesLabel"] = "Sizes by level";
        English["OutlineLevelSizesHint"] = "11–20 px for each level";

        Russian["OutlineHeader"] = "Оглавление";
        Russian["OutlineEmpty"] = "В документе нет заголовков";
        Russian["OutlineToggleTooltip"] = "Оглавление (Ctrl+Shift+B)";
        Russian["OutlineSettingsHeader"] = "ОГЛАВЛЕНИЕ";
        Russian["OutlineSideLabel"] = "Сторона панели";
        Russian["OutlineSideHint"] = "Где показывать оглавление";
        Russian["OutlineSideLeft"] = "Слева";
        Russian["OutlineSideRight"] = "Справа";
        Russian["OutlineMaxLevelLabel"] = "Показывать до уровня";
        Russian["OutlineMaxLevelHint"] = "Более глубокие заголовки скрыты";
        Russian["OutlineLineHeightLabel"] = "Межстрочный интервал";
        Russian["OutlineLineHeightHint"] = "Расстояние между пунктами";
        Russian["OutlineLevelSizesLabel"] = "Размеры по уровням";
        Russian["OutlineLevelSizesHint"] = "11–20 px для каждого уровня";
    }
}
```

На macOS подсказка должна показывать Cmd. Посмотреть, как это решено для других горячих клавиш (`grep -rn "Ctrl+" src/MarkMello.Presentation/Localization`), и сделать так же.

- [ ] **Step 6: Тесты проходят** (`dotnet test tests/MarkMello.Presentation.Tests --filter "FullyQualifiedName~ShellViewModelTests|FullyQualifiedName~Localization"`).

- [ ] **Step 7: Commit** (`:sparkles: feat(outline): add outline settings properties and strings`).

---

### Task 7: Контракт навигации и viewer

**Files:**
- Create: `src/MarkMello.Presentation/Views/IOutlineNavigationHost.cs`
- Create: `src/MarkMello.Presentation/Views/MarkdownDocumentView.Outline.cs`
- Create: `src/MarkMello.Presentation/Views/ViewerView.Outline.cs`
- Modify: `src/MarkMello.Presentation/Views/ViewerView.axaml.cs`: объявление класса и `OnScrollChanged`
- Test: `tests/MarkMello.Presentation.Tests/MarkdownDocumentViewOutlineTests.cs`

- [ ] **Step 1: Интерфейс**

```csharp
namespace MarkMello.Presentation.Views;

/// <summary>
/// Навигация оглавления, реализуемая активным контентом (reader или editor) —
/// по тому же принципу, что <see cref="IFindHost"/>. Строки — zero-based строки исходника.
/// </summary>
public interface IOutlineNavigationHost
{
    /// <summary>Строка исходника текущего места: верх видимой области или строка курсора.</summary>
    int? CurrentSourceLine { get; }

    event EventHandler? CurrentSourceLineChanged;

    bool TryNavigateToSourceLine(int sourceLine);
}
```

- [ ] **Step 2: Падающий тест** (headless, по образцу `MarkdownDocumentViewTests`):

```csharp
using Avalonia.Controls;
using MarkMello.Domain;
using MarkMello.Presentation.Views;

namespace MarkMello.Presentation.Tests;

[Collection(AvaloniaHeadlessTestGroup.Name)]
public sealed class MarkdownDocumentViewOutlineTests(AvaloniaHeadlessFixture fixture)
{
    [Fact]
    public Task ScrollsToHeadingBySourceLineAndReportsIt()
    {
        return fixture.RunAsync(() =>
        {
            var blocks = new List<MarkdownBlock>();
            for (var i = 0; i < 40; i++)
            {
                blocks.Add(new MarkdownHeadingBlock(2, [new MarkdownTextInline($"Section {i}")])
                    { SourceSpan = new MarkdownSourceSpan(i * 10) });
                blocks.Add(new MarkdownParagraphBlock([new MarkdownTextInline("text text text")])
                    { SourceSpan = new MarkdownSourceSpan(i * 10 + 2) });
            }

            var view = new MarkdownDocumentView { Document = new RenderedMarkdownDocument(blocks) };
            var scroll = new ScrollViewer { Content = view, Height = 300 };
            var window = new Window { Width = 600, Height = 300, Content = scroll };
            window.Show();
            window.UpdateLayout();

            Assert.True(view.TryScrollToHeadingSourceLine(200));
            window.UpdateLayout();

            Assert.Equal(200, view.GetCurrentHeadingSourceLine());
            Assert.False(view.TryScrollToHeadingSourceLine(7));
        });
    }
}
```

> Если `MarkdownDocumentView` в существующих тестах создаётся иначе (через helper), повторить их способ: `grep -n "new MarkdownDocumentView" tests/MarkMello.Presentation.Tests/*.cs`.

- [ ] **Step 3: Тест падает** (FAIL компиляции).

- [ ] **Step 4: `MarkdownDocumentView.Outline.cs`**

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace MarkMello.Presentation.Views;

public sealed partial class MarkdownDocumentView
{
    /// <summary>Отступ сверху, как у якорных переходов: заголовок «текущий», когда дошёл до этой линии.</summary>
    private const double OutlineActivationInset = 32;

    internal bool TryScrollToHeadingSourceLine(int sourceLine)
    {
        foreach (var (block, control) in _headingAnchorRegistrations)
        {
            if (block.SourceSpan?.StartLine == sourceLine)
            {
                return TryScrollTargetIntoView(control);
            }
        }

        return false;
    }

    internal int? GetCurrentHeadingSourceLine()
    {
        var scrollViewer = this.FindAncestorOfType<ScrollViewer>();
        if (scrollViewer is null)
        {
            return null;
        }

        int? current = null;
        foreach (var (block, control) in _headingAnchorRegistrations)
        {
            if (block.SourceSpan is not { } span
                || control.TranslatePoint(new Point(0, 0), scrollViewer) is not { } point)
            {
                continue;
            }

            if (point.Y > OutlineActivationInset)
            {
                break;
            }

            current = span.StartLine;
        }

        return current;
    }
}
```

- [ ] **Step 5: `ViewerView` как хост.** В `ViewerView.axaml.cs` поменять объявление на `public partial class ViewerView : UserControl, IFindHost, IOutlineNavigationHost`. В конец `OnScrollChanged(...)` (строка ~302) добавить `UpdateOutlineCurrentLine();`. Создать `ViewerView.Outline.cs`:

```csharp
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
```

> Если документ отрисован, но прокрутки не было, событие не придёт. Подписаться на уже существующее у `MarkdownDocumentView` событие окончания отрисовки (`grep -n "event EventHandler" src/MarkMello.Presentation/Views/MarkdownDocumentView*.cs`, в `EditWorkspaceView` используется `DocumentRendered`) и тоже вызвать `UpdateOutlineCurrentLine()`.

- [ ] **Step 6: Тест проходит** (`--filter FullyQualifiedName~MarkdownDocumentViewOutlineTests`).

- [ ] **Step 7: Commit** (`:sparkles: feat(outline): navigate viewer to headings by source line`).

---

### Task 8: Редактор как хост навигации

**Files:**
- Create: `src/MarkMello.Presentation/Views/EditWorkspaceView.Outline.cs`
- Modify: `src/MarkMello.Presentation/Views/EditWorkspaceView.axaml.cs`: объявление класса и место присваивания `_editorTextBox`

- [ ] **Step 1: Реализация**

Объявление: `public partial class EditWorkspaceView : UserControl, IFindHost, IOutlineNavigationHost`.

`EditWorkspaceView.Outline.cs`:
```csharp
using Avalonia;
using Avalonia.Controls;

namespace MarkMello.Presentation.Views;

public partial class EditWorkspaceView
{
    private int? _outlineCaretLine;

    public int? CurrentSourceLine => _outlineCaretLine;

    public event EventHandler? CurrentSourceLineChanged;

    /// <summary>ADR-0008 Rule 6: курсор в начало строки заголовка, строка наверх, фокус в редактор.</summary>
    public bool TryNavigateToSourceLine(int sourceLine)
    {
        if (_editorTextBox is null)
        {
            return false;
        }

        var text = _editorTextBox.Text ?? string.Empty;
        var index = GetLineStartCharacterIndex(text, sourceLine);
        _editorTextBox.SelectionStart = index;
        _editorTextBox.SelectionEnd = index;
        _editorTextBox.CaretIndex = index;
        ScrollEditorToOffset(index);
        FocusEditorAsync();
        return true;
    }

    private void OnEditorTextBoxPropertyChangedForOutline(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != TextBox.CaretIndexProperty || _editorTextBox is null)
        {
            return;
        }

        var line = GetSourceLineFromCharacterIndex(_editorTextBox.Text ?? string.Empty, _editorTextBox.CaretIndex);
        if (line == _outlineCaretLine)
        {
            return;
        }

        _outlineCaretLine = line;
        CurrentSourceLineChanged?.Invoke(this, EventArgs.Empty);
    }
}
```

Найти все места присваивания: `grep -n "_editorTextBox =" src/MarkMello.Presentation/Views/EditWorkspaceView.axaml.cs`.
- После каждого присваивания ненулевого значения добавить `_editorTextBox.PropertyChanged += OnEditorTextBoxPropertyChangedForOutline;`.
- Перед каждым обнулением или переприсваиванием (например, в `DetachScrollSynchronization`) добавить отписку `-=`.

- [ ] **Step 2: Сборка**

Run: `dotnet build MarkMello.sln`
Expected: 0 warnings, 0 errors.

- [ ] **Step 3: Регресс по scroll sync**

Run: `dotnet test tests/MarkMello.Presentation.Tests --filter "FullyQualifiedName~EditModeScrollSync|FullyQualifiedName~EditorSession"`
Expected: PASS.

- [ ] **Step 4: Commit** (`:sparkles: feat(outline): move editor caret to heading line`).

---

### Task 9: `OutlinePanelView`

**Files:**
- Create: `src/MarkMello.Presentation/Views/OutlinePanelView.axaml`
- Create: `src/MarkMello.Presentation/Views/OutlinePanelView.axaml.cs`

- [ ] **Step 1: Разметка.** Визуальный язык копирует `WorkspaceSidebarView.axaml`: те же кисти, `TreeLevelIndentConverter`, стиль активной планки. Длина файла должна остаться меньше 180 строк.

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MarkMello.Presentation.ViewModels"
             xmlns:views="using:MarkMello.Presentation.Views"
             x:Class="MarkMello.Presentation.Views.OutlinePanelView"
             x:DataType="vm:ShellViewModel">

  <UserControl.Styles>
    <Style Selector="TextBlock.mm-outline-header">
      <Setter Property="FontFamily" Value="{DynamicResource MmDocumentSansFontFamily}" />
      <Setter Property="FontSize" Value="{DynamicResource MmUiFontSize12}" />
      <Setter Property="FontWeight" Value="SemiBold" />
      <Setter Property="Foreground" Value="{DynamicResource MmTextSoftBrush}" />
      <Setter Property="VerticalAlignment" Value="Center" />
    </Style>
    <Style Selector="TextBlock.mm-outline-empty">
      <Setter Property="FontFamily" Value="{DynamicResource MmDocumentSansFontFamily}" />
      <Setter Property="FontSize" Value="{DynamicResource MmUiFontSize12_5}" />
      <Setter Property="Foreground" Value="{DynamicResource MmTextFaintBrush}" />
      <Setter Property="TextWrapping" Value="Wrap" />
    </Style>
    <Style Selector="TreeView.mm-outline-tree">
      <Setter Property="Background" Value="Transparent" />
      <Setter Property="BorderThickness" Value="0" />
      <Setter Property="Padding" Value="8,2,8,12" />
      <Setter Property="ItemsPanel">
        <ItemsPanelTemplate><VirtualizingStackPanel /></ItemsPanelTemplate>
      </Setter>
    </Style>
    <Style Selector="TreeView.mm-outline-tree TreeViewItem">
      <Setter Property="IsExpanded" Value="{CompiledBinding IsExpanded, Mode=TwoWay, DataType=vm:OutlineItemViewModel}" />
      <Setter Property="Padding" Value="0" />
      <Setter Property="CornerRadius" Value="4" />
      <Setter Property="MinHeight" Value="0" />
      <Setter Property="Foreground" Value="{DynamicResource MmTextBrush}" />
    </Style>
    <Style Selector="TreeView.mm-outline-tree TreeViewItem /template/ Panel#PART_ExpandCollapseChevronContainer">
      <Setter Property="Width" Value="{CompiledBinding HasChildren, DataType=vm:OutlineItemViewModel, Converter={x:Static views:TreeChevronWidthConverter.Instance}}" />
      <Setter Property="Margin" Value="{Binding $parent[TreeViewItem].Level, Converter={x:Static views:TreeLevelIndentConverter.Instance}}" />
    </Style>
    <Style Selector="TreeView.mm-outline-tree TreeViewItem /template/ Border#PART_LayoutRoot">
      <Setter Property="Background" Value="Transparent" />
      <Setter Property="CornerRadius" Value="4" />
    </Style>
    <Style Selector="TreeView.mm-outline-tree TreeViewItem:pointerover /template/ Border#PART_LayoutRoot">
      <Setter Property="Background" Value="{DynamicResource MmSurfaceHoverBrush}" />
    </Style>
    <Style Selector="TreeView.mm-outline-tree TreeViewItem:selected /template/ Border#PART_LayoutRoot">
      <Setter Property="Background" Value="Transparent" />
    </Style>
    <Style Selector="TextBlock.mm-outline-label">
      <Setter Property="FontFamily" Value="{DynamicResource MmDocumentSansFontFamily}" />
      <Setter Property="Foreground" Value="{DynamicResource MmTextBrush}" />
      <Setter Property="TextTrimming" Value="CharacterEllipsis" />
      <Setter Property="VerticalAlignment" Value="Center" />
    </Style>
    <Style Selector="TextBlock.mm-outline-label.top">
      <Setter Property="FontWeight" Value="SemiBold" />
    </Style>
    <Style Selector="TextBlock.mm-outline-label.active">
      <Setter Property="FontWeight" Value="Medium" />
    </Style>
    <Style Selector="Border.mm-outline-active-bar">
      <Setter Property="Width" Value="2" />
      <Setter Property="Margin" Value="-6,4,0,4" />
      <Setter Property="CornerRadius" Value="1" />
      <Setter Property="HorizontalAlignment" Value="Left" />
      <Setter Property="Background" Value="{DynamicResource MmAccentBrush}" />
    </Style>
  </UserControl.Styles>

  <DockPanel>
    <Border DockPanel.Dock="Top" Height="44" Padding="16,0">
      <TextBlock Classes="mm-outline-header" Text="{Binding OutlineHeader}" />
    </Border>

    <TextBlock DockPanel.Dock="Top"
               Classes="mm-outline-empty"
               Margin="16,4,16,0"
               IsVisible="{Binding Outline.IsEmpty, FallbackValue=False}"
               Text="{Binding OutlineEmpty}" />

    <TreeView Name="OutlineTree"
              Classes="mm-outline-tree"
              ItemsSource="{Binding Outline.Items}">
      <TreeView.ItemTemplate>
        <TreeDataTemplate DataType="vm:OutlineItemViewModel" ItemsSource="{Binding Children}">
          <Panel MinHeight="{Binding LineBoxHeight}">
            <Border Classes="mm-outline-active-bar" IsVisible="{Binding IsActive}" />
            <TextBlock Classes="mm-outline-label"
                       Classes.top="{Binding IsTopLevel}"
                       Classes.active="{Binding IsActive}"
                       FontSize="{Binding FontSize}"
                       Text="{Binding Text}"
                       ToolTip.Tip="{Binding Text}"
                       Tapped="OnItemTapped" />
          </Panel>
        </TreeDataTemplate>
      </TreeView.ItemTemplate>
    </TreeView>
  </DockPanel>
</UserControl>
```

> `TreeChevronWidthConverter` в сайдбаре привязан к `IsDirectory`. Здесь передаётся `HasChildren` (bool). Проверить, что конвертер принимает bool (`grep -n "class TreeChevronWidthConverter" -A15 -r src/MarkMello.Presentation`). Если он типизирован иначе, создать рядом `OutlineChevronWidthConverter` с той же логикой.

> DataType панели — `ShellViewModel`: в DataTemplate MainWindow контент `OutlineLeftContent`/`OutlineRightContent` равен `Outline`, поэтому в Task 10 DataContext выставляется на shell (см. там).

- [ ] **Step 2: Code-behind**

```csharp
using Avalonia.Controls;
using Avalonia.Input;
using MarkMello.Presentation.ViewModels;

namespace MarkMello.Presentation.Views;

public partial class OutlinePanelView : UserControl
{
    public OutlinePanelView()
    {
        InitializeComponent();
    }

    private void OnItemTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control { DataContext: OutlineItemViewModel item }
            && DataContext is ShellViewModel { Outline: { } outline })
        {
            outline.NavigateCommand.Execute(item);
            e.Handled = true;
        }
    }
}
```

- [ ] **Step 3: Сборка** (`dotnet build MarkMello.sln`, 0/0).

- [ ] **Step 4: Commit** (`:sparkles: feat(outline): add outline panel view`).

---

### Task 10: Размещение в `MainWindow`, кнопка, горячая клавиша, связь с хостом

**Files:**
- Modify: `src/MarkMello.Presentation/Views/MainWindow.axaml` (`KeyBindings` ~стр. 30–53, `SidebarLayout` ~стр. 232–265, плавающая группа ~стр. 294–330)
- Create: `src/MarkMello.Presentation/Views/MainWindow.Outline.cs`
- Modify: `src/MarkMello.Presentation/Views/MainWindow.ViewModelChanges.cs`
- Modify: `src/MarkMello.Presentation/Views/ViewerView.axaml.cs`: `ShouldShowMinimap()` и обработчик изменений VM

- [ ] **Step 1: Горячая клавиша.** После `Cmd+B` добавить:

```xml
    <KeyBinding Gesture="Ctrl+Shift+B" Command="{Binding ToggleOutlineCommand}" />
    <KeyBinding Gesture="Cmd+Shift+B" Command="{Binding ToggleOutlineCommand}" />
```

- [ ] **Step 2: Колонки.** У `SidebarLayout` заменить `Grid.ColumnDefinitions`:

```xml
        <Grid.ColumnDefinitions>
          <ColumnDefinition Width="Auto" MinWidth="0" MaxWidth="340" />  <!-- 0 файлы -->
          <ColumnDefinition Width="0" />                                 <!-- 1 splitter файлов -->
          <ColumnDefinition Width="0" MinWidth="0" MaxWidth="340" />     <!-- 2 оглавление слева -->
          <ColumnDefinition Width="0" />                                 <!-- 3 splitter слева -->
          <ColumnDefinition Width="*" />                                 <!-- 4 документ -->
          <ColumnDefinition Width="0" />                                 <!-- 5 splitter справа -->
          <ColumnDefinition Width="0" MinWidth="0" MaxWidth="340" />     <!-- 6 оглавление справа -->
        </Grid.ColumnDefinitions>
```

Существующий `<Grid Grid.Column="2" RowDefinitions="Auto,*">` (вкладки и тело) перевести на `Grid.Column="4"`. После splitter'а файлов вставить:

```xml
        <ContentControl Grid.Column="2" Content="{Binding OutlineLeftContent}">
          <ContentControl.ContentTemplate>
            <DataTemplate x:DataType="vm:OutlineViewModel">
              <Border Background="{DynamicResource MmSurfaceBrush}"
                      BorderBrush="{DynamicResource MmBorderBrush}"
                      BorderThickness="0,0,1,0">
                <views:OutlinePanelView DataContext="{Binding $parent[Window].DataContext}" />
              </Border>
            </DataTemplate>
          </ContentControl.ContentTemplate>
        </ContentControl>

        <GridSplitter Grid.Column="3"
                      Name="OutlineLeftSplitter"
                      Classes="mm-sidebar-splitter"
                      ZIndex="1"
                      IsVisible="{Binding OutlineLeftContent, Converter={x:Static ObjectConverters.IsNotNull}}"
                      DragCompleted="OnOutlineSplitterDragCompleted"
                      ResizeBehavior="PreviousAndNext"
                      ResizeDirection="Columns" />
```

После документа (перед закрытием `SidebarLayout`):

```xml
        <GridSplitter Grid.Column="5"
                      Name="OutlineRightSplitter"
                      Classes="mm-sidebar-splitter"
                      ZIndex="1"
                      IsVisible="{Binding OutlineRightContent, Converter={x:Static ObjectConverters.IsNotNull}}"
                      DragCompleted="OnOutlineSplitterDragCompleted"
                      ResizeBehavior="PreviousAndNext"
                      ResizeDirection="Columns" />

        <ContentControl Grid.Column="6" Content="{Binding OutlineRightContent}">
          <ContentControl.ContentTemplate>
            <DataTemplate x:DataType="vm:OutlineViewModel">
              <Border Background="{DynamicResource MmSurfaceBrush}"
                      BorderBrush="{DynamicResource MmBorderBrush}"
                      BorderThickness="1,0,0,0">
                <views:OutlinePanelView DataContext="{Binding $parent[Window].DataContext}" />
              </Border>
            </DataTemplate>
          </ContentControl.ContentTemplate>
        </ContentControl>
```

> Стиль `mm-sidebar-splitter` рисует подсветку у левого края зоны захвата (`Margin="3,0,0,0"`). Для правого splitter'а проверить вживую, что подсветка совпадает с границей панели. Если нет, добавить в `Themes/SidebarSplitter.axaml` класс-модификатор `.mm-splitter-right` с `HorizontalAlignment="Right"` у внутреннего Border.

- [ ] **Step 3: Кнопка.** У плавающей группы (`Border Classes="mm-top-chrome"` с `IsVisible="{Binding ShowsFloatingAppMenuButton}"`):
- `IsVisible` этого Border заменить на
```xml
                IsVisible="{MultiBinding Converter={x:Static BoolConverters.Or}, Bindings={Binding ShowsFloatingAppMenuButton}; {Binding ShowsFloatingOutlineButton}}"
```
  Если короткая форма MultiBinding не компилируется, записать её развёрнуто через `<Border.IsVisible><MultiBinding ...>`.
- Ставший независимым `ToggleButton Name="AppMenuTriggerButton"` получает `IsVisible="{Binding ShowsFloatingAppMenuButton}"`.
- После кнопки возврата сайдбара добавить:

```xml
            <ToggleButton Classes="topbar-ghost"
                          IsVisible="{Binding ShowsFloatingOutlineButton}"
                          IsChecked="{Binding IsOutlineToggleChecked, Mode=OneWay}"
                          Command="{Binding ToggleOutlineCommand}"
                          ToolTip.Tip="{Binding OutlineToggleTooltip}">
              <Grid Width="14" Height="14">
                <Path Data="M2 3.5H12 M4.5 7H12 M7 10.5H12"
                      Stroke="{DynamicResource MmTextSoftBrush}"
                      StrokeThickness="1.5"
                      StrokeLineCap="Round"
                      Fill="Transparent" />
              </Grid>
            </ToggleButton>
```

- [ ] **Step 4: `MainWindow.Outline.cs`**: ширина колонок, splitter'ы, связь с хостом.

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MarkMello.Domain;
using MarkMello.Presentation.ViewModels;

namespace MarkMello.Presentation.Views;

public partial class MainWindow
{
    private const int OutlineLeftColumn = 2;
    private const int OutlineRightColumn = 6;

    private OutlineViewModel? _attachedOutline;
    private IOutlineNavigationHost? _outlineHost;

    private void SyncOutlineColumns()
    {
        if (SidebarLayout is not { ColumnDefinitions: { Count: > OutlineRightColumn } columns })
        {
            return;
        }

        var prefs = _viewModel.OutlinePreferences;
        var left = _viewModel.OutlineLeftContent is not null;
        var right = _viewModel.OutlineRightContent is not null;

        SetOutlineColumn(columns[OutlineLeftColumn], left, prefs.Width);
        SetOutlineColumn(columns[OutlineRightColumn], right, prefs.Width);
        AttachOutline(_viewModel.Outline);
    }

    private static void SetOutlineColumn(ColumnDefinition column, bool visible, double width)
    {
        column.MinWidth = visible ? OutlinePreferences.MinWidth : 0;
        column.Width = visible ? new GridLength(OutlinePreferences.NormalizeWidth(width)) : new GridLength(0);
    }

    private void OnOutlineSplitterDragCompleted(object? sender, VectorEventArgs e)
    {
        if (SidebarLayout is not { ColumnDefinitions: { Count: > OutlineRightColumn } columns })
        {
            return;
        }

        var column = ReferenceEquals(sender, OutlineRightSplitter)
            ? columns[OutlineRightColumn]
            : columns[OutlineLeftColumn];
        _viewModel.SetOutlineWidth(column.Width.Value);
    }

    private void AttachOutline(OutlineViewModel? outline)
    {
        if (ReferenceEquals(outline, _attachedOutline))
        {
            return;
        }

        if (_attachedOutline is not null)
        {
            _attachedOutline.NavigateRequested -= OnOutlineNavigateRequested;
        }

        _attachedOutline = outline;
        if (outline is not null)
        {
            outline.NavigateRequested += OnOutlineNavigateRequested;
            ScheduleOutlineHostRefresh();
        }
    }

    private void OnOutlineNavigateRequested(object? sender, int sourceLine)
        => ResolveOutlineHost()?.TryNavigateToSourceLine(sourceLine);

    /// <summary>
    /// Хост меняется при переключении viewer ↔ editor и вкладок; его шаблон создаётся
    /// позже смены свойства, поэтому поиск откладывается на следующий проход диспетчера.
    /// </summary>
    private void ScheduleOutlineHostRefresh()
        => Dispatcher.UIThread.Post(() =>
        {
            DetachOutlineHost();
            if (ResolveOutlineHost() is { } host)
            {
                _attachedOutline?.SetCurrentSourceLine(host.CurrentSourceLine);
            }
        }, DispatcherPriority.Loaded);

    private IOutlineNavigationHost? ResolveOutlineHost()
    {
        if (_outlineHost is Visual cached && cached.IsAttachedToVisualTree())
        {
            return _outlineHost;
        }

        DetachOutlineHost();
        _outlineHost = this.FindControl<Panel>("BodyPanel")?
            .GetVisualDescendants()
            .OfType<IOutlineNavigationHost>()
            .FirstOrDefault();
        if (_outlineHost is not null)
        {
            _outlineHost.CurrentSourceLineChanged += OnOutlineHostLineChanged;
        }

        return _outlineHost;
    }

    private void DetachOutlineHost()
    {
        if (_outlineHost is not null)
        {
            _outlineHost.CurrentSourceLineChanged -= OnOutlineHostLineChanged;
            _outlineHost = null;
        }
    }

    private void OnOutlineHostLineChanged(object? sender, EventArgs e)
        => _attachedOutline?.SetCurrentSourceLine(_outlineHost?.CurrentSourceLine);
}
```

> `OutlineRightSplitter` — поле, сгенерированное из `Name`, так же как `SidebarLayout`. Если генератор полей в проекте выключен, получить через `this.FindControl<GridSplitter>("OutlineRightSplitter")`.

- [ ] **Step 5: Реакция на изменения VM.** В `MainWindow.ViewModelChanges.cs`, в `OnViewModelPropertyChanged`, перед блоком про `ShellOverlay` добавить:

```csharp
        if (e.PropertyName is nameof(ShellViewModel.OutlineLeftContent)
            or nameof(ShellViewModel.OutlineRightContent)
            or nameof(ShellViewModel.OutlinePreferences))
        {
            SyncOutlineColumns();
            return;
        }

        if (e.PropertyName is nameof(ShellViewModel.ActiveDocumentContent)
            or nameof(ShellViewModel.IsEditMode))
        {
            ScheduleOutlineHostRefresh();
        }
```

Убедиться, что `ActiveDocumentContent` действительно поднимает `PropertyChanged` (`grep -n "nameof(ActiveDocumentContent)" src/MarkMello.Presentation/ViewModels/*.cs`). Если нет, реагировать на `EditorSession` и `RenderedDocument`. Также вызвать `SyncOutlineColumns()` один раз там, где при старте вызывается `SyncSidebarColumn()` (`grep -n "SyncSidebarColumn()" src/MarkMello.Presentation/Views/MainWindow.axaml.cs`).

- [ ] **Step 6: Миникарта.** В `ViewerView.ShouldShowMinimap()` первой строкой после проверки `_scroll is null` добавить:

```csharp
        if (DataContext is ShellViewModel { HidesMinimapForOutline: true })
        {
            return false;
        }
```

В обработчике изменений VM у `ViewerView` (`grep -n "PropertyChanged" src/MarkMello.Presentation/Views/ViewerView.axaml.cs`) там, где реагируют на `ReadingPreferences`, добавить ветку для `nameof(ShellViewModel.HidesMinimapForOutline)`, которая вызывает `UpdateMinimapVisibility()`.

- [ ] **Step 7: Сборка и регресс окна**

Run:
```bash
dotnet build MarkMello.sln
dotnet test tests/MarkMello.Presentation.Tests --filter "FullyQualifiedName~MainWindow|FullyQualifiedName~DocumentTabsShell|FullyQualifiedName~DocumentMinimap"
```
Expected: 0/0 и PASS. Тест, который проверял индексы колонок `SidebarLayout` (если такой есть), обновить под новую раскладку и указать это в сообщении коммита.

- [ ] **Step 8: Commit** (`:sparkles: feat(outline): place outline panel in main window`).

---

### Task 11: Блок настроек

**Files:**
- Create: `src/MarkMello.Presentation/Views/OutlineSettingsView.axaml(.cs)`
- Modify: `src/MarkMello.Presentation/Views/AdvancedReadingSettingsWindow.axaml` (после `<views:InterfaceFontSizeSettingsView />`)

- [ ] **Step 1: Разметка.** Классы из существующих настроек: `mm-setting-row`, `mm-setting-label`, `mm-setting-hint`, `mm-segmented`, `mm-segmented-item`, `mm-setting-divider`. Блок «Размеры по уровням» сделан `Expander`'ом по умолчанию раскрытым. Если у проекта нет стиля `Expander`, сделать `ToggleButton` и `StackPanel` с `IsVisible`.

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MarkMello.Presentation.ViewModels"
             x:Class="MarkMello.Presentation.Views.OutlineSettingsView"
             x:DataType="vm:ShellViewModel">
  <StackPanel Spacing="0">
    <Line Classes="mm-setting-divider" StartPoint="0,0.5" EndPoint="1,0.5" />
    <TextBlock Classes="mm-setting-hint" Margin="0,16,0,4" FontWeight="SemiBold" Text="{Binding OutlineSettingsHeader}" />

    <Border Classes="mm-setting-row">
      <Grid ColumnDefinitions="*,Auto" ColumnSpacing="12">
        <StackPanel Spacing="2" VerticalAlignment="Center">
          <TextBlock Classes="mm-setting-label" Text="{Binding OutlineSideLabel}" />
          <TextBlock Classes="mm-setting-hint" TextWrapping="Wrap" Text="{Binding OutlineSideHint}" />
        </StackPanel>
        <Border Grid.Column="1" Classes="mm-segmented">
          <StackPanel Orientation="Horizontal" Spacing="1">
            <ToggleButton Classes="mm-segmented-item" IsChecked="{Binding IsOutlineLeftSelected}">
              <TextBlock Text="{Binding OutlineSideLeft}" />
            </ToggleButton>
            <ToggleButton Classes="mm-segmented-item" IsChecked="{Binding IsOutlineRightSelected}">
              <TextBlock Text="{Binding OutlineSideRight}" />
            </ToggleButton>
          </StackPanel>
        </Border>
      </Grid>
    </Border>

    <Border Classes="mm-setting-row">
      <Grid ColumnDefinitions="*,Auto" ColumnSpacing="12">
        <StackPanel Spacing="2" VerticalAlignment="Center">
          <TextBlock Classes="mm-setting-label" Text="{Binding OutlineMaxLevelLabel}" />
          <TextBlock Classes="mm-setting-hint" TextWrapping="Wrap" Text="{Binding OutlineMaxLevelHint}" />
        </StackPanel>
        <Border Grid.Column="1" Classes="mm-segmented">
          <ItemsControl ItemsSource="{Binding OutlineLevelSettings}">
            <ItemsControl.ItemsPanel>
              <ItemsPanelTemplate><StackPanel Orientation="Horizontal" Spacing="1" /></ItemsPanelTemplate>
            </ItemsControl.ItemsPanel>
            <ItemsControl.ItemTemplate>
              <DataTemplate x:DataType="vm:OutlineLevelSetting">
                <ToggleButton Classes="mm-segmented-item" IsChecked="{Binding IsMaxLevel}">
                  <TextBlock Text="{Binding LevelLabel}" />
                </ToggleButton>
              </DataTemplate>
            </ItemsControl.ItemTemplate>
          </ItemsControl>
        </Border>
      </Grid>
    </Border>

    <Border Classes="mm-setting-row">
      <Grid ColumnDefinitions="*,Auto" ColumnSpacing="12">
        <StackPanel Spacing="2" VerticalAlignment="Center">
          <TextBlock Classes="mm-setting-label" Text="{Binding OutlineLineHeightLabel}" />
          <TextBlock Classes="mm-setting-hint" TextWrapping="Wrap" Text="{Binding OutlineLineHeightHint}" />
        </StackPanel>
        <StackPanel Grid.Column="1" Orientation="Horizontal" Spacing="4" VerticalAlignment="Center">
          <Button Classes="mm-stepper" Content="−" Command="{Binding DecreaseOutlineLineHeightCommand}" />
          <TextBlock Classes="mm-slider-value" MinWidth="40" TextAlignment="Center" Text="{Binding OutlineLineHeightValueLabel}" />
          <Button Classes="mm-stepper" Content="+" Command="{Binding IncreaseOutlineLineHeightCommand}" />
        </StackPanel>
      </Grid>
    </Border>

    <Expander IsExpanded="True">
      <Expander.Header>
        <StackPanel Spacing="2">
          <TextBlock Classes="mm-setting-label" Text="{Binding OutlineLevelSizesLabel}" />
          <TextBlock Classes="mm-setting-hint" Text="{Binding OutlineLevelSizesHint}" />
        </StackPanel>
      </Expander.Header>
      <ItemsControl ItemsSource="{Binding OutlineLevelSettings}">
        <ItemsControl.ItemTemplate>
          <DataTemplate x:DataType="vm:OutlineLevelSetting">
            <Grid ColumnDefinitions="*,Auto" Margin="12,4,0,4" Opacity="{Binding IsWithinMaxLevel, Converter={x:Static vm:OutlineOpacityConverter.Instance}}">
              <TextBlock Classes="mm-setting-label" VerticalAlignment="Center" FontSize="{Binding FontSize}" Text="{Binding LevelLabel}" />
              <StackPanel Grid.Column="1" Orientation="Horizontal" Spacing="4">
                <Button Classes="mm-stepper" Content="−" Command="{Binding DecreaseFontSizeCommand}" />
                <TextBlock Classes="mm-slider-value" MinWidth="48" TextAlignment="Center" VerticalAlignment="Center" Text="{Binding FontSizeLabel}" />
                <Button Classes="mm-stepper" Content="+" Command="{Binding IncreaseFontSizeCommand}" />
              </StackPanel>
            </Grid>
          </DataTemplate>
        </ItemsControl.ItemTemplate>
      </ItemsControl>
    </Expander>
  </StackPanel>
</UserControl>
```

- Класса кнопки `mm-stepper` в проекте может не быть (`grep -rn "mm-stepper" src/MarkMello.Presentation/Themes`). Если его нет, добавить стиль в `Themes/Controls.axaml`: квадрат 28×28, рамка `MmBorderBrush`, скругление 6, фон `MmElevatedBackgroundBrush`, как на макете.
- Добавить `OutlineOpacityConverter` (bool → 1.0 / 0.5) в `src/MarkMello.Presentation/ViewModels/OutlineOpacityConverter.cs`:

```csharp
using System.Globalization;
using Avalonia.Data.Converters;

namespace MarkMello.Presentation.ViewModels;

/// <summary>Строки уровней глубже MaxLevel приглушены, но редактируемы (ADR-0008 Rule 11).</summary>
public sealed class OutlineOpacityConverter : IValueConverter
{
    public static OutlineOpacityConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? 1.0 : 0.5;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
```

Code-behind `OutlineSettingsView.axaml.cs`: стандартный конструктор с `InitializeComponent();`, как у `InterfaceFontSizeSettingsView`.

- [ ] **Step 2: Подключение.** В `AdvancedReadingSettingsWindow.axaml` после `<views:InterfaceFontSizeSettingsView />` добавить `<views:OutlineSettingsView />`.

- [ ] **Step 3: Headless-тест привязок** (дописать в `AdvancedReadingBindingsTests.cs`, по образцу первого теста):

```csharp
    [Fact]
    public Task OutlineSettingsSideToggleUpdatesPreferences()
    {
        return fixture.RunAsync(() =>
        {
            var shell = ShellViewModelTests.CreateAdvancedReadingTestShell();
            var view = new OutlineSettingsView { DataContext = shell };
            var window = new Window { Content = view };
            window.Show();

            var right = view.GetVisualDescendants().OfType<ToggleButton>()
                .First(b => b.Content is TextBlock { Text: var t } && t == shell.OutlineSideRight);
            right.IsChecked = true;

            Assert.Equal(OutlinePanelSide.Right, shell.OutlinePreferences.Side);
        });
    }
```

- [ ] **Step 4: Тесты и сборка** (`dotnet test tests/MarkMello.Presentation.Tests --filter FullyQualifiedName~AdvancedReadingBindings`).

- [ ] **Step 5: Commit** (`:sparkles: feat(outline): add outline settings section`).

---

### Task 12: Ручная проверка и финал

- [ ] **Step 1: Запуск**: `dotnet run --project src/MarkMello.Desktop -- sample.md`.

- [ ] **Step 2: Сценарии по макету и ADR.** Каждый отметить «ок» или описать отклонение:
1. Первый запуск: панели нет, на кнопке тултип «Оглавление (Ctrl+Shift+B)».
2. Ctrl+Shift+B показывает панель слева шириной 260. Повторно — скрывает. После перезапуска состояние восстанавливается.
3. Клик по пункту прокручивает документ к заголовку.
4. При прокрутке подсвечивается текущий раздел: оранжевая планка и Medium.
5. Свернуть ветку с текущим разделом: подсвечен её заголовок.
6. Граница: курсор ↔, оранжевая подсветка, диапазон 220–340, ширина сохраняется.
7. Настройки → Справа: панель справа, миникарта скрыта. Обратно слева: миникарта вернулась.
8. Глубина H2: H3 пропали. Размер H1 = 18 применяется сразу. Интервал 2.0 применяется.
9. Ctrl+E: панель на месте. Клик ставит курсор на строку заголовка, строка наверху, можно печатать. Подсветка следует за курсором.
10. Правка заголовка в редакторе: оглавление обновляется после debounce, свёрнутые ветки не раскрываются.
11. Открыть папку с панелью слева: порядок файлы | оглавление | документ. Вкладки: у каждой своё оглавление.
12. Документ без заголовков: «В документе нет заголовков».
13. Тёмная тема, русский и английский интерфейс.
14. Длинный заголовок: многоточие и тултип.

- [ ] **Step 3: NO GOD FILES.** Run: `wc -l` для всех созданных и изменённых файлов. Если что-то больше примерно 300 строк, разделить по ответственности. `MainWindow.axaml` и `ShellViewModel.cs` уже были большими, к ним добавлено только необходимое.

- [ ] **Step 4: Тесты затронутого кода**

Run:
```bash
dotnet test tests/MarkMello.Domain.Tests --filter "FullyQualifiedName~Outline"
dotnet test tests/MarkMello.Presentation.Tests --filter "FullyQualifiedName~Outline|FullyQualifiedName~JsonSettingsStore|FullyQualifiedName~ShellViewModelTests|FullyQualifiedName~MainWindow|FullyQualifiedName~AdvancedReading|FullyQualifiedName~EditModeScrollSync"
```
Expected: всё PASS.

- [ ] **Step 5: Push**: `git push`.

---

## Известные отклонения и решения по ходу реализации

1. **Edit mode без кнопки.** Плавающая группа кнопок у автора скрыта в режиме редактора (`ShowsAppMenuControl => !IsEditMode`), поэтому кнопка оглавления там тоже не видна. Панель управляется горячей клавишей. Если нужна кнопка в редакторе, это вопрос к макету: например, кнопка в тулбаре редактора.
2. **Идентификатор пункта.** ADR говорит о якоре, реализация использует строку исходника, потому что она одинаково работает в viewer и editor. Свёрнутость при перестроении сохраняется по паре «уровень + текст».
3. **Только заголовки верхнего уровня документа.** Заголовки внутри цитат и списков в оглавление не попадают.
