using MarkMello.Application.UseCases;
using MarkMello.Domain;
using MarkMello.Domain.Workspace;
using MarkMello.Presentation.Localization;
using MarkMello.Presentation.ViewModels;

namespace MarkMello.Presentation.Tests;

/// <summary>Вопрос «Закрыть?» перед закрытием вкладки, файла и окна и его отключение.</summary>
public sealed class CloseConfirmationTests
{
    [Fact]
    public async Task ClosingATabAsksFirstAndKeepsItUntilAccepted()
    {
        var harness = await CreateHarnessAsync();
        
        await harness.ViewModel.CloseActiveTabCommand.ExecuteAsync(null);

        Assert.True(harness.ViewModel.IsCloseConfirmOpen);
        Assert.Equal(2, harness.ViewModel.OpenDocuments.Tabs.Count);

        await harness.ViewModel.AcceptCloseConfirmCommand.ExecuteAsync(null);

        Assert.False(harness.ViewModel.IsCloseConfirmOpen);
        Assert.Single(harness.ViewModel.OpenDocuments.Tabs);
    }

    [Fact]
    public async Task CancellingTheQuestionLeavesTheTabOpen()
    {
        var harness = await CreateHarnessAsync();

        await harness.ViewModel.CloseActiveTabCommand.ExecuteAsync(null);
        harness.ViewModel.CancelCloseConfirmCommand.Execute(null);

        Assert.False(harness.ViewModel.IsCloseConfirmOpen);
        Assert.Equal(2, harness.ViewModel.OpenDocuments.Tabs.Count);
    }

    [Fact]
    public async Task DontAskAgainClosesAndTurnsTheQuestionOffForGood()
    {
        var harness = await CreateHarnessAsync();

        await harness.ViewModel.CloseActiveTabCommand.ExecuteAsync(null);
        harness.ViewModel.CloseConfirmDontAsk = true;
        await harness.ViewModel.AcceptCloseConfirmCommand.ExecuteAsync(null);

        Assert.False(harness.ViewModel.ConfirmBeforeClose);
        Assert.False(harness.Settings.ConfirmBeforeClose);

        await harness.ViewModel.CloseActiveTabCommand.ExecuteAsync(null);

        Assert.False(harness.ViewModel.IsCloseConfirmOpen);
        Assert.Empty(harness.ViewModel.OpenDocuments.Tabs);
    }

    [Fact]
    public async Task DisabledSettingClosesWithoutAsking()
    {
        var harness = await CreateHarnessAsync();
        harness.ViewModel.ConfirmBeforeClose = false;

        await harness.ViewModel.CloseActiveTabCommand.ExecuteAsync(null);

        Assert.False(harness.ViewModel.IsCloseConfirmOpen);
        Assert.Single(harness.ViewModel.OpenDocuments.Tabs);
    }

    [Fact]
    public async Task WindowCloseAsksAndRaisesCloseRequestedOnlyAfterAccept()
    {
        var harness = await CreateHarnessAsync();
        var closeRequested = 0;
        harness.ViewModel.CloseRequested += (_, _) => closeRequested++;

        Assert.True(harness.ViewModel.TryQueueCloseRequest());
        Assert.True(harness.ViewModel.IsCloseConfirmOpen);
        Assert.Equal(0, closeRequested);

        await harness.ViewModel.AcceptCloseConfirmCommand.ExecuteAsync(null);

        Assert.Equal(1, closeRequested);
    }

    [Fact]
    public async Task WindowWithoutDocumentsClosesWithoutAsking()
    {
        var harness = await CreateHarnessAsync();
        harness.ViewModel.ConfirmBeforeClose = false;
        await harness.ViewModel.CloseActiveTabCommand.ExecuteAsync(null);
        await harness.ViewModel.CloseActiveTabCommand.ExecuteAsync(null);
        harness.ViewModel.ConfirmBeforeClose = true;

        Assert.False(harness.ViewModel.TryQueueCloseRequest());
    }

    [Fact]
    public async Task SettingIsLoadedFromTheStoreOnInitialize()
    {
        var harness = CreateHarness();
        harness.Settings.ConfirmBeforeClose = false;

        await harness.ViewModel.InitializeAsync();

        Assert.False(harness.ViewModel.ConfirmBeforeClose);
    }

    private static async Task<EditorTestHarness> CreateHarnessAsync()
    {
        var harness = CreateHarness();
        // Вкладки существуют только в режиме открытой папки.
        await harness.ViewModel.OpenFolderPathAsync(@"C:\docs");
        await harness.ViewModel.OpenPathAsync(@"C:\docs\first.md");
        await harness.ViewModel.OpenPathAsync(@"C:\docs\second.md");
        return harness;
    }

    private static EditorTestHarness CreateHarness()
    {
        var loader = new StubDocumentLoader();
        loader.Sources[@"C:\docs\first.md"] = new MarkdownSource(@"C:\docs\first.md", "first.md", "# first");
        loader.Sources[@"C:\docs\second.md"] = new MarkdownSource(@"C:\docs\second.md", "second.md", "# second");

        var fileSystem = new FakeWorkspaceFileSystem();
        fileSystem.AddDirectory(
            @"C:\docs",
            WorkspaceEntry.ForFile(@"C:\docs\first.md", "first.md"),
            WorkspaceEntry.ForFile(@"C:\docs\second.md", "second.md"));
        var settings = new InMemorySettingsStore();

        var viewModel = new ShellViewModel(
            new OpenDocumentUseCase(loader),
            new SaveDocumentUseCase(new RecordingDocumentSaver()),
            new StubFilePicker(),
            new StubCommandLineActivation(),
            new LocalizationService(AppLanguage.English),
            settings,
            new RecordingThemeService(),
            new RecordingStartupMetrics(),
            new RenderMarkdownDocumentUseCase(new TestMarkdownRenderer(), new FakeDiagramRenderService()),
            new StubUpdateService(),
            new OpenFolderUseCase(fileSystem),
            new ExpandFolderNodeUseCase(fileSystem),
            new SearchWorkspaceFilesUseCase(fileSystem),
            new WorkspaceFileOperationsUseCase(fileSystem, new FakePlatformServices()),
            new FakePlatformServices(),
            static () => new FakeWorkspaceWatcher(),
            new RecordingWindowLauncher());

        return new EditorTestHarness(settings, viewModel);
    }

    private sealed record EditorTestHarness(InMemorySettingsStore Settings, ShellViewModel ViewModel);
}
