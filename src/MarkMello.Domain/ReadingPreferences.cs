namespace MarkMello.Domain;

/// <summary>
/// Пользовательские настройки чтения документа. Применяются live, не требуют перезапуска.
/// </summary>
/// <param name="FontFamily">Семейство шрифта (Serif/Sans/Mono).</param>
/// <param name="FontSize">Базовый размер шрифта в пикселях.</param>
/// <param name="LineHeight">Межстрочный интервал (множитель к размеру шрифта).</param>
/// <param name="ContentWidth">Максимальная полезная ширина текста документа в пикселях.</param>
/// <param name="DocumentMinimapMode">Режим отображения миникарты документа.</param>
public sealed record ReadingPreferences(
    FontFamilyMode FontFamily,
    int FontSize,
    double LineHeight,
    int ContentWidth,
    DocumentMinimapMode DocumentMinimapMode = DocumentMinimapMode.Auto,
    string? SerifFontFamily = null,
    string? SansFontFamily = null,
    string? MonoFontFamily = null,
    double LetterSpacing = 0,
    int InterfaceFontSize = ReadingPreferences.DefaultInterfaceFontSize)
{
    public const int MinFontSize = 14;
    public const int MaxFontSize = 24;
    public const double MinLineHeight = 1.2;
    public const double MaxLineHeight = 2.0;
    public const double LineHeightStep = 0.05;
    public const double MinLetterSpacing = 0;
    public const double MaxLetterSpacing = 2;
    public const double LetterSpacingStep = 0.1;
    public const int DefaultInterfaceFontSize = 14;
    public const int MinInterfaceFontSize = 11;
    public const int MaxInterfaceFontSize = 18;
    public const int NarrowContentWidth = 640;
    public const int MediumContentWidth = 820;
    public const int WideContentWidth = 1080;
    public const int MinContentWidth = NarrowContentWidth;
    public const int MaxContentWidth = 1280;
    public const int ContentWidthStep = 20;

    private const int LegacyNarrowContentWidth = 580;
    private const int LegacyMediumContentWidth = 720;
    private const int LegacyWideContentWidth = 860;

    /// <summary>
    /// Безопасные значения по умолчанию. Используются при отсутствии или повреждении сохранённых настроек.
    /// </summary>
    public static ReadingPreferences Default { get; } = new(
        FontFamily: FontFamilyMode.Serif,
        FontSize: 18,
        LineHeight: 1.7,
        ContentWidth: MediumContentWidth,
        DocumentMinimapMode: DocumentMinimapMode.Auto);

    /// <summary>
    /// Нормализует пользовательские настройки до безопасного и предсказуемого диапазона.
    /// Используется для live-обновлений и восстановления из persistence.
    /// </summary>
    public static ReadingPreferences Normalize(ReadingPreferences? preferences)
    {
        if (preferences is null)
        {
            return Default;
        }

        var fontFamily = Enum.IsDefined(preferences.FontFamily)
            ? preferences.FontFamily
            : Default.FontFamily;

        var fontSize = Math.Clamp(preferences.FontSize, MinFontSize, MaxFontSize);
        var lineHeight = NormalizeLineHeight(preferences.LineHeight);
        var contentWidth = NormalizeContentWidth(preferences.ContentWidth);
        var documentMinimapMode = Enum.IsDefined(preferences.DocumentMinimapMode)
            ? preferences.DocumentMinimapMode
            : Default.DocumentMinimapMode;

        return new ReadingPreferences(
            fontFamily, fontSize, lineHeight, contentWidth, documentMinimapMode,
            NormalizeFontName(preferences.SerifFontFamily),
            NormalizeFontName(preferences.SansFontFamily),
            NormalizeFontName(preferences.MonoFontFamily),
            NormalizeLetterSpacing(preferences.LetterSpacing),
            Math.Clamp(preferences.InterfaceFontSize, MinInterfaceFontSize, MaxInterfaceFontSize));
    }

    public ReadingPreferences Normalize() => Normalize(this);

    public string? GetFontFamilyName(FontFamilyMode mode) => mode switch
    {
        FontFamilyMode.Sans => SansFontFamily,
        FontFamilyMode.Mono => MonoFontFamily,
        _ => SerifFontFamily
    };

    public ReadingPreferences WithFontFamilyName(FontFamilyMode mode, string? name) => mode switch
    {
        FontFamilyMode.Sans => this with { SansFontFamily = name },
        FontFamilyMode.Mono => this with { MonoFontFamily = name },
        _ => this with { SerifFontFamily = name }
    };

    private static string? NormalizeFontName(string? name)
        => string.IsNullOrWhiteSpace(name) ? null : name.Trim();

    private static double NormalizeLetterSpacing(double value)
    {
        if (!double.IsFinite(value))
        {
            return Default.LetterSpacing;
        }

        var clamped = Math.Clamp(value, MinLetterSpacing, MaxLetterSpacing);
        return Math.Round(Math.Round(clamped / LetterSpacingStep) * LetterSpacingStep, 1);
    }

    private static double NormalizeLineHeight(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return Default.LineHeight;
        }

        var clamped = Math.Clamp(value, MinLineHeight, MaxLineHeight);
        var rounded = Math.Round(clamped / LineHeightStep, MidpointRounding.AwayFromZero) * LineHeightStep;
        return Math.Round(rounded, 2, MidpointRounding.AwayFromZero);
    }

    private static int NormalizeContentWidth(int value)
    {
        var migrated = value switch
        {
            LegacyNarrowContentWidth => NarrowContentWidth,
            LegacyMediumContentWidth => MediumContentWidth,
            LegacyWideContentWidth => WideContentWidth,
            _ => value
        };

        var clamped = Math.Clamp(migrated, MinContentWidth, MaxContentWidth);
        var rounded = (int)Math.Round(clamped / (double)ContentWidthStep, MidpointRounding.AwayFromZero) * ContentWidthStep;
        return Math.Clamp(rounded, MinContentWidth, MaxContentWidth);
    }
}
