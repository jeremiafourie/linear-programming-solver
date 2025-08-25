using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AvaloniaGraphControl;
using linear_programming_solver.Models;

namespace linear_programming_solver.ViewModels;

public partial class TableauIterationsViewModel : ViewModelBase
{
    [ObservableProperty]
    private int _currentIteration = 0;

    [ObservableProperty]
    private int _totalIterations = 0;

    [ObservableProperty]
    private TableauData _currentTableauData = new();

    [ObservableProperty]
    private string _pivotInfo = "";

    [ObservableProperty]
    private Graph _tableauGraph = new();

    public ObservableCollection<string> Iterations { get; } = new();
    public ObservableCollection<TableauData> AllTableauData { get; } = new();

    public TableauIterationsViewModel()
    {
        LoadSampleTableauData();
        BuildTableauGraph();
        ShowIteration(0);
    }

    public void LoadIterations(System.Collections.Generic.List<IterationData> iterations)
    {
        StatusMessage = "Loading tableau iterations...";
        
        AllTableauData.Clear();
        Iterations.Clear();
        
        foreach (var iteration in iterations)
        {
            var tableauData = new TableauData
            {
                IterationNumber = iteration.IterationNumber,
                PivotInfo = iteration.Description,
                IsOptimal = iteration.IsOptimal
            };
            
            // Copy variable columns
            tableauData.VariableColumns.Clear();
            foreach (var col in iteration.VariableColumns)
            {
                tableauData.VariableColumns.Add(col);
            }
            
            // Copy tableau rows
            tableauData.Rows.Clear();
            foreach (var row in iteration.TableauRows)
            {
                tableauData.Rows.Add(new TableauRow
                {
                    BasisVariable = row.BasisVariable,
                    Coefficients = new ObservableCollection<double>(row.Coefficients),
                    Rhs = row.Rhs
                });
            }
            
            AllTableauData.Add(tableauData);
            Iterations.Add($"Iteration {iteration.IterationNumber}: {iteration.Description}");
        }
        
        TotalIterations = AllTableauData.Count;
        BuildTableauGraph();
        
        if (TotalIterations > 0)
        {
            ShowIteration(0);
        }
        else
        {
            LoadSampleTableauData();
            BuildTableauGraph();
            ShowIteration(0);
        }
    }

    private void LoadSampleTableauData()
    {
        AllTableauData.Clear();
        Iterations.Clear();
        
        // Iteration 0: Initial Tableau
        var iteration0 = new TableauData
        {
            IterationNumber = 0,
            VariableColumns = new ObservableCollection<string> { "x1", "x2", "x3", "s1", "s2" },
            PivotInfo = "Initial tableau - need to find entering variable",
            IsOptimal = false
        };
        
        iteration0.Rows.Add(new TableauRow 
        { 
            BasisVariable = "s1", 
            Coefficients = new ObservableCollection<double> { 1.000, 2.000, 1.000, 1.000, 0.000 }, 
            Rhs = 8.000 
        });
        iteration0.Rows.Add(new TableauRow 
        { 
            BasisVariable = "s2", 
            Coefficients = new ObservableCollection<double> { 2.000, 1.000, 0.000, 0.000, 1.000 }, 
            Rhs = 10.000 
        });
        iteration0.Rows.Add(new TableauRow 
        { 
            BasisVariable = "Z", 
            Coefficients = new ObservableCollection<double> { -2.000, -3.000, -1.000, 0.000, 0.000 }, 
            Rhs = 0.000 
        });
        
        AllTableauData.Add(iteration0);
        Iterations.Add("Initial Tableau");
        
        // Iteration 1: After first pivot
        var iteration1 = new TableauData
        {
            IterationNumber = 1,
            VariableColumns = new ObservableCollection<string> { "x1", "x2", "x3", "s1", "s2" },
            PivotInfo = "Pivot: x2 enters, s1 leaves at element (1,2) = 2.000",
            IsOptimal = false
        };
        
        iteration1.Rows.Add(new TableauRow 
        { 
            BasisVariable = "x2", 
            Coefficients = new ObservableCollection<double> { 0.500, 1.000, 0.500, 0.500, 0.000 }, 
            Rhs = 4.000 
        });
        iteration1.Rows.Add(new TableauRow 
        { 
            BasisVariable = "s2", 
            Coefficients = new ObservableCollection<double> { 1.500, 0.000, -0.500, -0.500, 1.000 }, 
            Rhs = 6.000 
        });
        iteration1.Rows.Add(new TableauRow 
        { 
            BasisVariable = "Z", 
            Coefficients = new ObservableCollection<double> { -0.500, 0.000, 0.500, 1.500, 0.000 }, 
            Rhs = 12.000 
        });
        
        AllTableauData.Add(iteration1);
        Iterations.Add("Iteration 1: x2 enters, s1 leaves");
        
        // Iteration 2: Optimal Solution
        var iteration2 = new TableauData
        {
            IterationNumber = 2,
            VariableColumns = new ObservableCollection<string> { "x1", "x2", "x3", "s1", "s2" },
            PivotInfo = "Optimal solution reached - all coefficients non-negative",
            IsOptimal = true
        };
        
        iteration2.Rows.Add(new TableauRow 
        { 
            BasisVariable = "x2", 
            Coefficients = new ObservableCollection<double> { 0.000, 1.000, 2.000, 1.000, -1.000 }, 
            Rhs = 2.000 
        });
        iteration2.Rows.Add(new TableauRow 
        { 
            BasisVariable = "x1", 
            Coefficients = new ObservableCollection<double> { 1.000, 0.000, -1.000, -1.000, 2.000 }, 
            Rhs = 4.000 
        });
        iteration2.Rows.Add(new TableauRow 
        { 
            BasisVariable = "Z", 
            Coefficients = new ObservableCollection<double> { 0.000, 0.000, 0.000, 1.000, 1.000 }, 
            Rhs = 14.000 
        });
        
        AllTableauData.Add(iteration2);
        Iterations.Add("Iteration 2: Optimal Solution");
        
        TotalIterations = AllTableauData.Count;
    }

    private void BuildTableauGraph()
    {
        var graph = new Graph();
        
        // AvaloniaGraphControl infers nodes from edges, so we only need to create edges
        
        // Create edges between consecutive iterations
        for (int i = 0; i < AllTableauData.Count - 1; i++)
        {
            var fromData = AllTableauData[i];
            var toData = AllTableauData[i + 1];
            
            graph.Edges.Add(new Edge(fromData, toData));
        }
        
        TableauGraph = graph;
    }

    [RelayCommand]
    private void PreviousIteration()
    {
        if (CurrentIteration > 0)
        {
            CurrentIteration--;
            ShowIteration(CurrentIteration);
        }
    }

    [RelayCommand]
    private void NextIteration()
    {
        if (CurrentIteration < TotalIterations - 1)
        {
            CurrentIteration++;
            ShowIteration(CurrentIteration);
        }
    }

    [RelayCommand]
    private void JumpToFinal()
    {
        if (TotalIterations > 0)
        {
            CurrentIteration = TotalIterations - 1;
            ShowIteration(CurrentIteration);
        }
    }

    [RelayCommand]
    private void JumpToFirst()
    {
        CurrentIteration = 0;
        ShowIteration(CurrentIteration);
    }

    private void ShowIteration(int iteration)
    {
        if (iteration < 0 || iteration >= TotalIterations) return;

        var tableauData = AllTableauData[iteration];
        CurrentTableauData = tableauData;
        PivotInfo = tableauData.PivotInfo;
        StatusMessage = $"Viewing iteration {iteration} of {TotalIterations - 1}";
    }

    public bool CanGoPrevious => CurrentIteration > 0;
    public bool CanGoNext => CurrentIteration < TotalIterations - 1;
}