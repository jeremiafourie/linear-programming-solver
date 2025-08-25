using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using linear_programming_solver.Models;
using linear_programming_solver.Services;
using linear_programming_solver.ViewModels.Dialogs;
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

    [ObservableProperty]
    private AlgorithmType _selectedAlgorithm = AlgorithmType.PrimalSimplex;

    [ObservableProperty]
    private SolutionResult? _currentSolution;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    // Services
    private readonly SolutionEngine _solutionEngine = new();
    private readonly SensitivityAnalysisService _sensitivityService = new();
    private readonly DialogService _dialogService = new();

    // Child ViewModels
    public WelcomeViewModel WelcomeViewModel { get; }
    public ProblemEditorViewModel ProblemEditorViewModel { get; }
    public SolutionTableViewModel SolutionTableViewModel { get; }
    public IterationDiagramViewModel IterationDiagramViewModel { get; }
    public TableauIterationsViewModel TableauIterationsViewModel { get; }
    public CanonicalFormViewModel CanonicalFormViewModel { get; }

    public MainWindowViewModel(Window window)
    {
        _window = window;
        
        // Initialize child ViewModels
        WelcomeViewModel = new WelcomeViewModel(this);
        ProblemEditorViewModel = new ProblemEditorViewModel();
        SolutionTableViewModel = new SolutionTableViewModel();
        IterationDiagramViewModel = new IterationDiagramViewModel();
        TableauIterationsViewModel = new TableauIterationsViewModel();
        CanonicalFormViewModel = new CanonicalFormViewModel();
        
        // Connect ProblemEditor events
        ProblemEditorViewModel.SolutionCompleted += OnSolutionCompleted;
        ProblemEditorViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ProblemEditorViewModel.StatusMessage))
            {
                StatusMessage = ProblemEditorViewModel.StatusMessage;
            }
        };
        
        // Start with Welcome view
        CurrentViewModel = WelcomeViewModel;
    }

    private void OnSolutionCompleted(SolutionResult result)
    {
        CurrentSolution = result;
        
        if (result.Success)
        {
            // Update the selected algorithm to match what was used
            if (ProblemEditorViewModel.SelectedAlgorithm != null)
            {
                SelectedAlgorithm = ProblemEditorViewModel.SelectedAlgorithm.Algorithm;
            }
            
            // Automatically navigate to solution view
            NavigateToSolutionTable();
        }
    }

    #region Navigation Methods

    public void NavigateToWelcome()
    {
        CurrentViewModel = WelcomeViewModel;
        StatusMessage = "Welcome view";
    }

    public void NavigateToProblemEditor()
    {
        // Always sync the current model with the editor
        ProblemEditorViewModel.LoadProblem(CurrentModel);
        CurrentViewModel = ProblemEditorViewModel;
        StatusMessage = "Problem Editor view";
    }

    public void NavigateToSolutionTable()
    {
        if (CurrentSolution != null)
            SolutionTableViewModel.LoadSolution(CurrentSolution);
        CurrentViewModel = SolutionTableViewModel;
        StatusMessage = "Solution Table view";
    }

    public void NavigateToTableauIterations()
    {
        if (CurrentSolution?.Solution?.Iterations != null && CurrentSolution.Solution.Iterations.Count > 0)
        {
            TableauIterationsViewModel.LoadIterations(CurrentSolution.Solution.Iterations);
            CurrentViewModel = TableauIterationsViewModel;
            StatusMessage = "Tableau Iterations view";
        }
        else
        {
            StatusMessage = "No iteration data available. Please solve a problem first.";
        }
    }

    public void NavigateToCanonicalForm()
    {
        if (CurrentSolution != null)
        {
            CanonicalFormViewModel.LoadCanonicalForm(CurrentSolution.OriginalProblem, CurrentSolution.CanonicalForm);
            CurrentViewModel = CanonicalFormViewModel;
            StatusMessage = "Canonical Form view";
        }
        else
        {
            StatusMessage = "No problem available. Please load or solve a problem first.";
        }
    }

    #endregion

    #region File Menu Commands

    [RelayCommand]
    public async Task NewProblemAsync()
    {
        CurrentModel = new LinearProgramModel { IsLoaded = true, FileName = "New Problem", FileContent = "max +2 +3 +3 +5 +2 +4\n+11 +8 +6 +14 +10 +10 <= 40\nbin bin bin bin bin bin" };
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
    
    [RelayCommand]
    private async Task ExportResults()
    {
        if (CurrentSolution == null)
        {
            StatusMessage = "No solution to export";
            return;
        }

        try
        {
            var topLevel = TopLevel.GetTopLevel(_window);
            var file = await topLevel!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export Results",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Text files") { Patterns = new[] { "*.txt" } },
                    new FilePickerFileType("Output files") { Patterns = new[] { "*.out" } }
                }
            });

            if (file != null)
            {
                await _solutionEngine.ExportResultsAsync(CurrentSolution, file.Path.LocalPath);
                StatusMessage = "Results exported successfully";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error exporting results: {ex.Message}";
        }
    }
    
    [RelayCommand] private void Exit() { Environment.Exit(0); }

    #endregion

    #region Algorithm Menu Commands
    
    [RelayCommand] 
    private void SelectPrimalSimplex() 
    { 
        SelectedAlgorithm = AlgorithmType.PrimalSimplex; 
        StatusMessage = "Primal Simplex algorithm selected"; 
    }
    
    [RelayCommand] 
    private void SelectRevisedPrimalSimplex() 
    { 
        SelectedAlgorithm = AlgorithmType.RevisedPrimalSimplex; 
        StatusMessage = "Revised Primal Simplex algorithm selected"; 
    }
    
    [RelayCommand] 
    private void SelectBranchAndBoundSimplex() 
    { 
        SelectedAlgorithm = AlgorithmType.BranchAndBoundSimplex; 
        StatusMessage = "Branch & Bound Simplex algorithm selected"; 
    }
    
    [RelayCommand] 
    private void SelectCuttingPlane() 
    { 
        SelectedAlgorithm = AlgorithmType.CuttingPlane; 
        StatusMessage = "Cutting Plane algorithm selected"; 
    }
    
    [RelayCommand] 
    private void SelectBranchAndBoundKnapsack() 
    { 
        SelectedAlgorithm = AlgorithmType.BranchAndBoundKnapsack; 
        StatusMessage = "Branch & Bound Knapsack algorithm selected"; 
    }
    
    [RelayCommand] 
    private async Task SolveProblem() 
    { 
        if (!CurrentModel.IsLoaded)
        {
            StatusMessage = "No problem loaded";
            return;
        }

        try
        {
            StatusMessage = $"Solving with {SelectedAlgorithm}...";
            CurrentSolution = await _solutionEngine.SolveAsync(CurrentModel.FileContent, SelectedAlgorithm);
            
            if (CurrentSolution.Success)
            {
                StatusMessage = $"Solution found: {CurrentSolution.Solution.Status}";
                NavigateToSolutionTable();
            }
            else
            {
                StatusMessage = $"Solution failed: {CurrentSolution.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error solving problem: {ex.Message}";
        }
    }
    
    [RelayCommand] private void ViewSolutionDetails() { NavigateToSolutionTable(); }

    #endregion

    #region Analysis Menu Commands

    // Variable Analysis
    [RelayCommand] 
    private void NonBasicVariableRange() 
    { 
        if (CurrentSolution?.Success == true)
        {
            // For demo, analyze first non-basic variable
            var nonBasic = CurrentSolution.Solution.NonBasicVariables.FirstOrDefault();
            if (nonBasic >= 0)
            {
                var result = _sensitivityService.AnalyzeNonBasicVariableRange(CurrentSolution, nonBasic);
                StatusMessage = $"Range for {result.VariableName}: [{result.LowerBound:F3}, {result.UpperBound:F3}]";
            }
        }
        else
        {
            StatusMessage = "No optimal solution available for sensitivity analysis";
        }
    }
    
    [RelayCommand] 
    private async Task ApplyNonBasicVariableAsync() 
    { 
        if (CurrentSolution?.Success != true)
        {
            StatusMessage = "No optimal solution available for sensitivity analysis";
            return;
        }

        var dialogViewModel = new VariableSelectionDialogViewModel();
        dialogViewModel.InitializeForNonBasicChange(CurrentSolution);
        
        var result = await _dialogService.ShowVariableSelectionDialogAsync(dialogViewModel, _window);
        if (result && dialogViewModel.SelectedVariable != null)
        {
            var sensitivityResult = _sensitivityService.ApplyVariableChange(
                CurrentSolution, 
                dialogViewModel.SelectedVariable.Index, 
                (double)dialogViewModel.NewValue);
                
            var resultViewModel = new SensitivityResultDialogViewModel();
            double newObjective = CurrentSolution.Solution.ObjectiveValue + (sensitivityResult.UpperBound - sensitivityResult.CurrentValue);
            resultViewModel.ShowChangeAnalysis(sensitivityResult, (double)dialogViewModel.NewValue, 
                sensitivityResult.UpperBound - sensitivityResult.CurrentValue, newObjective);
            
            await _dialogService.ShowSensitivityResultDialogAsync(resultViewModel, _window);
        }
    }
    
    [RelayCommand] 
    private void BasicVariableRange() 
    { 
        if (CurrentSolution?.Success == true)
        {
            // For demo, analyze first basic variable
            var basic = CurrentSolution.Solution.BasicVariables.FirstOrDefault();
            if (basic >= 0)
            {
                var result = _sensitivityService.AnalyzeBasicVariableRange(CurrentSolution, basic);
                StatusMessage = $"Range for {result.VariableName}: [{result.LowerBound:F3}, {result.UpperBound:F3}]";
            }
        }
        else
        {
            StatusMessage = "No optimal solution available for sensitivity analysis";
        }
    }
    
    [RelayCommand] 
    private async Task ApplyBasicVariableAsync() 
    { 
        if (CurrentSolution?.Success != true)
        {
            StatusMessage = "No optimal solution available for sensitivity analysis";
            return;
        }

        var dialogViewModel = new VariableSelectionDialogViewModel();
        dialogViewModel.InitializeForBasicChange(CurrentSolution);
        
        var result = await _dialogService.ShowVariableSelectionDialogAsync(dialogViewModel, _window);
        if (result && dialogViewModel.SelectedVariable != null)
        {
            var sensitivityResult = _sensitivityService.ApplyVariableChange(
                CurrentSolution, 
                dialogViewModel.SelectedVariable.Index, 
                (double)dialogViewModel.NewValue);
                
            var resultViewModel = new SensitivityResultDialogViewModel();
            double newObjective = CurrentSolution.Solution.ObjectiveValue + (sensitivityResult.UpperBound - sensitivityResult.CurrentValue);
            resultViewModel.ShowChangeAnalysis(sensitivityResult, (double)dialogViewModel.NewValue, 
                sensitivityResult.UpperBound - sensitivityResult.CurrentValue, newObjective);
            
            await _dialogService.ShowSensitivityResultDialogAsync(resultViewModel, _window);
        }
    }

    // Constraint Analysis
    [RelayCommand] 
    private void RhsValueRange() 
    { 
        if (CurrentSolution?.Success == true)
        {
            var result = _sensitivityService.AnalyzeRightHandSideRange(CurrentSolution, 0);
            StatusMessage = $"RHS range for {result.VariableName}: [{result.LowerBound:F3}, {result.UpperBound:F3}]";
        }
        else
        {
            StatusMessage = "No optimal solution available for sensitivity analysis";
        }
    }
    
    [RelayCommand] 
    private async Task ApplyRhsChangeAsync() 
    { 
        if (CurrentSolution?.Success != true)
        {
            StatusMessage = "No optimal solution available for sensitivity analysis";
            return;
        }

        var dialogViewModel = new ConstraintSelectionDialogViewModel();
        dialogViewModel.InitializeForRhsChange(CurrentSolution);
        
        var result = await _dialogService.ShowConstraintSelectionDialogAsync(dialogViewModel, _window);
        if (result && dialogViewModel.SelectedConstraint != null)
        {
            var sensitivityResult = _sensitivityService.ApplyRhsChange(
                CurrentSolution, 
                dialogViewModel.SelectedConstraint.Index, 
                (double)dialogViewModel.NewRhsValue);
                
            var resultViewModel = new SensitivityResultDialogViewModel();
            double newObjective = CurrentSolution.Solution.ObjectiveValue + (sensitivityResult.UpperBound - sensitivityResult.CurrentValue);
            resultViewModel.ShowChangeAnalysis(sensitivityResult, (double)dialogViewModel.NewRhsValue, 
                sensitivityResult.UpperBound - sensitivityResult.CurrentValue, newObjective);
            
            await _dialogService.ShowSensitivityResultDialogAsync(resultViewModel, _window);
        }
    }
    [RelayCommand] 
    private async Task NonBasicColumnRangeAsync() 
    { 
        if (CurrentSolution?.Success != true)
        {
            StatusMessage = "No optimal solution available for sensitivity analysis";
            return;
        }

        var dialogViewModel = new VariableSelectionDialogViewModel();
        dialogViewModel.InitializeForColumnRange(CurrentSolution);
        
        var result = await _dialogService.ShowVariableSelectionDialogAsync(dialogViewModel, _window);
        if (result && dialogViewModel.SelectedVariable != null)
        {
            // For simplicity, analyze the first constraint row
            int columnIndex = 0;
            var sensitivityResult = _sensitivityService.AnalyzeNonBasicVariableColumnRange(
                CurrentSolution, 
                dialogViewModel.SelectedVariable.Index, 
                columnIndex);
                
            var resultViewModel = new SensitivityResultDialogViewModel();
            resultViewModel.ShowRangeAnalysis(sensitivityResult);
            
            await _dialogService.ShowSensitivityResultDialogAsync(resultViewModel, _window);
        }
    }
    [RelayCommand] 
    private async Task ApplyColumnChangeAsync() 
    { 
        if (CurrentSolution?.Success != true)
        {
            StatusMessage = "No optimal solution available for sensitivity analysis";
            return;
        }

        var dialogViewModel = new VariableSelectionDialogViewModel();
        dialogViewModel.InitializeForColumnChange(CurrentSolution);
        
        var result = await _dialogService.ShowVariableSelectionDialogAsync(dialogViewModel, _window);
        if (result && dialogViewModel.SelectedVariable != null)
        {
            // For simplicity, apply change to the first constraint row
            int columnIndex = 0;
            var sensitivityResult = _sensitivityService.ApplyNonBasicVariableColumnChange(
                CurrentSolution, 
                dialogViewModel.SelectedVariable.Index, 
                columnIndex,
                (double)dialogViewModel.NewValue);
                
            var resultViewModel = new SensitivityResultDialogViewModel();
            double newObjective = CurrentSolution.Solution.ObjectiveValue;
            resultViewModel.ShowChangeAnalysis(sensitivityResult, (double)dialogViewModel.NewValue, 0.0, newObjective);
            
            await _dialogService.ShowSensitivityResultDialogAsync(resultViewModel, _window);
        }
    }

    // Model Modification
    [RelayCommand] 
    private async Task AddNewActivityAsync() 
    { 
        if (CurrentSolution?.Success != true)
        {
            StatusMessage = "No optimal solution available for adding activities";
            return;
        }

        var dialogViewModel = new VariableSelectionDialogViewModel();
        dialogViewModel.InitializeForNewActivity(CurrentSolution);
        
        var result = await _dialogService.ShowVariableSelectionDialogAsync(dialogViewModel, _window);
        if (result)
        {
            // Create sample activity coefficients (simplified)
            double[] activityCoeffs = new double[CurrentSolution.CanonicalForm.ConstraintCount];
            for (int i = 0; i < activityCoeffs.Length; i++)
            {
                activityCoeffs[i] = 1.0; // Default coefficients
            }
            
            var sensitivityResult = _sensitivityService.AddNewActivity(
                CurrentSolution, 
                activityCoeffs,
                (double)dialogViewModel.NewValue); // Use NewValue as objective coefficient
                
            var resultViewModel = new SensitivityResultDialogViewModel();
            resultViewModel.ShowRangeAnalysis(sensitivityResult);
            
            await _dialogService.ShowSensitivityResultDialogAsync(resultViewModel, _window);
        }
    }
    [RelayCommand] 
    private async Task AddNewConstraintAsync() 
    { 
        if (CurrentSolution?.Success != true)
        {
            StatusMessage = "No optimal solution available for adding constraints";
            return;
        }

        var dialogViewModel = new ConstraintSelectionDialogViewModel();
        dialogViewModel.InitializeForNewConstraint(CurrentSolution);
        
        var result = await _dialogService.ShowConstraintSelectionDialogAsync(dialogViewModel, _window);
        if (result)
        {
            // Create sample constraint coefficients (simplified)
            double[] constraintCoeffs = new double[CurrentSolution.CanonicalForm.TotalVariableCount];
            for (int i = 0; i < Math.Min(constraintCoeffs.Length, CurrentSolution.Solution.Variables.Length); i++)
            {
                constraintCoeffs[i] = 1.0; // Default coefficients
            }
            
            var sensitivityResult = _sensitivityService.AddNewConstraint(
                CurrentSolution, 
                constraintCoeffs,
                ConstraintType.LessEqual,
                (double)dialogViewModel.NewRhsValue);
                
            var resultViewModel = new SensitivityResultDialogViewModel();
            resultViewModel.ShowRangeAnalysis(sensitivityResult);
            
            await _dialogService.ShowSensitivityResultDialogAsync(resultViewModel, _window);
        }
    }

    // Shadow Prices
    [RelayCommand] 
    private async Task ShadowPricesAsync() 
    { 
        if (CurrentSolution?.Success != true)
        {
            StatusMessage = "No optimal solution available for shadow price analysis";
            return;
        }

        var prices = _sensitivityService.CalculateShadowPrices(CurrentSolution);
        
        var resultViewModel = new SensitivityResultDialogViewModel();
        resultViewModel.ShowShadowPriceAnalysis(prices);
        
        await _dialogService.ShowSensitivityResultDialogAsync(resultViewModel, _window);
    }

    // Duality
    [RelayCommand] 
    private void GenerateDualProblem() 
    { 
        if (CurrentSolution?.Success == true)
        {
            var dual = _sensitivityService.GenerateDualProblem(CurrentSolution.OriginalProblem);
            var dualString = _sensitivityService.FormatDualProblem(dual);
            StatusMessage = "Dual problem generated - check output";
            // Could show in a dialog or save to file
        }
        else
        {
            StatusMessage = "No problem available to generate dual";
        }
    }
    
    [RelayCommand] 
    private async Task SolveDualProblem() 
    { 
        if (CurrentSolution?.Success != true)
        {
            StatusMessage = "No problem available to solve dual";
            return;
        }

        try
        {
            StatusMessage = "Generating and solving dual problem...";
            
            var dual = _sensitivityService.GenerateDualProblem(CurrentSolution.OriginalProblem);
            var dualString = _sensitivityService.FormatDualProblem(dual);
            
            // Convert dual problem to LP format and solve
            var dualSolution = await _solutionEngine.SolveAsync(dualString, SelectedAlgorithm);
            
            if (dualSolution.Success)
            {
                StatusMessage = $"Dual problem solved: {dualSolution.Solution.Status}, Objective: {dualSolution.Solution.ObjectiveValue:F3}";
                
                // Show dual solution in dialog
                var resultViewModel = new SensitivityResultDialogViewModel();
                resultViewModel.ShowDualSolutionAnalysis(CurrentSolution, dualSolution);
                await _dialogService.ShowSensitivityResultDialogAsync(resultViewModel, _window);
            }
            else
            {
                StatusMessage = $"Failed to solve dual problem: {dualSolution.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error solving dual problem: {ex.Message}";
        }
    }
    
    [RelayCommand] 
    private void VerifyDualityType() 
    { 
        if (CurrentSolution?.Success != true)
        {
            StatusMessage = "No problem available to verify duality";
            return;
        }
        
        try
        {
            var dualityType = _sensitivityService.VerifyDualityType(CurrentSolution);
            StatusMessage = $"Duality verification: {dualityType}";
            
            // Could show detailed analysis in dialog
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error verifying duality: {ex.Message}";
        }
    }

    #endregion

    #region View Menu Commands

    [RelayCommand] private void SolutionTable() { NavigateToSolutionTable(); }
    [RelayCommand] private void TableauIterations() { NavigateToTableauIterations(); }
    [RelayCommand] private void CanonicalForm() { NavigateToCanonicalForm(); }
    [RelayCommand] private void FullScreen() { StatusMessage = "Full Screen toggled"; }
    [RelayCommand] private void StatusBar() { StatusMessage = "Status Bar toggled"; }

    #endregion
}