using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace linear_programming_solver.Models;

public partial class TableauData : ObservableObject
{
    [ObservableProperty]
    private int _iterationNumber;
    
    [ObservableProperty] 
    private string _pivotInfo = "";
    
    [ObservableProperty]
    private bool _isOptimal;
    
    public ObservableCollection<TableauRow> Rows { get; set; } = new();
    public ObservableCollection<string> VariableColumns { get; set; } = new();
    public string Title => IsOptimal ? $"Optimal Solution" : $"Iteration {IterationNumber}";
}

public partial class TableauRow : ObservableObject
{
    [ObservableProperty]
    private string _basisVariable = "";
    
    [ObservableProperty]
    private double _rhs;
    
    public ObservableCollection<double> Coefficients { get; set; } = new();
    
    public string FormattedRhs => $"{Rhs:F3}";
    
    public ObservableCollection<string> FormattedCoefficients => 
        new(Coefficients.Select(c => $"{c:F3}"));
}

public partial class TableauNode : ObservableObject
{
    [ObservableProperty]
    private string _id = "";
    
    [ObservableProperty]
    private TableauData _tableauData = new();
    
    [ObservableProperty]
    private int _iterationIndex;
    
    [ObservableProperty]
    private bool _isOptimal;
    
    [ObservableProperty]
    private string _pivotInfo = "";
    
    public string DisplayTitle => IsOptimal ? "Optimal" : $"T{IterationIndex}";
}

public class TableauBranch
{
    public int FromIteration { get; set; }
    public int ToIteration { get; set; }
    public string PivotOperation { get; set; } = "";
    public string EdgeId { get; set; } = "";
}