using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AvaloniaGraphControl;
using linear_programming_solver.Models;

namespace linear_programming_solver.ViewModels;

public partial class IterationDiagramViewModel : ViewModelBase
{
    [ObservableProperty]
    private int _currentIteration = 0;

    [ObservableProperty]
    private int _totalIterations = 0;

    [ObservableProperty]
    private string _algorithmName = "";

    [ObservableProperty]
    private Graph _iterationGraph = new();

    public ObservableCollection<IterationData> AllIterations { get; } = new();
    public ObservableCollection<IterationBranch> IterationBranches { get; } = new();

    public IterationDiagramViewModel()
    {
        LoadSampleData();
    }

    // Generic method to load iterations from any algorithm
    public void LoadIterations(string algorithmName, List<IterationData> iterations)
    {
        AlgorithmName = algorithmName;
        AllIterations.Clear();
        
        foreach (var iteration in iterations)
        {
            AllIterations.Add(iteration);
        }
        
        TotalIterations = AllIterations.Count;
        BuildIterationGraph();
        StatusMessage = $"Loaded {TotalIterations} iterations for {algorithmName}";
    }

    private void LoadSampleData()
    {
        // Sample data for different algorithm types
        LoadBranchAndBoundSample();
        // LoadPrimalSimplexSample();
        // LoadCuttingPlaneSample();
    }

    private void LoadPrimalSimplexSample()
    {
        AllIterations.Clear();
        
        // Iteration 0: Initial Tableau
        var iteration0 = new IterationData
        {
            IterationNumber = 0,
            AlgorithmType = "Primal Simplex",
            Description = "Initial tableau - Z = 0",
            Status = "Continue",
            NodeType = "Initial",
            IsOptimal = false,
            IsFinal = false
        };
        
        // Add tableau data for iteration 0
        iteration0.TableauRows.Add(new TableauRow 
        { 
            BasisVariable = "s1", 
            Coefficients = new ObservableCollection<double> { 1.0, 2.0, 1.0, 1.0, 0.0 }, 
            Rhs = 8.0 
        });
        iteration0.TableauRows.Add(new TableauRow 
        { 
            BasisVariable = "s2", 
            Coefficients = new ObservableCollection<double> { 2.0, 1.0, 0.0, 0.0, 1.0 }, 
            Rhs = 10.0 
        });
        iteration0.TableauRows.Add(new TableauRow 
        { 
            BasisVariable = "Z", 
            Coefficients = new ObservableCollection<double> { -2.0, -3.0, -1.0, 0.0, 0.0 }, 
            Rhs = 0.0 
        });
        
        iteration0.Data["ObjectiveValue"] = 0.0;
        iteration0.Data["BasisVariables"] = new[] { "s1", "s2" };
        AllIterations.Add(iteration0);
        
        // Iteration 1: After First Pivot
        var iteration1 = new IterationData
        {
            IterationNumber = 1,
            AlgorithmType = "Primal Simplex",
            Description = "After pivot - Z = 12",
            Status = "Continue",
            NodeType = "Iteration",
            IsOptimal = false,
            IsFinal = false
        };
        
        // Add tableau data for iteration 1
        iteration1.TableauRows.Add(new TableauRow 
        { 
            BasisVariable = "x2", 
            Coefficients = new ObservableCollection<double> { 0.5, 1.0, 0.5, 0.5, 0.0 }, 
            Rhs = 4.0 
        });
        iteration1.TableauRows.Add(new TableauRow 
        { 
            BasisVariable = "s2", 
            Coefficients = new ObservableCollection<double> { 1.5, 0.0, -0.5, -0.5, 1.0 }, 
            Rhs = 6.0 
        });
        iteration1.TableauRows.Add(new TableauRow 
        { 
            BasisVariable = "Z", 
            Coefficients = new ObservableCollection<double> { -0.5, 0.0, 0.5, 1.5, 0.0 }, 
            Rhs = 12.0 
        });
        
        iteration1.Data["ObjectiveValue"] = 12.0;
        iteration1.Data["BasisVariables"] = new[] { "x2", "s2" };
        AllIterations.Add(iteration1);
        
        // Iteration 2: Optimal Solution
        var iteration2 = new IterationData
        {
            IterationNumber = 2,
            AlgorithmType = "Primal Simplex",
            Description = "Optimal solution - Z = 14",
            Status = "Optimal",
            NodeType = "Optimal",
            IsOptimal = true,
            IsFinal = true
        };
        
        // Add tableau data for iteration 2
        iteration2.TableauRows.Add(new TableauRow 
        { 
            BasisVariable = "x2", 
            Coefficients = new ObservableCollection<double> { 0.0, 1.0, 2.0, 1.0, -1.0 }, 
            Rhs = 2.0 
        });
        iteration2.TableauRows.Add(new TableauRow 
        { 
            BasisVariable = "x1", 
            Coefficients = new ObservableCollection<double> { 1.0, 0.0, -1.0, -1.0, 2.0 }, 
            Rhs = 4.0 
        });
        iteration2.TableauRows.Add(new TableauRow 
        { 
            BasisVariable = "Z", 
            Coefficients = new ObservableCollection<double> { 0.0, 0.0, 0.0, 1.0, 1.0 }, 
            Rhs = 14.0 
        });
        
        iteration2.Data["ObjectiveValue"] = 14.0;
        iteration2.Data["BasisVariables"] = new[] { "x1", "x2" };
        AllIterations.Add(iteration2);
        
        AlgorithmName = "Primal Simplex Algorithm";
        TotalIterations = AllIterations.Count;
        BuildIterationGraph();
    }

    // Branch & Bound with comprehensive tree structure
    private void LoadBranchAndBoundSample()
    {
        AllIterations.Clear();
        
        // Root node: LP Relaxation
        var root = new IterationData
        {
            IterationNumber = 0,
            AlgorithmType = "Branch & Bound",
            Description = "Root LP Relaxation - Z = 15.333",
            Status = "Branch Required",
            NodeType = "Root",
            IsOptimal = false,
            IsFinal = false
        };
        
        // Root tableau data
        root.TableauRows.Add(new TableauRow 
        { 
            BasisVariable = "x2", 
            Coefficients = new ObservableCollection<double> { 0.0, 1.0, 1.333, 0.667, 0.0 }, 
            Rhs = 4.667 
        });
        root.TableauRows.Add(new TableauRow 
        { 
            BasisVariable = "x1", 
            Coefficients = new ObservableCollection<double> { 1.0, 0.0, -0.667, -0.333, 0.0 }, 
            Rhs = 2.333 
        });
        root.TableauRows.Add(new TableauRow 
        { 
            BasisVariable = "Z", 
            Coefficients = new ObservableCollection<double> { 0.0, 0.0, 0.333, 0.667, 0.0 }, 
            Rhs = 15.333 
        });
        AllIterations.Add(root);
        
        // Branch 1: x1 ≤ 2
        var branch1 = new IterationData
        {
            IterationNumber = 1,
            AlgorithmType = "Branch & Bound",
            Description = "Branch: x1 ≤ 2 - Z = 15.0",
            Status = "Branch Required",
            NodeType = "Branch",
            IsOptimal = false,
            IsFinal = false
        };
        
        branch1.TableauRows.Add(new TableauRow 
        { 
            BasisVariable = "x2", 
            Coefficients = new ObservableCollection<double> { 0.0, 1.0, 1.0, 1.0, 0.0 }, 
            Rhs = 5.0 
        });
        branch1.TableauRows.Add(new TableauRow 
        { 
            BasisVariable = "x1", 
            Coefficients = new ObservableCollection<double> { 1.0, 0.0, 0.0, 0.0, 1.0 }, 
            Rhs = 2.0 
        });
        branch1.TableauRows.Add(new TableauRow 
        { 
            BasisVariable = "Z", 
            Coefficients = new ObservableCollection<double> { 0.0, 0.0, 1.0, 1.0, 0.0 }, 
            Rhs = 15.0 
        });
        AllIterations.Add(branch1);
        
        // Branch 2: x1 ≥ 3  
        var branch2 = new IterationData
        {
            IterationNumber = 2,
            AlgorithmType = "Branch & Bound",
            Description = "Branch: x1 ≥ 3 - Infeasible",
            Status = "Infeasible",
            NodeType = "Branch",
            IsOptimal = false,
            IsFinal = true
        };
        
        branch2.TableauRows.Add(new TableauRow 
        { 
            BasisVariable = "—", 
            Coefficients = new ObservableCollection<double> { 0.0, 0.0, 0.0, 0.0, 0.0 }, 
            Rhs = 0.0 
        });
        branch2.TableauRows.Add(new TableauRow 
        { 
            BasisVariable = "—", 
            Coefficients = new ObservableCollection<double> { 0.0, 0.0, 0.0, 0.0, 0.0 }, 
            Rhs = 0.0 
        });
        branch2.TableauRows.Add(new TableauRow 
        { 
            BasisVariable = "—", 
            Coefficients = new ObservableCollection<double> { 0.0, 0.0, 0.0, 0.0, 0.0 }, 
            Rhs = 0.0 
        });
        AllIterations.Add(branch2);
        
        // Branch 1.1: x1 ≤ 2, x2 ≤ 4
        var branch11 = new IterationData
        {
            IterationNumber = 3,
            AlgorithmType = "Branch & Bound",
            Description = "Branch: x1≤2, x2≤4 - Z = 14.0",
            Status = "Integer Solution",
            NodeType = "Branch",
            IsOptimal = true,
            IsFinal = false
        };
        
        branch11.TableauRows.Add(new TableauRow 
        { 
            BasisVariable = "x2", 
            Coefficients = new ObservableCollection<double> { 0.0, 1.0, 0.0, 1.0, 0.0 }, 
            Rhs = 4.0 
        });
        branch11.TableauRows.Add(new TableauRow 
        { 
            BasisVariable = "x1", 
            Coefficients = new ObservableCollection<double> { 1.0, 0.0, 0.0, 0.0, 1.0 }, 
            Rhs = 2.0 
        });
        branch11.TableauRows.Add(new TableauRow 
        { 
            BasisVariable = "Z", 
            Coefficients = new ObservableCollection<double> { 0.0, 0.0, 1.0, 1.0, 0.0 }, 
            Rhs = 14.0 
        });
        AllIterations.Add(branch11);
        
        // Branch 1.2: x1 ≤ 2, x2 ≥ 5
        var branch12 = new IterationData
        {
            IterationNumber = 4,
            AlgorithmType = "Branch & Bound",
            Description = "Branch: x1≤2, x2≥5 - Z = 13.0",
            Status = "Integer Solution",
            NodeType = "Branch",
            IsOptimal = false,
            IsFinal = true
        };
        
        branch12.TableauRows.Add(new TableauRow 
        { 
            BasisVariable = "x2", 
            Coefficients = new ObservableCollection<double> { 0.0, 1.0, 0.0, 0.0, 0.0 }, 
            Rhs = 5.0 
        });
        branch12.TableauRows.Add(new TableauRow 
        { 
            BasisVariable = "x1", 
            Coefficients = new ObservableCollection<double> { 1.0, 0.0, 1.0, 0.0, 0.0 }, 
            Rhs = 1.0 
        });
        branch12.TableauRows.Add(new TableauRow 
        { 
            BasisVariable = "Z", 
            Coefficients = new ObservableCollection<double> { 0.0, 0.0, 2.0, 1.0, 0.0 }, 
            Rhs = 13.0 
        });
        AllIterations.Add(branch12);
        
        AlgorithmName = "Branch & Bound Algorithm - Integer Programming";
        TotalIterations = AllIterations.Count;
        BuildIterationGraph();
    }

    private void BuildIterationGraph()
    {
        var graph = new Graph();
        IterationBranches.Clear();
        
        // Create Branch & Bound tree structure
        if (AllIterations.Count > 0 && AllIterations[0].AlgorithmType == "Branch & Bound")
        {
            // Root to first level branches (0 -> 1, 0 -> 2)
            if (AllIterations.Count > 1)
            {
                graph.Edges.Add(new Edge(AllIterations[0], AllIterations[1])); // Root -> x1 <= 2
                if (AllIterations.Count > 2)
                    graph.Edges.Add(new Edge(AllIterations[0], AllIterations[2])); // Root -> x1 >= 3
            }
            
            // Second level branches from node 1 (1 -> 3, 1 -> 4)
            if (AllIterations.Count > 3)
            {
                graph.Edges.Add(new Edge(AllIterations[1], AllIterations[3])); // x1<=2 -> x1<=2,x2<=4
                if (AllIterations.Count > 4)
                    graph.Edges.Add(new Edge(AllIterations[1], AllIterations[4])); // x1<=2 -> x1<=2,x2>=5
            }
            
            // Create branch descriptions
            if (AllIterations.Count > 1)
            {
                IterationBranches.Add(new IterationBranch
                {
                    FromIteration = 0, ToIteration = 1,
                    BranchCondition = "x1 ≤ 2", EdgeId = "0_1"
                });
            }
            if (AllIterations.Count > 2)
            {
                IterationBranches.Add(new IterationBranch
                {
                    FromIteration = 0, ToIteration = 2,
                    BranchCondition = "x1 ≥ 3", EdgeId = "0_2"
                });
            }
            if (AllIterations.Count > 3)
            {
                IterationBranches.Add(new IterationBranch
                {
                    FromIteration = 1, ToIteration = 3,
                    BranchCondition = "x2 ≤ 4", EdgeId = "1_3"
                });
            }
            if (AllIterations.Count > 4)
            {
                IterationBranches.Add(new IterationBranch
                {
                    FromIteration = 1, ToIteration = 4,
                    BranchCondition = "x2 ≥ 5", EdgeId = "1_4"
                });
            }
        }
        else
        {
            // Linear flow for other algorithm types (Primal Simplex, etc.)
            for (int i = 0; i < AllIterations.Count - 1; i++)
            {
                var fromIteration = AllIterations[i];
                var toIteration = AllIterations[i + 1];
                
                graph.Edges.Add(new Edge(fromIteration, toIteration));
                
                IterationBranches.Add(new IterationBranch
                {
                    FromIteration = i,
                    ToIteration = i + 1,
                    BranchCondition = GetBranchCondition(fromIteration, toIteration),
                    EdgeId = $"{fromIteration.IterationNumber}_{toIteration.IterationNumber}"
                });
            }
        }
        
        IterationGraph = graph;
    }

    private string GetBranchCondition(IterationData from, IterationData to)
    {
        return from.AlgorithmType switch
        {
            "Primal Simplex" => "Pivot Operation",
            "Branch & Bound" when to.Description.Contains("≤") => to.Description.Split('\n')[0],
            "Branch & Bound" when to.Description.Contains("≥") => to.Description.Split('\n')[0],
            "Cutting Plane" => "Cut Added",
            _ => "Next Iteration"
        };
    }

    [RelayCommand]
    private void PreviousIteration()
    {
        if (CurrentIteration > 0)
        {
            CurrentIteration--;
            StatusMessage = $"Viewing iteration {CurrentIteration}";
        }
    }

    [RelayCommand]
    private void NextIteration()
    {
        if (CurrentIteration < TotalIterations - 1)
        {
            CurrentIteration++;
            StatusMessage = $"Viewing iteration {CurrentIteration}";
        }
    }

    [RelayCommand]
    private void JumpToFinal()
    {
        if (TotalIterations > 0)
        {
            CurrentIteration = TotalIterations - 1;
            StatusMessage = $"Viewing final iteration";
        }
    }

    [RelayCommand]
    private void JumpToFirst()
    {
        CurrentIteration = 0;
        StatusMessage = $"Viewing initial iteration";
    }

    public bool CanGoPrevious => CurrentIteration > 0;
    public bool CanGoNext => CurrentIteration < TotalIterations - 1;
}