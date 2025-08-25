using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Media;
using linear_programming_solver.Services;

namespace linear_programming_solver.ViewModels.Dialogs;

public partial class SensitivityResultDialogViewModel : BaseDialogViewModel
{
    [ObservableProperty]
    private string _analysisType = "";

    [ObservableProperty]
    private string _variableName = "";

    [ObservableProperty]
    private double _currentValue = 0;

    [ObservableProperty]
    private string _status = "";

    [ObservableProperty]
    private IBrush _statusColor = Brushes.Black;

    [ObservableProperty]
    private bool _showRange = false;

    [ObservableProperty]
    private double _lowerBound = 0;

    [ObservableProperty]
    private double _upperBound = 0;

    [ObservableProperty]
    private double _rangeSize = 0;

    [ObservableProperty]
    private bool _showImpactAnalysis = false;

    [ObservableProperty]
    private double _newValue = 0;

    [ObservableProperty]
    private double _objectiveChange = 0;

    [ObservableProperty]
    private double _newObjectiveValue = 0;

    [ObservableProperty]
    private bool _showShadowPrices = false;

    [ObservableProperty]
    private string _detailedDescription = "";

    public ObservableCollection<ShadowPrice> ShadowPrices { get; } = new();

    public void ShowRangeAnalysis(SensitivityResult result)
    {
        DialogTitle = "Sensitivity Range Analysis";
        AnalysisType = "Range Analysis";
        VariableName = result.VariableName;
        CurrentValue = result.CurrentValue;
        Status = result.Success ? "Valid" : "Error";
        StatusColor = result.Success ? Brushes.Green : Brushes.Red;

        ShowRange = result.Success;
        ShowImpactAnalysis = false;
        ShowShadowPrices = false;

        if (result.Success)
        {
            LowerBound = result.LowerBound;
            UpperBound = result.UpperBound;
            RangeSize = double.IsInfinity(result.UpperBound) || double.IsInfinity(result.LowerBound) ? 
                       double.PositiveInfinity : result.UpperBound - result.LowerBound;
            
            DetailedDescription = result.Description + "\n\n" +
                                 $"The variable {result.VariableName} can vary between {result.LowerBound:F3} and {result.UpperBound:F3} " +
                                 "without changing the optimal basis. Changes outside this range may lead to a different optimal solution.";
        }
        else
        {
            DetailedDescription = $"Error: {result.ErrorMessage}";
        }
    }

    public void ShowChangeAnalysis(SensitivityResult result, double newValue, double objectiveChange, double newObjectiveValue)
    {
        DialogTitle = "Sensitivity Change Analysis";
        AnalysisType = "Change Analysis";
        VariableName = result.VariableName;
        CurrentValue = result.CurrentValue;
        Status = result.Success ? "Applied" : "Error";
        StatusColor = result.Success ? Brushes.Green : Brushes.Red;

        ShowRange = false;
        ShowImpactAnalysis = result.Success;
        ShowShadowPrices = false;

        if (result.Success)
        {
            NewValue = newValue;
            ObjectiveChange = objectiveChange;
            NewObjectiveValue = newObjectiveValue;
            
            DetailedDescription = result.Description + "\n\n" +
                                 $"Changing {result.VariableName} from {result.CurrentValue:F3} to {newValue:F3} " +
                                 $"results in an objective function change of {objectiveChange:F3}, " +
                                 $"giving a new objective value of {newObjectiveValue:F3}.";
        }
        else
        {
            DetailedDescription = $"Error: {result.ErrorMessage}";
        }
    }

    public void ShowShadowPriceAnalysis(System.Collections.Generic.List<ShadowPrice> shadowPrices)
    {
        DialogTitle = "Shadow Price Analysis";
        AnalysisType = "Shadow Prices";
        VariableName = "All Constraints";
        Status = "Calculated";
        StatusColor = Brushes.Green;

        ShowRange = false;
        ShowImpactAnalysis = false;
        ShowShadowPrices = true;

        ShadowPrices.Clear();
        foreach (var price in shadowPrices)
        {
            ShadowPrices.Add(price);
        }

        DetailedDescription = "Shadow prices represent the marginal value of relaxing each constraint by one unit. " +
                             "A positive shadow price indicates that increasing the RHS of that constraint would improve " +
                             "the objective function value. A zero shadow price means the constraint is not binding.";
    }

    [RelayCommand]
    private void Export()
    {
        // TODO: Implement export functionality
        StatusMessage = "Export functionality not yet implemented";
    }
}