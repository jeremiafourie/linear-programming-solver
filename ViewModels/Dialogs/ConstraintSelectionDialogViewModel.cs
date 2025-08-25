using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using linear_programming_solver.Services;

namespace linear_programming_solver.ViewModels.Dialogs;

public partial class ConstraintSelectionDialogViewModel : BaseDialogViewModel
{
    [ObservableProperty]
    private string _description = "";

    [ObservableProperty]
    private bool _showRhsInput = false;

    [ObservableProperty]
    private decimal _newRhsValue = 0;

    [ObservableProperty]
    private ConstraintInfo? _selectedConstraint;

    public ObservableCollection<ConstraintInfo> AvailableConstraints { get; } = new();

    public void InitializeForRhsRange(SolutionResult solution)
    {
        DialogTitle = "RHS Value Range Analysis";
        Description = "Select a constraint to analyze the allowable range of its right-hand-side value while maintaining optimality.";
        ShowRhsInput = false;
        
        LoadConstraints(solution);
    }

    public void InitializeForRhsChange(SolutionResult solution)
    {
        DialogTitle = "Apply RHS Value Change";
        Description = "Select a constraint and specify a new right-hand-side value to analyze the impact on the solution.";
        ShowRhsInput = true;
        
        LoadConstraints(solution);
    }

    private void LoadConstraints(SolutionResult solution)
    {
        AvailableConstraints.Clear();
        
        if (solution.Success && solution.CanonicalForm != null)
        {
            for (int i = 0; i < solution.CanonicalForm.ConstraintCount; i++)
            {
                var constraint = new ConstraintInfo
                {
                    Index = i,
                    Name = $"Constraint {i + 1}",
                    Description = GenerateConstraintDescription(solution, i),
                    CurrentRHS = solution.CanonicalForm.RightHandSide[i]
                };
                AvailableConstraints.Add(constraint);
            }
        }
        
        SelectedConstraint = AvailableConstraints.FirstOrDefault();
        if (SelectedConstraint != null)
        {
            NewRhsValue = (decimal)SelectedConstraint.CurrentRHS;
        }
    }

    private string GenerateConstraintDescription(SolutionResult solution, int constraintIndex)
    {
        // Generate a description showing the constraint in mathematical form
        var terms = new List<string>();
        var canonical = solution.CanonicalForm;
        
        for (int j = 0; j < canonical.TotalVariableCount; j++)
        {
            var coeff = canonical.ConstraintMatrix[constraintIndex, j];
            if (System.Math.Abs(coeff) > 1e-10)
            {
                var sign = coeff >= 0 && terms.Count > 0 ? "+" : "";
                var varName = canonical.GetVariableName(j);
                terms.Add($"{sign}{coeff:F1}{varName}");
            }
        }
        
        return $"{string.Join(" ", terms)} = {canonical.RightHandSide[constraintIndex]:F3}";
    }

    public void InitializeForNewConstraint(SolutionResult solution)
    {
        DialogTitle = "Add New Constraint";
        Description = "Specify the right-hand-side value for the new constraint to be added to the problem.";
        ShowRhsInput = true;
        
        // For adding new constraint, we create a placeholder
        AvailableConstraints.Clear();
        AvailableConstraints.Add(new ConstraintInfo
        {
            Index = -1,
            Name = "New Constraint",
            Description = "New constraint to be added (x1 + x2 + ... <= RHS)",
            CurrentRHS = 0.0
        });
        
        SelectedConstraint = AvailableConstraints.FirstOrDefault();
        NewRhsValue = 10.0m; // Default RHS value
    }

    protected override bool ValidateInput()
    {
        if (SelectedConstraint == null)
        {
            StatusMessage = "Please select a constraint";
            return false;
        }

        return true;
    }
}

public class ConstraintInfo
{
    public int Index { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public double CurrentRHS { get; set; }
}