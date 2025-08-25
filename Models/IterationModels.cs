using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace linear_programming_solver.Models;

// Generic iteration data that can be used for all algorithm types
public partial class IterationData : ObservableObject
{
    [ObservableProperty]
    private int _iterationNumber;
    
    [ObservableProperty] 
    private string _algorithmType = ""; // Primal Simplex, Branch & Bound, etc.
    
    [ObservableProperty] 
    private string _description = "";
    
    [ObservableProperty]
    private string _status = ""; // In Progress, Optimal, Infeasible, etc.
    
    [ObservableProperty]
    private bool _isOptimal;
    
    [ObservableProperty]
    private bool _isFinal;
    
    [ObservableProperty]
    private string _nodeType = "Iteration"; // Iteration, Branch, Cut, etc.
    
    // Tableau data for display in DataGrid
    public ObservableCollection<TableauRow> TableauRows { get; set; } = new();
    public ObservableCollection<string> VariableColumns { get; set; } = new();
    
    // Generic data storage for different algorithm types
    public Dictionary<string, object> Data { get; set; } = new();
    
    public string Title => IsOptimal ? "Optimal Solution" : 
                          IsFinal ? "Final Result" :
                          $"{NodeType} {IterationNumber}";
    
    public string DisplayText => $"{Title}\n{Description}";
}

// TableauRow is defined in TableauModels.cs and will be imported

// Edge/branch information between iterations
public class IterationBranch
{
    public int FromIteration { get; set; }
    public int ToIteration { get; set; }
    public string BranchCondition { get; set; } = ""; // x1 <= 2, Cut added, etc.
    public string EdgeId { get; set; } = "";
}