using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using linear_programming_solver.Services;

namespace linear_programming_solver.ViewModels.Dialogs;

public partial class VariableSelectionDialogViewModel : BaseDialogViewModel
{
    [ObservableProperty]
    private string _description = "";

    [ObservableProperty]
    private bool _showValueInput = false;

    [ObservableProperty]
    private string _valueInputLabel = "New Value:";

    [ObservableProperty]
    private decimal _newValue = 0;

    [ObservableProperty]
    private VariableInfo? _selectedVariable;

    public ObservableCollection<VariableInfo> AvailableVariables { get; } = new();

    public void InitializeForNonBasicRange(SolutionResult solution)
    {
        DialogTitle = "Non-Basic Variable Range Analysis";
        Description = "Select a non-basic variable to analyze its allowable range while maintaining optimality.";
        ShowValueInput = false;
        
        LoadNonBasicVariables(solution);
    }

    public void InitializeForBasicRange(SolutionResult solution)
    {
        DialogTitle = "Basic Variable Range Analysis";
        Description = "Select a basic variable to analyze its allowable range while maintaining optimality.";
        ShowValueInput = false;
        
        LoadBasicVariables(solution);
    }

    public void InitializeForNonBasicChange(SolutionResult solution)
    {
        DialogTitle = "Apply Non-Basic Variable Change";
        Description = "Select a non-basic variable and specify a new value to analyze the impact on the solution.";
        ShowValueInput = true;
        ValueInputLabel = "New Value:";
        
        LoadNonBasicVariables(solution);
    }

    public void InitializeForBasicChange(SolutionResult solution)
    {
        DialogTitle = "Apply Basic Variable Change";
        Description = "Select a basic variable and specify a new value to analyze the impact on the solution.";
        ShowValueInput = true;
        ValueInputLabel = "New Value:";
        
        LoadBasicVariables(solution);
    }

    private void LoadNonBasicVariables(SolutionResult solution)
    {
        AvailableVariables.Clear();
        
        if (solution.Success && solution.Solution != null)
        {
            foreach (int varIndex in solution.Solution.NonBasicVariables)
            {
                var variable = new VariableInfo
                {
                    Index = varIndex,
                    Name = solution.CanonicalForm.GetVariableName(varIndex),
                    CurrentValue = solution.Solution.Variables[varIndex],
                    Status = "Non-Basic"
                };
                AvailableVariables.Add(variable);
            }
        }
        
        SelectedVariable = AvailableVariables.FirstOrDefault();
    }

    private void LoadBasicVariables(SolutionResult solution)
    {
        AvailableVariables.Clear();
        
        if (solution.Success && solution.Solution != null)
        {
            foreach (int varIndex in solution.Solution.BasicVariables)
            {
                var variable = new VariableInfo
                {
                    Index = varIndex,
                    Name = solution.CanonicalForm.GetVariableName(varIndex),
                    CurrentValue = solution.Solution.Variables[varIndex],
                    Status = "Basic"
                };
                AvailableVariables.Add(variable);
            }
        }
        
        SelectedVariable = AvailableVariables.FirstOrDefault();
    }

    public void InitializeForColumnRange(SolutionResult solution)
    {
        DialogTitle = "Non-Basic Variable Column Range Analysis";
        Description = "Select a non-basic variable to analyze the allowable range of its technological coefficient in the constraint matrix.";
        ShowValueInput = false;
        
        LoadVariables(solution, includeBasic: false, includeNonBasic: true);
    }

    public void InitializeForColumnChange(SolutionResult solution)
    {
        DialogTitle = "Apply Technological Coefficient Change";
        Description = "Select a non-basic variable and specify a new technological coefficient value for analysis.";
        ShowValueInput = true;
        ValueInputLabel = "New Coefficient:";
        
        LoadVariables(solution, includeBasic: false, includeNonBasic: true);
    }

    public void InitializeForNewActivity(SolutionResult solution)
    {
        DialogTitle = "Add New Activity";
        Description = "Specify the objective function coefficient for the new activity to be added to the problem.";
        ShowValueInput = true;
        ValueInputLabel = "Objective Coefficient:";
        
        // For adding new activity, we don't need to select existing variables
        AvailableVariables.Clear();
        AvailableVariables.Add(new VariableInfo
        {
            Index = -1,
            Name = "New Activity",
            CurrentValue = 0.0,
            Status = "New"
        });
        
        SelectedVariable = AvailableVariables.FirstOrDefault();
        NewValue = 1.0m; // Default objective coefficient
    }

    private void LoadVariables(SolutionResult solution, bool includeBasic, bool includeNonBasic)
    {
        AvailableVariables.Clear();
        
        if (solution.Success && solution.Solution != null)
        {
            if (includeNonBasic)
            {
                foreach (int varIndex in solution.Solution.NonBasicVariables)
                {
                    var variable = new VariableInfo
                    {
                        Index = varIndex,
                        Name = solution.CanonicalForm.GetVariableName(varIndex),
                        CurrentValue = solution.Solution.Variables[varIndex],
                        Status = "Non-Basic"
                    };
                    AvailableVariables.Add(variable);
                }
            }
            
            if (includeBasic)
            {
                foreach (int varIndex in solution.Solution.BasicVariables)
                {
                    var variable = new VariableInfo
                    {
                        Index = varIndex,
                        Name = solution.CanonicalForm.GetVariableName(varIndex),
                        CurrentValue = solution.Solution.Variables[varIndex],
                        Status = "Basic"
                    };
                    AvailableVariables.Add(variable);
                }
            }
        }
        
        SelectedVariable = AvailableVariables.FirstOrDefault();
    }

    protected override bool ValidateInput()
    {
        if (SelectedVariable == null)
        {
            StatusMessage = "Please select a variable";
            return false;
        }

        return true;
    }
}

public class VariableInfo
{
    public int Index { get; set; }
    public string Name { get; set; } = "";
    public double CurrentValue { get; set; }
    public string Status { get; set; } = "";
}