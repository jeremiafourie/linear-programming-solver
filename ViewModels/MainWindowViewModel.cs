using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using linear_programming_solver.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace linear_programming_solver.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
  private readonly Window _window;

    [ObservableProperty]
    private ViewModelBase _currentViewModel;

    [ObservableProperty]
    private string _windowTitle = "Linear Programming Solver";

    [ObservableProperty]
    private LinearProgramModel _currentModel = new();

    // Child ViewModels
    public WelcomeViewModel WelcomeViewModel { get; }
    public ProblemEditorViewModel ProblemEditorViewModel { get; }
    public SolutionTableViewModel SolutionTableViewModel { get; }
    public TableauIterationsViewModel TableauIterationsViewModel { get; }

    public MainWindowViewModel(Window window)
    {
        _window = window;
        
        // Initialize child ViewModels
        WelcomeViewModel = new WelcomeViewModel(this);
        ProblemEditorViewModel = new ProblemEditorViewModel();
        SolutionTableViewModel = new SolutionTableViewModel();
        TableauIterationsViewModel = new TableauIterationsViewModel();
        
        // Start with Welcome view
        CurrentViewModel = WelcomeViewModel;
    }

    #region Navigation Methods

    public void NavigateToWelcome()
    {
        CurrentViewModel = WelcomeViewModel;
        StatusMessage = "Welcome view";
    }

    public void NavigateToProblemEditor()
    {
        if (CurrentModel.IsLoaded)
        {
            ProblemEditorViewModel.LoadProblem(CurrentModel);
        }
        CurrentViewModel = ProblemEditorViewModel;
        StatusMessage = "Problem Editor view";
    }

    public void NavigateToSolutionTable()
    {
        SolutionTableViewModel.LoadSolution(/* pass solution data */);
        CurrentViewModel = SolutionTableViewModel;
        StatusMessage = "Solution Table view";
    }

    public void NavigateToTableauIterations()
    {
        TableauIterationsViewModel.LoadIterations(/* pass iterations data */);
        CurrentViewModel = TableauIterationsViewModel;
        StatusMessage = "Tableau Iterations view";
    }

    #endregion

    #region File Menu Commands

    [RelayCommand]
    public async Task NewProblemAsync()
    {
        CurrentModel = new LinearProgramModel { IsLoaded = true, FileName = "New Problem", FileContent = "max +2 +3 +3 +5 +2 +4\n+11 +8 +6 +14 +10 +10 <=40\nbin bin bin bin bin bin" };
        WindowTitle = "Linear Programming Solver - New Problem";
        NavigateToProblemEditor();
        StatusMessage = "New problem created";
    }

    [RelayCommand]
    public async Task OpenProblemAsync()
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(_window);
            var files = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Problem",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("LP files") { Patterns = new[] { "*.lp", "*.txt" } },
                    new FilePickerFileType("All files") { Patterns = new[] { "*.*" } }
                }
            });

            if (files.Count >= 1)
            {
                var filePath = files[0].Path.LocalPath;
                var content = await File.ReadAllTextAsync(filePath);
                var fileName = Path.GetFileName(filePath);
                
                CurrentModel = new LinearProgramModel
                {
                    FileName = fileName,
                    FileContent = content,
                    IsLoaded = true
                };
                
                WindowTitle = $"Linear Programming Solver - {fileName}";
                NavigateToProblemEditor();
                StatusMessage = $"Problem loaded: {fileName}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error opening problem: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveProblemAsync()
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(_window);
            var file = await topLevel!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Problem",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("LP files") { Patterns = new[] { "*.lp" } },
                    new FilePickerFileType("Text files") { Patterns = new[] { "*.txt" } }
                }
            });

            if (file != null)
            {
                await File.WriteAllTextAsync(file.Path.LocalPath, CurrentModel.FileContent);
                StatusMessage = "Problem saved successfully";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving problem: {ex.Message}";
        }
    }

    [RelayCommand] private async Task SaveProblemAsAsync() { await SaveProblemAsync(); }
    [RelayCommand] private void ExportResults() { StatusMessage = "Export Results clicked"; }
    [RelayCommand] private void Exit() { Environment.Exit(0); }

    #endregion

    #region Algorithm Menu Commands
    
    [RelayCommand] private void SelectPrimalSimplex() { StatusMessage = "Primal Simplex algorithm selected"; }
    [RelayCommand] private void SelectRevisedPrimalSimplex() { StatusMessage = "Revised Primal Simplex algorithm selected"; }
    [RelayCommand] private void SelectBranchAndBoundSimplex() { StatusMessage = "Branch & Bound Simplex algorithm selected"; }
    [RelayCommand] private void SelectCuttingPlane() { StatusMessage = "Cutting Plane algorithm selected"; }
    [RelayCommand] private void SelectBranchAndBoundKnapsack() { StatusMessage = "Branch & Bound Knapsack algorithm selected"; }
    [RelayCommand] private void SolveProblem() { StatusMessage = "Solve Problem clicked"; NavigateToSolutionTable(); }
    [RelayCommand] private void SolveConfiguration() { StatusMessage = "Solve Configuration clicked"; }
    [RelayCommand] private void ViewSolutionDetails() { StatusMessage = "View Solution Details clicked"; }

    #endregion

    #region Analysis Menu Commands

    // Variable Analysis
    [RelayCommand] private void NonBasicVariableRange() { StatusMessage = "Non-Basic Variable Range clicked"; }
    [RelayCommand] private void ApplyNonBasicVariable() { StatusMessage = "Apply Non-Basic Variable Change clicked"; }
    [RelayCommand] private void BasicVariableRange() { StatusMessage = "Basic Variable Range clicked"; }
    [RelayCommand] private void ApplyBasicVariable() { StatusMessage = "Apply Basic Variable Change clicked"; }

    // Constraint Analysis
    [RelayCommand] private void RhsValueRange() { StatusMessage = "RHS Value Range clicked"; }
    [RelayCommand] private void ApplyRhsChange() { StatusMessage = "Apply RHS Change clicked"; }
    [RelayCommand] private void NonBasicColumnRange() { StatusMessage = "Non-Basic Column Range clicked"; }
    [RelayCommand] private void ApplyColumnChange() { StatusMessage = "Apply Column Change clicked"; }

    // Model Modification
    [RelayCommand] private void AddNewActivity() { StatusMessage = "Add New Activity clicked"; }
    [RelayCommand] private void AddNewConstraint() { StatusMessage = "Add New Constraint clicked"; }

    // Shadow Prices
    [RelayCommand] private void ShadowPrices() { StatusMessage = "Shadow Prices clicked"; }

    // Duality
    [RelayCommand] private void GenerateDualProblem() { StatusMessage = "Generate Dual Problem clicked"; }
    [RelayCommand] private void SolveDualProblem() { StatusMessage = "Solve Dual Problem clicked"; }
    [RelayCommand] private void VerifyDualityType() { StatusMessage = "Verify Duality Type clicked"; }

    #endregion

    #region View Menu Commands

    [RelayCommand] private void SolutionTable() { NavigateToSolutionTable(); }
    [RelayCommand] private void TableauIterations() { NavigateToTableauIterations(); }
    [RelayCommand] private void CanonicalForm() { StatusMessage = "Canonical Form view clicked"; }
    [RelayCommand] private void FullScreen() { StatusMessage = "Full Screen toggled"; }
    [RelayCommand] private void StatusBar() { StatusMessage = "Status Bar toggled"; }

    #endregion
}