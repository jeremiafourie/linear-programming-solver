using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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

    public void LoadSolution(/* SolutionResult solution */)
    {
        StatusMessage = "Solution loaded";
        SolutionStatus = "Optimal";
        ObjectiveValue = 23.000;
        AlgorithmUsed = "Primal Simplex";
        Iterations = 2;
        
        // Clear and populate collections
        Variables.Clear();
        Variables.Add(new VariableResult { Name = "x1", Value = 2.000, Status = "Basic", ShadowPrice = 0.000 });
        Variables.Add(new VariableResult { Name = "x2", Value = 3.000, Status = "Basic", ShadowPrice = 0.000 });
        Variables.Add(new VariableResult { Name = "x3", Value = 0.000, Status = "Non-Basic", ShadowPrice = 1.500 });
        Variables.Add(new VariableResult { Name = "x4", Value = 0.000, Status = "Non-Basic", ShadowPrice = 0.000 });

        Constraints.Clear();
        Constraints.Add(new ConstraintResult { Name = "Constraint 1", Slack = 0.000, Status = "Binding", ShadowPrice = 2.000 });
        Constraints.Add(new ConstraintResult { Name = "Constraint 2", Slack = 5.000, Status = "Non-Binding", ShadowPrice = 0.000 });
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
        LoadSolution();
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