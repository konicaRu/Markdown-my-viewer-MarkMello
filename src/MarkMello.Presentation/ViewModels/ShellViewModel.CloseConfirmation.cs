using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MarkMello.Presentation.ViewModels;

/// <summary>
/// Вопрос «Закрыть?» перед закрытием вкладки, файла или всего окна. Отключается галочкой
/// в самом вопросе или переключателем в расширенных настройках. Если у документа есть
/// несохранённые правки, вопрос задаёт диалог «Сохранить / Не сохранять» — второй вопрос
/// поверх него не нужен.
/// </summary>
public partial class ShellViewModel
{
    private bool _confirmBeforeClose = true;
    private bool _windowCloseConfirmed;
    private Func<Task>? _pendingCloseConfirmAction;

    [ObservableProperty]
    private bool _isCloseConfirmOpen;

    [ObservableProperty]
    private bool _closeConfirmDontAsk;

    [ObservableProperty]
    private string _closeConfirmMessage = string.Empty;

    public bool ConfirmBeforeClose
    {
        get => _confirmBeforeClose;
        set
        {
            if (_confirmBeforeClose == value)
            {
                return;
            }

            _confirmBeforeClose = value;
            OnPropertyChanged();
            _ = _settings.SaveConfirmBeforeCloseAsync(value).AsTask();
        }
    }

    public string CloseConfirmTitle => _localization["CloseConfirmTitle"];
    public string CloseConfirmDontAskLabel => _localization["CloseConfirmDontAsk"];
    public string CloseConfirmCancel => _localization["CloseConfirmCancel"];
    public string CloseConfirmAccept => _localization["CloseConfirmAccept"];
    public string ConfirmBeforeCloseLabel => _localization["ConfirmBeforeCloseLabel"];
    public string ConfirmBeforeCloseHint => _localization["ConfirmBeforeCloseHint"];

    internal async Task LoadConfirmBeforeCloseAsync()
    {
        _confirmBeforeClose = await _settings.LoadConfirmBeforeCloseAsync().ConfigureAwait(true);
        OnPropertyChanged(nameof(ConfirmBeforeClose));
    }

    /// <summary>
    /// Показывает вопрос и выполняет <paramref name="onConfirm"/> только после «Закрыть».
    /// Возвращает false, если вопрос выключен — вызывающий закрывает сразу.
    /// </summary>
    private bool TryRequestCloseConfirmation(string messageKey, Func<Task> onConfirm)
    {
        if (!_confirmBeforeClose)
        {
            return false;
        }

        if (IsCloseConfirmOpen)
        {
            return true;
        }

        _pendingCloseConfirmAction = onConfirm;
        CloseConfirmDontAsk = false;
        CloseConfirmMessage = _localization[messageKey];
        IsCloseConfirmOpen = true;
        return true;
    }

    [RelayCommand]
    private async Task AcceptCloseConfirmAsync()
    {
        var action = _pendingCloseConfirmAction;
        if (CloseConfirmDontAsk)
        {
            ConfirmBeforeClose = false;
        }

        DismissCloseConfirmation();
        if (action is not null)
        {
            await action().ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private void CancelCloseConfirm() => DismissCloseConfirmation();

    private void DismissCloseConfirmation()
    {
        _pendingCloseConfirmAction = null;
        IsCloseConfirmOpen = false;
    }

    private void RefreshCloseConfirmationTexts()
    {
        OnPropertyChanged(nameof(CloseConfirmTitle));
        OnPropertyChanged(nameof(CloseConfirmDontAskLabel));
        OnPropertyChanged(nameof(CloseConfirmCancel));
        OnPropertyChanged(nameof(CloseConfirmAccept));
        OnPropertyChanged(nameof(ConfirmBeforeCloseLabel));
        OnPropertyChanged(nameof(ConfirmBeforeCloseHint));
    }
}
