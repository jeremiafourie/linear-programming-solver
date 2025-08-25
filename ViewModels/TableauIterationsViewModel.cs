using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using linear_programming_solver.Models;
using Avalonia.Media;

namespace linear_programming_solver.ViewModels;

public partial class TableauIterationsViewModel : ViewModelBase
{
    [ObservableProperty]
    private IterationTreeNode? _selectedIterationNode;

    [ObservableProperty]
    private IterationDetailsViewModel _selectedIterationDetails = new();

    public ObservableCollection<IterationTreeNode> IterationTree { get; } = new();
    public ObservableCollection<IterationData> AllIterations { get; } = new();

    public TableauIterationsViewModel()
    {
        LoadSampleIterationData();
        BuildIterationTree();
        SelectFirstIteration();
    }

    partial void OnSelectedIterationNodeChanged(IterationTreeNode? value)
    {
        if (value?.IterationData != null)
        {
            UpdateSelectedIterationDetails(value.IterationData);
        }
    }

    public void LoadIterations(List<IterationData> iterations)
    {
        StatusMessage = "Loading algorithm iterations...";
        
        AllIterations.Clear();
        
        foreach (var iteration in iterations)
        {
            AllIterations.Add(iteration);
        }
        
        BuildIterationTree();
        SelectFirstIteration();
    }

    private void LoadSampleIterationData()
    {
        AllIterations.Clear();
        
        // Sample Primal Simplex iterations
        var iteration0 = new IterationData
        {
            IterationNumber = 0,
            AlgorithmType = "Primal Simplex",
            Description = "Initial tableau - need to find entering variable",
            Status = "In Progress",
            IsOptimal = false,
            NodeType = "Initial"
        };
        
        iteration0.TableauRows.Add(new TableauRow 
        { 
            BasisVariable = "s1", 
            Coefficients = new ObservableCollection<double> { 1.000, 2.000, 1.000, 1.000, 0.000 }, 
            Rhs = 8.000 
        });
        iteration0.TableauRows.Add(new TableauRow 
        { 
            BasisVariable = "s2", 
            Coefficients = new ObservableCollection<double> { 2.000, 1.000, 0.000, 0.000, 1.000 }, 
            Rhs = 10.000 
        });
        iteration0.TableauRows.Add(new TableauRow 
        { 
            BasisVariable = "Z", 
            Coefficients = new ObservableCollection<double> { -2.000, -3.000, -1.000, 0.000, 0.000 }, 
            Rhs = 0.000 
        });
        foreach (var col in new[] { "x1", "x2", "x3", "s1", "s2" })
        {
            iteration0.VariableColumns.Add(col);
        }
        AllIterations.Add(iteration0);
        
        var iteration1 = new IterationData
        {
            IterationNumber = 1,
            AlgorithmType = "Primal Simplex",
            Description = "x2 enters, s1 leaves",
            Status = "In Progress",
            IsOptimal = false,
            NodeType = "Iteration"
        };
        
        iteration1.TableauRows.Add(new TableauRow 
        { 
            BasisVariable = "x2", 
            Coefficients = new ObservableCollection<double> { 0.500, 1.000, 0.500, 0.500, 0.000 }, 
            Rhs = 4.000 
        });
        iteration1.TableauRows.Add(new TableauRow 
        { 
            BasisVariable = "s2", 
            Coefficients = new ObservableCollection<double> { 1.500, 0.000, -0.500, -0.500, 1.000 }, 
            Rhs = 6.000 
        });
        iteration1.TableauRows.Add(new TableauRow 
        { 
            BasisVariable = "Z", 
            Coefficients = new ObservableCollection<double> { -0.500, 0.000, 0.500, 1.500, 0.000 }, 
            Rhs = 12.000 
        });
        foreach (var col in new[] { "x1", "x2", "x3", "s1", "s2" })
        {
            iteration1.VariableColumns.Add(col);
        }
        iteration1.Data["PivotRow"] = 0;
        iteration1.Data["PivotColumn"] = 1;
        AllIterations.Add(iteration1);
        
        var iteration2 = new IterationData
        {
            IterationNumber = 2,
            AlgorithmType = "Primal Simplex",
            Description = "Optimal solution reached",
            Status = "Optimal",
            IsOptimal = true,
            NodeType = "Final"
        };
        
        iteration2.TableauRows.Add(new TableauRow 
        { 
            BasisVariable = "x2", 
            Coefficients = new ObservableCollection<double> { 0.000, 1.000, 2.000, 1.000, -1.000 }, 
            Rhs = 2.000 
        });
        iteration2.TableauRows.Add(new TableauRow 
        { 
            BasisVariable = "x1", 
            Coefficients = new ObservableCollection<double> { 1.000, 0.000, -1.000, -1.000, 2.000 }, 
            Rhs = 4.000 
        });
        iteration2.TableauRows.Add(new TableauRow 
        { 
            BasisVariable = "Z", 
            Coefficients = new ObservableCollection<double> { 0.000, 0.000, 0.000, 1.000, 1.000 }, 
            Rhs = 14.000 
        });
        foreach (var col in new[] { "x1", "x2", "x3", "s1", "s2" })
        {
            iteration2.VariableColumns.Add(col);
        }
        AllIterations.Add(iteration2);
    }

    private void BuildIterationTree()
    {
        IterationTree.Clear();
        
        if (!AllIterations.Any()) return;
        
        // Group iterations by algorithm type
        var algorithmGroups = AllIterations.GroupBy(i => i.AlgorithmType).ToList();
        
        foreach (var group in algorithmGroups)
        {
            var algorithmNode = new IterationTreeNode
            {
                DisplayName = group.Key,
                NodeType = "Algorithm",
                Icon = GetAlgorithmIcon(group.Key),
                BackgroundBrush = GetAlgorithmColor(group.Key),
                BorderBrush = GetAlgorithmBorderColor(group.Key),
                FontWeight = FontWeight.Bold
            };
            
            foreach (var iteration in group.OrderBy(i => i.IterationNumber))
            {
                var iterationNode = new IterationTreeNode
                {
                    DisplayName = $"{iteration.Title}",
                    NodeType = iteration.NodeType,
                    IterationData = iteration,
                    Icon = GetIterationIcon(iteration),
                    BackgroundBrush = GetIterationColor(iteration),
                    BorderBrush = GetIterationBorderColor(iteration),
                    StatusIndicator = GetStatusIndicator(iteration),
                    StatusColor = GetStatusColor(iteration),
                    HasStatus = !string.IsNullOrEmpty(iteration.Status),
                    FontWeight = iteration.IsOptimal ? FontWeight.Bold : FontWeight.Normal
                };
                
                algorithmNode.Children.Add(iterationNode);
            }
            
            IterationTree.Add(algorithmNode);
        }
    }
    
    private void UpdateSelectedIterationDetails(IterationData iteration)
    {
        SelectedIterationDetails = new IterationDetailsViewModel
        {
            Title = iteration.Title,
            Description = iteration.Description,
            TableauRows = new ObservableCollection<EnhancedTableauRow>(),
            PivotInfo = GetPivotInfo(iteration),
            PivotDetails = GetPivotDetails(iteration),
            AdditionalInfo = GetAdditionalInfo(iteration),
            HasPivotInfo = !string.IsNullOrEmpty(GetPivotInfo(iteration)),
            HasPivotDetails = !string.IsNullOrEmpty(GetPivotDetails(iteration)),
            HasAdditionalInfo = !string.IsNullOrEmpty(GetAdditionalInfo(iteration)),
            PivotInfoBackground = iteration.IsOptimal ? "#E8F5E8" : "#FFF3CD",
            PivotInfoBorder = iteration.IsOptimal ? "#28A745" : "#FFC107"
        };
        
        // Convert TableauRows to EnhancedTableauRows with pivot highlighting
        var pivotRow = iteration.Data.TryGetValue("PivotRow", out var pRow) ? (int)pRow : -1;
        var pivotCol = iteration.Data.TryGetValue("PivotColumn", out var pCol) ? (int)pCol : -1;
        
        for (int i = 0; i < iteration.TableauRows.Count; i++)
        {
            var row = iteration.TableauRows[i];
            var enhancedRow = new EnhancedTableauRow
            {
                BasisVariable = row.BasisVariable,
                Coefficients = new ObservableCollection<double>(row.Coefficients),
                Rhs = row.Rhs,
                PivotRow = pivotRow,
                PivotColumn = pivotCol,
                RowIndex = i
            };
            SelectedIterationDetails.TableauRows.Add(enhancedRow);
        }
    }

    private void SelectFirstIteration()
    {
        var firstIterationNode = IterationTree
            .SelectMany(alg => alg.Children)
            .FirstOrDefault();
            
        if (firstIterationNode != null)
        {
            SelectedIterationNode = firstIterationNode;
        }
    }
    
    private string GetAlgorithmIcon(string algorithmType) => algorithmType switch
    {
        "Primal Simplex" => "PS",
        "Revised Primal Simplex" => "RPS",
        "Branch & Bound" => "BB",
        "Cutting Plane" => "CP",
        "Knapsack" => "KS",
        _ => "ALG"
    };
    
    private IBrush GetAlgorithmColor(string algorithmType) => algorithmType switch
    {
        "Primal Simplex" => new SolidColorBrush(Color.FromRgb(52, 144, 220)),
        "Revised Primal Simplex" => new SolidColorBrush(Color.FromRgb(40, 167, 69)),
        "Branch & Bound" => new SolidColorBrush(Color.FromRgb(255, 193, 7)),
        "Cutting Plane" => new SolidColorBrush(Color.FromRgb(220, 53, 69)),
        "Knapsack" => new SolidColorBrush(Color.FromRgb(102, 16, 242)),
        _ => new SolidColorBrush(Color.FromRgb(108, 117, 125))
    };
    
    private IBrush GetAlgorithmBorderColor(string algorithmType) => algorithmType switch
    {
        "Primal Simplex" => new SolidColorBrush(Color.FromRgb(0, 123, 255)),
        "Revised Primal Simplex" => new SolidColorBrush(Color.FromRgb(28, 125, 47)),
        "Branch & Bound" => new SolidColorBrush(Color.FromRgb(227, 172, 0)),
        "Cutting Plane" => new SolidColorBrush(Color.FromRgb(187, 45, 59)),
        "Knapsack" => new SolidColorBrush(Color.FromRgb(85, 13, 202)),
        _ => new SolidColorBrush(Color.FromRgb(73, 80, 87))
    };
    
    private string GetIterationIcon(IterationData iteration) => iteration.NodeType switch
    {
        "Initial" => "I",
        "Iteration" => "T",
        "Final" => "F",
        "Branch" => "B",
        "Cut" => "C",
        _ => "I"
    };
    
    private IBrush GetIterationColor(IterationData iteration)
    {
        if (iteration.IsOptimal) return new SolidColorBrush(Color.FromRgb(40, 167, 69));
        return iteration.NodeType switch
        {
            "Initial" => new SolidColorBrush(Color.FromRgb(108, 117, 125)),
            "Final" => new SolidColorBrush(Color.FromRgb(40, 167, 69)),
            _ => new SolidColorBrush(Color.FromRgb(52, 144, 220))
        };
    }
    
    private IBrush GetIterationBorderColor(IterationData iteration)
    {
        if (iteration.IsOptimal) return new SolidColorBrush(Color.FromRgb(28, 125, 47));
        return iteration.NodeType switch
        {
            "Initial" => new SolidColorBrush(Color.FromRgb(73, 80, 87)),
            "Final" => new SolidColorBrush(Color.FromRgb(28, 125, 47)),
            _ => new SolidColorBrush(Color.FromRgb(0, 123, 255))
        };
    }
    
    private string GetStatusIndicator(IterationData iteration) => iteration.Status switch
    {
        "Optimal" => "✓",
        "Infeasible" => "✗",
        "Unbounded" => "∞",
        "In Progress" => "⏳",
        _ => ""
    };
    
    private IBrush GetStatusColor(IterationData iteration) => iteration.Status switch
    {
        "Optimal" => new SolidColorBrush(Color.FromRgb(40, 167, 69)),
        "Infeasible" => new SolidColorBrush(Color.FromRgb(220, 53, 69)),
        "Unbounded" => new SolidColorBrush(Color.FromRgb(255, 193, 7)),
        "In Progress" => new SolidColorBrush(Color.FromRgb(52, 144, 220)),
        _ => new SolidColorBrush(Color.FromRgb(108, 117, 125))
    };
    
    private string GetPivotInfo(IterationData iteration)
    {
        if (iteration.IsOptimal) return "Optimal solution reached - no pivot needed";
        if (iteration.Data.TryGetValue("PivotRow", out var pRow) && iteration.Data.TryGetValue("PivotColumn", out var pCol))
        {
            return $"Pivot at row {(int)pRow + 1}, column {(int)pCol + 1}";
        }
        return iteration.Description;
    }
    
    private string GetPivotDetails(IterationData iteration)
    {
        if (iteration.Data.TryGetValue("PivotRow", out var pRow) && iteration.Data.TryGetValue("PivotColumn", out var pCol))
        {
            var row = (int)pRow;
            var col = (int)pCol;
            if (row < iteration.TableauRows.Count && col < iteration.TableauRows[row].Coefficients.Count)
            {
                var pivotValue = iteration.TableauRows[row].Coefficients[col];
                return $"Pivot element value: {pivotValue:F3}";
            }
        }
        return "";
    }
    
    private string GetAdditionalInfo(IterationData iteration)
    {
        var info = new List<string>();
        
        if (iteration.IsOptimal)
        {
            info.Add("This is the optimal solution.");
        }
        
        if (iteration.NodeType == "Initial")
        {
            info.Add("Starting tableau for the algorithm.");
        }
        
        return string.Join(" ", info);
    }
}

public partial class IterationTreeNode : ObservableObject
{
    [ObservableProperty]
    private string _displayName = "";
    
    [ObservableProperty]
    private string _nodeType = "";
    
    [ObservableProperty]
    private string _icon = "";
    
    [ObservableProperty]
    private IBrush _backgroundBrush = new SolidColorBrush(Colors.Gray);
    
    [ObservableProperty]
    private IBrush _borderBrush = new SolidColorBrush(Colors.DarkGray);
    
    [ObservableProperty]
    private string _statusIndicator = "";
    
    [ObservableProperty]
    private IBrush _statusColor = new SolidColorBrush(Colors.Black);
    
    [ObservableProperty]
    private bool _hasStatus;
    
    [ObservableProperty]
    private FontWeight _fontWeight = FontWeight.Normal;
    
    public IterationData? IterationData { get; set; }
    public ObservableCollection<IterationTreeNode> Children { get; } = new();
}

public partial class IterationDetailsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "";
    
    [ObservableProperty]
    private string _description = "";
    
    [ObservableProperty]
    private string _pivotInfo = "";
    
    [ObservableProperty]
    private string _pivotDetails = "";
    
    [ObservableProperty]
    private string _additionalInfo = "";
    
    [ObservableProperty]
    private bool _hasPivotInfo;
    
    [ObservableProperty]
    private bool _hasPivotDetails;
    
    [ObservableProperty]
    private bool _hasAdditionalInfo;
    
    [ObservableProperty]
    private string _pivotInfoBackground = "#FFF3CD";
    
    [ObservableProperty]
    private string _pivotInfoBorder = "#FFC107";
    
    public ObservableCollection<EnhancedTableauRow> TableauRows { get; set; } = new();
}

public partial class EnhancedTableauRow : TableauRow
{
    [ObservableProperty]
    private int _pivotRow = -1;
    
    [ObservableProperty]
    private int _pivotColumn = -1;
    
    [ObservableProperty]
    private int _rowIndex;
    
    public bool IsPivotCell => PivotRow == RowIndex && PivotColumn >= 0 && PivotColumn < Coefficients.Count;
}