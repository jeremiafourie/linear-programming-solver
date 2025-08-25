using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using linear_programming_solver.Services;

namespace linear_programming_solver.ViewModels;

public partial class SolutionTableViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _problemType = "Maximization";

    [ObservableProperty]
    private string _solutionStatus = "Not Solved";

    [ObservableProperty]
    private double _objectiveValue = 0.0;

    [ObservableProperty]
    private string _algorithmUsed = "None";

    [ObservableProperty]
    private int _iterations = 0;

    public ObservableCollection<VariableResult> Variables { get; } = new();
    public ObservableCollection<ConstraintResult> Constraints { get; } = new();

    public SolutionTableViewModel()
    {
        LoadSampleData();
    }

    public void LoadSolution(SolutionResult solution)
    {
        if (solution.Success && solution.Solution != null)
        {
            SolutionStatus = solution.Solution.Status.ToString();
            ObjectiveValue = solution.Solution.ObjectiveValue;
            AlgorithmUsed = solution.Solution.Algorithm;
            Iterations = solution.Solution.Iterations.Count;
            ProblemType = solution.CanonicalForm.IsMaximization ? "Maximization" : "Minimization";
            
            // Clear and populate variables
            Variables.Clear();
            for (int i = 0; i < solution.Solution.Variables.Length; i++)
            {
                var variableName = solution.CanonicalForm.GetVariableName(i);
                var value = solution.Solution.Variables[i];
                var isBasic = solution.Solution.BasicVariables.Contains(i);
                
                Variables.Add(new VariableResult 
                { 
                    Name = variableName, 
                    Value = value, 
                    Status = isBasic ? "Basic" : "Non-Basic", 
                    ShadowPrice = 0.000 // TODO: Calculate shadow prices
                });
            }

            // Clear constraints (simplified for now)
            Constraints.Clear();
            for (int i = 0; i < solution.CanonicalForm.ConstraintCount; i++)
            {
                Constraints.Add(new ConstraintResult 
                { 
                    Name = $"Constraint {i + 1}", 
                    Slack = 0.000, // TODO: Calculate slack values
                    Status = "Active", 
                    ShadowPrice = 0.000 // TODO: Calculate shadow prices
                });
            }
            
            StatusMessage = $"Solution loaded: {SolutionStatus}";
        }
        else
        {
            SolutionStatus = "Error";
            StatusMessage = solution.ErrorMessage ?? "Unknown error";
        }
    }

    private void LoadSampleData()
    {
        Variables.Add(new VariableResult { Name = "x1", Value = 0.000, Status = "Not Solved", ShadowPrice = 0.000 });
        Variables.Add(new VariableResult { Name = "x2", Value = 0.000, Status = "Not Solved", ShadowPrice = 0.000 });
        Variables.Add(new VariableResult { Name = "x3", Value = 0.000, Status = "Not Solved", ShadowPrice = 0.000 });

        Constraints.Add(new ConstraintResult { Name = "Constraint 1", Slack = 0.000, Status = "Not Solved", ShadowPrice = 0.000 });
        Constraints.Add(new ConstraintResult { Name = "Constraint 2", Slack = 0.000, Status = "Not Solved", ShadowPrice = 0.000 });
    }

    [RelayCommand]
    private void ExportSolution()
    {
        StatusMessage = "Exporting solution...";
    }

    [RelayCommand]
    private void RefreshSolution()
    {
        StatusMessage = "Refreshing solution data...";
        // LoadSolution(); - requires parameter now
    }
}

public class VariableResult
{
    public string Name { get; set; } = "";
    public double Value { get; set; }
    public string Status { get; set; } = "";
    public double ShadowPrice { get; set; }
}

public class ConstraintResult
{
    public string Name { get; set; } = "";
    public double Slack { get; set; }
    public string Status { get; set; } = "";
    public double ShadowPrice { get; set; }
}