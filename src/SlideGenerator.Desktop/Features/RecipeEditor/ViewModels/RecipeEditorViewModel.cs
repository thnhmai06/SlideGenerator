/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: RecipeEditorViewModel.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SlideGenerator.Desktop.Features.RecipeEditor.Models;
using SlideGenerator.Desktop.Features.RecipeEditor.Services;
using SlideGenerator.Desktop.Services.Dialogs;
using SlideGenerator.Recipe.Models;
using SlideGenerator.Recipe.Services;
using SlideGenerator.Summarizer.Workbooks;
using RecipeModel = SlideGenerator.Recipe.Models.Recipe;

namespace SlideGenerator.Desktop.Features.RecipeEditor.ViewModels;

/// <summary>
///     Coordinates the editor page for one <see cref="RecipeModel" />: owns the mapping navigator
///     (<see cref="Sessions" />) and the three per-mapping panels (<see cref="Canvas" />/<see cref="TextBindings" />/
///     <see cref="Sources" />), fetching each selected mapping's presentation/workbook summaries via
///     <see cref="ISummaryCache" /> and handing all three panels the same flattened <see cref="AvailableColumns" />
///     list — the same names must resolve to the same <c>BindingDisplayState</c> in the canvas and the text list.
///     Touched-placeholder/shape state lives on <see cref="MappingEditSession" />, not inside the panels
///     themselves, so switching mappings and back doesn't revert a confirmed Normalized binding to Suggested.
/// </summary>
public sealed partial class RecipeEditorViewModel : ObservableObject
{
    private readonly ISummaryCache _summaryCache;
    private readonly IFilePicker _filePicker;
    private readonly IDialogService _dialogService;
    private readonly IRecipeRepository _recipeRepository;

    // The session whose mapping is actually reflected in the three panels right now. Distinct from
    // SelectedSession, which is set *before* LoadMappingAsync runs — if the load bails early (template slide
    // no longer exists), the panels still hold the previous session's content, and ProjectCurrentSessionEdits
    // must not mistake that stale content for the new session's edits.
    private MappingEditSession? _loadedSession;

    // True for the duration of InitializeAsync — OnNameChanged must not mark the recipe dirty just because
    // InitializeAsync is setting Name to the recipe's already-saved value.
    private bool _isInitializing;

    [ObservableProperty] private int? _id;
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private RecipeModel _recipe = new([]);
    [ObservableProperty] private MappingEditSession? _selectedSession;
    [ObservableProperty] private bool _isDirty;
    [ObservableProperty] private bool _isLoading;

    /// <summary>Gets or sets whether the page shows the 4-step Guided flow or the full Advanced layout (plan
    ///     §5.2.a) — one <see cref="RecipeModel" />, one ViewModel, one <c>IsGuided</c> flag that only changes
    ///     which panels <c>RecipeEditorView</c> arranges on screen.</summary>
    [ObservableProperty] private bool _isGuided;

    [ObservableProperty] private GuidedStep _guidedStep = GuidedStep.Template;

    /// <summary>Raised after <see cref="SaveCommand" />/<see cref="SaveAndRunCommand" /> persists successfully — the recipe list should refresh.</summary>
    public event Action? Saved;

    /// <summary>Raised after <see cref="SaveAndRunCommand" /> actually starts a run — the host page should navigate to Runs.</summary>
    public event Action<string>? RunStarted;

    /// <summary>Gets the combined <see cref="Models.BindingDisplayState" /> counts across both text placeholders and
    ///     image shapes — the Advanced-mode warning strip's summary (plan §5.2: "một dải cảnh báo trong Advanced").</summary>
    public (int Assigned, int Suggested, int NeedsSelection, int Unassigned) CombinedSummary
    {
        get
        {
            var text = TextBindings.Summary;
            var image = Canvas.Summary;
            return (text.Assigned + image.Assigned, text.Suggested + image.Suggested,
                text.NeedsSelection + image.NeedsSelection, text.Unassigned + image.Unassigned);
        }
    }

    /// <summary>Gets whether any binding still needs the user to pick from Ambiguous candidates — blocks
    ///     <see cref="SaveAndRunCommand" /> (plan §5.2: "Không cho Lưu và chạy khi còn mục Ambiguous chưa quyết"
    ///     — plain <see cref="SaveCommand" /> is unaffected, saving a draft with unresolved bindings is fine).</summary>
    public bool HasUnresolvedBindings => CombinedSummary.NeedsSelection > 0;

    /// <summary>Gets whether Guided step ① has a template picked yet.</summary>
    public bool HasTemplate => Sessions.Count > 0;

    /// <summary>Gets the file count Guided step ④ shows ("sẽ tạo N file") — in Guided there's always exactly one
    ///     mapping, so this is just its worksheet-source count (mirrors <c>Service.BuildJobs</c>'s
    ///     mapping×source flattening for the single-mapping case).</summary>
    public int GuidedFileCount => Sources.Sources.Count;

    /// <summary>Gets the current Guided step's user-facing title — no internal vocabulary (plan §5.2.a).</summary>
    public string GuidedStepTitle => GuidedStep switch
    {
        GuidedStep.Template => "① Mẫu slide",
        GuidedStep.Data => "② Dữ liệu",
        GuidedStep.Binding => "③ Ghép",
        GuidedStep.Review => "④ Xem lại",
        _ => ""
    };

    /// <summary>Gets one edit session per mapping in <see cref="Recipe" />, in order.</summary>
    public ObservableCollection<MappingEditSession> Sessions { get; } = [];

    /// <summary>Gets whether the mapping navigator should be shown — only meaningful once there's more than one mapping to pick between.</summary>
    public bool ShowMappingNavigator => Sessions.Count >= 2;

    /// <summary>Gets every column visible to <see cref="SelectedSession" />'s worksheet sources — fed identically to <see cref="Canvas" /> and <see cref="TextBindings" />.</summary>
    public IReadOnlyList<string> AvailableColumns { get; private set; } = [];

    /// <summary>Gets the canvas panel for the currently selected mapping's template slide.</summary>
    public SlideCanvasViewModel Canvas { get; }

    /// <summary>Gets the text-placeholder binding panel for the currently selected mapping.</summary>
    public TextBindingsViewModel TextBindings { get; }

    /// <summary>Gets the worksheet-source panel for the currently selected mapping.</summary>
    public WorksheetSourcesViewModel Sources { get; }

    /// <summary>Constructs the editor, wiring the three child panels it coordinates.</summary>
    public RecipeEditorViewModel(ISummaryCache summaryCache, IFilePicker filePicker, IDialogService dialogService,
        IRecipeRepository recipeRepository)
    {
        _summaryCache = summaryCache;
        _filePicker = filePicker;
        _dialogService = dialogService;
        _recipeRepository = recipeRepository;
        Canvas = new SlideCanvasViewModel();
        TextBindings = new TextBindingsViewModel();
        Sources = new WorksheetSourcesViewModel(filePicker, summaryCache);
        Sessions.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(ShowMappingNavigator));
            OnPropertyChanged(nameof(HasTemplate));
            RefreshCommandStates();
        };
        Canvas.Changed += OnChildChanged;
        TextBindings.Changed += OnChildChanged;
        Sources.Changed += OnChildChanged;
    }

    /// <summary>Loads a brand-new, unsaved recipe into the editor — <see cref="Id" /> stays <see langword="null" />
    ///     until the first successful <see cref="SaveCommand" />, and the editor starts in Guided mode (plan
    ///     §5.2.a: "Guided (mặc định khi tạo recipe mới)").</summary>
    public Task InitializeAsync(RecipeModel recipe) => InitializeAsync(null, "", recipe);

    /// <summary>Loads the given recipe into the editor, selecting its first mapping (if any), and clears the dirty
    ///     flag. Starts in Advanced mode when <paramref name="id" /> is already saved (plan §5.2.a: "Recipe đã có
    ///     sẵn khi mở từ Recipes list thì vào thẳng Advanced"), Guided otherwise.</summary>
    public async Task InitializeAsync(int? id, string name, RecipeModel recipe)
    {
        _isInitializing = true;
        try
        {
            Id = id;
            Name = name;
            Recipe = recipe;
            IsDirty = false;
            IsGuided = id is null;
            GuidedStep = GuidedStep.Template;
            _loadedSession = null;

            Sessions.Clear();
            foreach (var mapping in recipe.Mappings) Sessions.Add(new MappingEditSession(mapping));

            SelectedSession = null;
            if (Sessions.Count > 0) await SelectSessionAsync(Sessions[0]).ConfigureAwait(true);
        }
        finally
        {
            _isInitializing = false;
        }

        RefreshCommandStates();
    }

    private void OnChildChanged()
    {
        MarkDirty();
        OnPropertyChanged(nameof(CombinedSummary));
        OnPropertyChanged(nameof(HasUnresolvedBindings));
        OnPropertyChanged(nameof(GuidedFileCount));
    }

    private void MarkDirty()
    {
        IsDirty = true;
        RefreshCommandStates();
    }

    /// <summary>Re-evaluates every command whose <c>CanExecute</c> depends on dirty/binding/step state — cheaper
    ///     to call unconditionally from every mutation point than to track exactly which commands each one
    ///     could affect.</summary>
    private void RefreshCommandStates()
    {
        SaveCommand.NotifyCanExecuteChanged();
        SaveAndRunCommand.NotifyCanExecuteChanged();
        NextGuidedStepCommand.NotifyCanExecuteChanged();
        PreviousGuidedStepCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Marks the recipe dirty after an edit dispatched directly by view code-behind rather than through
    ///     one of this ViewModel's own commands — the inspector's ROI reorder/remove buttons (P4.4) operate
    ///     straight on <see cref="ShapeOverlayViewModel" />, which has no dirty-tracking hook of its own.</summary>
    public void NotifyEdited()
    {
        MarkDirty();
    }

    partial void OnNameChanged(string value)
    {
        if (_isInitializing) return;
        MarkDirty();
    }

    partial void OnGuidedStepChanged(GuidedStep value)
    {
        OnPropertyChanged(nameof(GuidedStepTitle));
        RefreshCommandStates();
    }

    private bool CanSave() => IsDirty && !string.IsNullOrWhiteSpace(Name);

    /// <summary>Persists the recipe — inserts on first save (<see cref="Id" /> still <see langword="null" />),
    ///     updates thereafter. Disabled while dirty tracking says there's nothing to save, or <see cref="Name" />
    ///     is blank (plan §5.2: "Lưu: thủ công bằng nút Lưu. Nút bật khi dirty" — no Ambiguous condition here,
    ///     that only gates <see cref="SaveAndRunCommand" />).</summary>
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        await PersistAsync().ConfigureAwait(true);
    }

    private bool CanSaveAndRun() => HasTemplate && !HasUnresolvedBindings && !string.IsNullOrWhiteSpace(Name);

    /// <summary>Guided step ④'s "Lưu và chạy" — saves first if dirty, then opens the same run dialog the
    ///     Recipes list uses. Blocked while any binding still needs an Ambiguous pick (plan §5.2: "Không cho
    ///     Lưu và chạy khi còn mục Ambiguous chưa quyết").</summary>
    [RelayCommand(CanExecute = nameof(CanSaveAndRun))]
    private async Task SaveAndRunAsync()
    {
        if (IsDirty) await PersistAsync().ConfigureAwait(true);
        if (Id is null) return;

        var requestId = await _dialogService.ShowRunDialogAsync(Id.Value, Name).ConfigureAwait(true);
        if (requestId is not null) RunStarted?.Invoke(requestId);
    }

    private async Task PersistAsync()
    {
        var input = new RecipeInput(Name, ToRecipe());
        var metadata = Id is null
            ? await _recipeRepository.AddAsync(input).ConfigureAwait(true)
            : await _recipeRepository.UpdateAsync(Id.Value, input).ConfigureAwait(true);

        Id = metadata.Id;
        IsDirty = false;
        RefreshCommandStates();
        Saved?.Invoke();
    }

    private bool CanGoToNextGuidedStep() => GuidedStep switch
    {
        GuidedStep.Template => HasTemplate,
        GuidedStep.Data => Sources.Sources.Count > 0,
        GuidedStep.Binding => true,
        _ => false
    };

    /// <summary>Advances Guided mode to the next step — each step's prerequisite is enforced by
    ///     <see cref="CanGoToNextGuidedStep" />, not by this method.</summary>
    [RelayCommand(CanExecute = nameof(CanGoToNextGuidedStep))]
    private void NextGuidedStep()
    {
        GuidedStep = GuidedStep switch
        {
            GuidedStep.Template => GuidedStep.Data,
            GuidedStep.Data => GuidedStep.Binding,
            GuidedStep.Binding => GuidedStep.Review,
            _ => GuidedStep
        };
    }

    private bool CanGoToPreviousGuidedStep() => GuidedStep != GuidedStep.Template;

    [RelayCommand(CanExecute = nameof(CanGoToPreviousGuidedStep))]
    private void PreviousGuidedStep()
    {
        GuidedStep = GuidedStep switch
        {
            GuidedStep.Data => GuidedStep.Template,
            GuidedStep.Binding => GuidedStep.Data,
            GuidedStep.Review => GuidedStep.Binding,
            _ => GuidedStep
        };
    }

    /// <summary>Guided step ④'s "Mở chế độ nâng cao" link — stays on the same recipe, just switches template.</summary>
    [RelayCommand]
    private void SwitchToAdvanced()
    {
        IsGuided = false;
    }

    /// <summary>Projects in-flight edits, then rebuilds <see cref="Recipe" /> from every session's current mapping — the read path a save needs, since edits live on <see cref="Sessions" /> until pulled.</summary>
    public RecipeModel ToRecipe()
    {
        ProjectCurrentSessionEdits();
        return Recipe with { Mappings = Sessions.Select(s => s.Mapping).ToList() };
    }

    /// <summary>Opens the template picker; on a pick, appends a new mapping (no sources/instructions yet) and selects it.</summary>
    [RelayCommand]
    private async Task AddMappingAsync()
    {
        var template = await _dialogService.ShowTemplatePickerAsync().ConfigureAwait(true);
        if (template is null) return;

        var session = new MappingEditSession(new Mapping([], template, [], []));
        Sessions.Add(session);
        MarkDirty();
        await SelectSessionAsync(session).ConfigureAwait(true);
    }

    /// <summary>Removes a mapping from the recipe. If it was selected, selects a neighboring mapping (or none, if it was the last one).</summary>
    [RelayCommand]
    private void RemoveMapping(MappingEditSession session)
    {
        var index = Sessions.IndexOf(session);
        if (index < 0) return;

        Sessions.RemoveAt(index);
        MarkDirty();
        if (ReferenceEquals(_loadedSession, session)) _loadedSession = null;
        if (!ReferenceEquals(SelectedSession, session)) return;

        SelectedSession = null;
        var next = Sessions.Count > 0 ? Sessions[Math.Min(index, Sessions.Count - 1)] : null;
        if (next is not null) _ = SelectSessionAsync(next);
    }

    /// <summary>Moves a mapping one position earlier in the navigator order.</summary>
    [RelayCommand]
    private void MoveMappingUp(MappingEditSession session)
    {
        var index = Sessions.IndexOf(session);
        if (index <= 0) return;
        Sessions.Move(index, index - 1);
        MarkDirty();
    }

    /// <summary>Moves a mapping one position later in the navigator order.</summary>
    [RelayCommand]
    private void MoveMappingDown(MappingEditSession session)
    {
        var index = Sessions.IndexOf(session);
        if (index < 0 || index >= Sessions.Count - 1) return;
        Sessions.Move(index, index + 1);
        MarkDirty();
    }

    /// <summary>Opens a file picker and sets the shape's fallback image path — the image used when the row's own source is missing or invalid.</summary>
    [RelayCommand]
    private async Task PickFallbackImageAsync(ShapeOverlayViewModel overlay)
    {
        var path = await _filePicker.PickFileAsync("Chọn ảnh mặc định",
            [new Avalonia.Platform.Storage.FilePickerFileType("Ảnh") { Patterns = ["*.png", "*.jpg", "*.jpeg"] }]).ConfigureAwait(true);
        if (path is null) return;
        overlay.FallbackImagePath = path;
        MarkDirty();
    }

    /// <summary>Commits in-flight edits from the previously selected session, then loads <paramref name="session" />'s mapping into the three panels.</summary>
    [RelayCommand]
    public async Task SelectSessionAsync(MappingEditSession session)
    {
        ProjectCurrentSessionEdits();
        SelectedSession = session;

        IsLoading = true;
        try
        {
            await LoadMappingAsync(session).ConfigureAwait(true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Projects the three panels' current edits back into <see cref="SelectedSession" />'s mapping —
    ///     call before navigating away so edits aren't lost. A no-op unless <see cref="SelectedSession" /> is
    ///     the session actually reflected in the panels right now — otherwise a load that bailed early
    ///     (missing template slide) would project stale, previous-session content onto the new session. Real
    ///     dirty tracking is P4.6's job; this intentionally leaves <see cref="IsDirty" /> untouched so merely
    ///     switching mappings to look at them doesn't mark the recipe dirty.</summary>
    public void ProjectCurrentSessionEdits()
    {
        if (SelectedSession is null || !ReferenceEquals(SelectedSession, _loadedSession)) return;

        SelectedSession.Mapping = SelectedSession.Mapping with
        {
            TextInstructions = TextBindings.ToTextInstructions(),
            ImageInstructions = Canvas.ToImageInstructions(),
            Sources = Sources.ToWorksheetSources()
        };
    }

    private async Task LoadMappingAsync(MappingEditSession session)
    {
        var mapping = session.Mapping;

        var presentationSummary = await _summaryCache.GetPresentationAsync(mapping.Template.Presentation).ConfigureAwait(true);
        var slide = presentationSummary.Slides.FirstOrDefault(s => s.Slide == mapping.Template.Slide);
        if (slide is null) return; // template slide no longer exists in the presentation — nothing to show

        var worksheetPairs = new List<(WorksheetSource Source, WorksheetSummary Summary)>();
        foreach (var source in mapping.Sources)
        {
            var workbookSummary = await _summaryCache.GetWorkbookAsync(source.Workbook).ConfigureAwait(true);
            var worksheetSummary = workbookSummary.Worksheets.FirstOrDefault(w => w.Worksheet == source.Worksheet);
            if (worksheetSummary is not null) worksheetPairs.Add((source, worksheetSummary));
        }

        AvailableColumns = FlattenAvailableColumns(worksheetPairs.Select(p => p.Summary).ToList());
        OnPropertyChanged(nameof(AvailableColumns));

        Canvas.Load(slide, mapping.ImageInstructions, AvailableColumns, session.TouchedShapes);
        TextBindings.Load(slide.Placeholders, mapping.TextInstructions, AvailableColumns, session.TouchedPlaceholders);
        Sources.Load(worksheetPairs);
        _loadedSession = session;
    }

    /// <summary>Unions and dedupes every worksheet's preview headers — the same column list every panel must see for a mapping's suggestions/dropdowns to agree.</summary>
    internal static IReadOnlyList<string> FlattenAvailableColumns(IReadOnlyList<WorksheetSummary> summaries)
    {
        return summaries
            .SelectMany(s => s.Preview?.Headers ?? [])
            .Distinct()
            .ToList();
    }
}
