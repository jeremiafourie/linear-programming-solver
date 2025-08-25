using linear_programming_solver.Models;
using linear_programming_solver.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace linear_programming_solver.ViewModels;

public partial class ProblemEditorViewModel : ViewModelBase
{
    private readonly SolutionEngine _solutionEngine;

    [ObservableProperty]
    private LinearProgramModel _currentModel = new();

    [ObservableProperty]
    private ObservableCollection<AlgorithmOption> _availableAlgorithms = new();

    [ObservableProperty]
    private AlgorithmOption? _selectedAlgorithm;

    [ObservableProperty]
    private bool _canSolve = false;

    [ObservableProperty]
    private bool _isSolving = false;

    public event Action<SolutionResult>? SolutionCompleted;

    public ProblemEditorViewModel()
    {
        _solutionEngine = new SolutionEngine();
        InitializeAlgorithms();
        
        // Set default algorithm
        SelectedAlgorithm = AvailableAlgorithms.FirstOrDefault();
        
        // Update CanSolve when content or algorithm changes
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(CurrentModel) || 
                e.PropertyName == nameof(SelectedAlgorithm))
            {
                UpdateCanSolve();
            }
        };
    }

    private void InitializeAlgorithms()
    {
        AvailableAlgorithms.Clear();
        AvailableAlgorithms.Add(new AlgorithmOption(AlgorithmType.PrimalSimplex, "Primal Simplex", "Standard primal simplex algorithm for LP problems"));
        AvailableAlgorithms.Add(new AlgorithmOption(AlgorithmType.RevisedPrimalSimplex, "Revised Primal Simplex", "Revised primal simplex using product form"));
        AvailableAlgorithms.Add(new AlgorithmOption(AlgorithmType.BranchAndBoundSimplex, "Branch & Bound Simplex", "Branch and bound for integer programming"));
        AvailableAlgorithms.Add(new AlgorithmOption(AlgorithmType.CuttingPlane, "Cutting Plane", "Cutting plane algorithm for integer programming"));
        AvailableAlgorithms.Add(new AlgorithmOption(AlgorithmType.BranchAndBoundKnapsack, "Branch & Bound Knapsack", "Specialized knapsack branch and bound"));
    }

    private void UpdateCanSolve()
    {
        CanSolve = !IsSolving && 
                   !string.IsNullOrWhiteSpace(CurrentModel?.FileContent) && 
                   SelectedAlgorithm != null;
    }

    public void LoadProblem(LinearProgramModel model)
    {
        CurrentModel = model;
        StatusMessage = $"Editing: {model.FileName}";
        UpdateValidAlgorithms();
        UpdateCanSolve();
    }

    private void UpdateValidAlgorithms()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(CurrentModel?.FileContent))
            {
                // Show all algorithms if no content
                foreach (var alg in AvailableAlgorithms)
                    alg.IsValid = true;
                return;
            }

            var linearProgram = LinearProgramParser.Parse(CurrentModel.FileContent);
            
            foreach (var algorithmOption in AvailableAlgorithms)
            {
                algorithmOption.IsValid = IsAlgorithmValidForProblem(algorithmOption.Algorithm, linearProgram);
            }

            // If current selection is no longer valid, select first valid option
            if (SelectedAlgorithm != null && !SelectedAlgorithm.IsValid)
            {
                SelectedAlgorithm = AvailableAlgorithms.FirstOrDefault(a => a.IsValid);
            }
        }
        catch
        {
            // If parsing fails, disable algorithms that require specific problem types
            foreach (var alg in AvailableAlgorithms)
            {
                alg.IsValid = alg.Algorithm == AlgorithmType.PrimalSimplex || alg.Algorithm == AlgorithmType.RevisedPrimalSimplex;
            }
        }
    }

    private bool IsAlgorithmValidForProblem(AlgorithmType algorithm, LinearProgram problem)
    {
        return algorithm switch
        {
            AlgorithmType.PrimalSimplex => true, // Works for all LP problems
            AlgorithmType.RevisedPrimalSimplex => true, // Works for all LP problems
            AlgorithmType.BranchAndBoundSimplex => problem.IsIntegerProgram, // Requires integer variables
            AlgorithmType.CuttingPlane => problem.IsIntegerProgram, // Requires integer variables
            AlgorithmType.BranchAndBoundKnapsack => problem.IsBinaryProgram && problem.Constraints.Count == 1, // Binary knapsack only
            _ => false
        };
    }

    [RelayCommand]
    private async Task SaveProblem()
    {
        try
        {
            if (CurrentModel == null || string.IsNullOrWhiteSpace(CurrentModel.FileContent))
            {
                StatusMessage = "No content to save";
                return;
            }

            // Create filename if not set
            if (string.IsNullOrWhiteSpace(CurrentModel.FileName))
            {
                CurrentModel.FileName = $"problem_{DateTime.Now:yyyyMMdd_HHmmss}.lp";
            }

            // Ensure .lp extension
            if (!CurrentModel.FileName.EndsWith(".lp", StringComparison.OrdinalIgnoreCase))
            {
                CurrentModel.FileName += ".lp";
            }

            // Save to file
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), CurrentModel.FileName);
            await File.WriteAllTextAsync(filePath, CurrentModel.FileContent);
            
            CurrentModel.IsLoaded = true;
            StatusMessage = $"Problem saved to: {CurrentModel.FileName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving problem: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SolveProblem()
    {
        if (!CanSolve || SelectedAlgorithm == null)
        {
            StatusMessage = "Cannot solve - check problem content and algorithm selection";
            return;
        }

        try
        {
            IsSolving = true;
            UpdateCanSolve();
            StatusMessage = $"Solving problem using {SelectedAlgorithm.DisplayName}...";

            var result = await _solutionEngine.SolveAsync(CurrentModel.FileContent, SelectedAlgorithm.Algorithm);

            if (result.Success)
            {
                StatusMessage = $"Solution completed: {result.Solution.Status} (Objective: {result.Solution.ObjectiveValue:F3})";
                
                // Save solution to file
                var outputFileName = Path.ChangeExtension(CurrentModel.FileName ?? "solution", ".txt");
                var outputPath = Path.Combine(Directory.GetCurrentDirectory(), $"output_{outputFileName}");
                await _solutionEngine.ExportResultsAsync(result, outputPath);
                
                StatusMessage += $" - Results exported to: {Path.GetFileName(outputPath)}";
                
                // Notify that solution is complete
                SolutionCompleted?.Invoke(result);
            }
            else
            {
                StatusMessage = $"Solution failed: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error solving problem: {ex.Message}";
        }
        finally
        {
            IsSolving = false;
            UpdateCanSolve();
        }
    }

    partial void OnCurrentModelChanged(LinearProgramModel? oldValue, LinearProgramModel newValue)
    {
        if (oldValue != null)
        {
            oldValue.PropertyChanged -= OnModelPropertyChanged;
        }

        if (newValue != null)
        {
            newValue.PropertyChanged += OnModelPropertyChanged;
        }

        UpdateValidAlgorithms();
        UpdateCanSolve();
    }

    private void OnModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LinearProgramModel.FileContent))
        {
            UpdateValidAlgorithms();
            UpdateCanSolve();
        }
    }
}

public partial class AlgorithmOption : ObservableObject
{
    public AlgorithmType Algorithm { get; }
    public string DisplayName { get; }
    public string Description { get; }

    [ObservableProperty]
    private bool _isValid = true;

    public AlgorithmOption(AlgorithmType algorithm, string displayName, string description)
    {
        Algorithm = algorithm;
        DisplayName = displayName;
        Description = description;
    }
}