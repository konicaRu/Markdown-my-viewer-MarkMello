namespace MarkMello.Presentation.Localization;

public sealed partial class LocalizationService
{
    private static void AddCloseConfirmationStrings()
    {
        English["CloseConfirmTitle"] = "Close?";
        English["CloseConfirmTab"] = "Close this tab?";
        English["CloseConfirmFile"] = "Close this document?";
        English["CloseConfirmWindow"] = "Close MarkMello?";
        English["CloseConfirmDontAsk"] = "Don't ask again";
        English["CloseConfirmCancel"] = "Cancel";
        English["CloseConfirmAccept"] = "Close";
        English["ConfirmBeforeCloseLabel"] = "Ask before closing";
        English["ConfirmBeforeCloseHint"] = "Confirm closing a tab, a document or the app";

        Russian["CloseConfirmTitle"] = "Закрыть?";
        Russian["CloseConfirmTab"] = "Закрыть эту вкладку?";
        Russian["CloseConfirmFile"] = "Закрыть этот документ?";
        Russian["CloseConfirmWindow"] = "Закрыть MarkMello?";
        Russian["CloseConfirmDontAsk"] = "Больше не спрашивать";
        Russian["CloseConfirmCancel"] = "Отмена";
        Russian["CloseConfirmAccept"] = "Закрыть";
        Russian["ConfirmBeforeCloseLabel"] = "Спрашивать перед закрытием";
        Russian["ConfirmBeforeCloseHint"] = "Подтверждать закрытие вкладки, документа или приложения";
    }
}
