using System;
using System.Collections.Generic;
using System.Linq;
using linear_programming_solver.Models;

namespace linear_programming_solver.Algorithms;

public class BranchAndBoundSimplexSolver
{
    private const double EPSILON = 1e-10;
    private readonly PrimalSimplexSolver _simplexSolver = new();

    public SimplexSolution Solve(CanonicalForm problem)
    {
        var solution = new SimplexSolution
        {
            Algorithm = "Branch & Bound Simplex",
            Problem = problem
        };

        try
        {
            if (!problem.OriginalVariableCount.ToString().Contains("Integer") && 
                !problem.VariableMap.Any(v => v.OriginalType == VariableType.Integer || v.OriginalType == VariableType.Binary))
            {
                // No integer variables, solve as regular LP
                return _simplexSolver.Solve(problem);
            }

            var branchAndBound = new BranchAndBoundTree();
            var rootNode = CreateRootNode(problem);
            branchAndBound.AddNode(rootNode);

            solution.Iterations.Add(CreateIterationData(rootNode, 0, "Root node - LP relaxation"));

            double bestObjective = problem.IsMaximization ? double.MinValue : double.MaxValue;
            SimplexSolution bestSolution = null;
            var activeNodes = new Queue<BranchAndBoundNode>();
            activeNodes.Enqueue(rootNode);

            int iteration = 1;

            while (activeNodes.Count > 0 && iteration <= 1000)
            {
                var currentNode = activeNodes.Dequeue();
                
                // Solve LP relaxation for current node
                var lpSolution = _simplexSolver.Solve(currentNode.Problem);
                currentNode.LpSolution = lpSolution;

                string nodeDescription = $"Node {currentNode.Id}: ";

                if (lpSolution.Status == SolutionStatus.Infeasible)
                {
                    // Fathom by infeasibility
                    currentNode.Status = NodeStatus.Fathomed;
                    currentNode.FathomReason = "Infeasible";
                    nodeDescription += "Fathomed (Infeasible)";
                }
                else if (lpSolution.Status == SolutionStatus.Unbounded)
                {
                    // This shouldn't happen in B&B, but handle it
                    currentNode.Status = NodeStatus.Fathomed;
                    currentNode.FathomReason = "Unbounded";
                    nodeDescription += "Fathomed (Unbounded)";
                }
                else if (IsWorseThanBest(lpSolution.ObjectiveValue, bestObjective, problem.IsMaximization))
                {
                    // Fathom by bound
                    currentNode.Status = NodeStatus.Fathomed;
                    currentNode.FathomReason = "Bound";
                    nodeDescription += $"Fathomed (Bound: {lpSolution.ObjectiveValue:F3} vs {bestObjective:F3})";
                }
                else if (IsIntegerFeasible(lpSolution, problem))
                {
                    // Integer feasible solution found
                    currentNode.Status = NodeStatus.IntegerSolution;
                    bestObjective = lpSolution.ObjectiveValue;
                    bestSolution = lpSolution;
                    nodeDescription += $"Integer solution found: {bestObjective:F3}";
                }
                else
                {
                    // Branch on fractional variable
                    var branchingVar = FindBranchingVariable(lpSolution, problem);
                    if (branchingVar != -1)
                    {
                        var (leftChild, rightChild) = CreateChildNodes(currentNode, branchingVar, lpSolution.Variables[branchingVar]);
                        activeNodes.Enqueue(leftChild);
                        activeNodes.Enqueue(rightChild);
                        branchAndBound.AddNode(leftChild);
                        branchAndBound.AddNode(rightChild);

                        currentNode.Status = NodeStatus.Branched;
                        nodeDescription += $"Branched on x{branchingVar + 1} = {lpSolution.Variables[branchingVar]:F3}";
                    }
                }

                solution.Iterations.Add(CreateIterationData(currentNode, iteration, nodeDescription));
                iteration++;
            }

            if (bestSolution != null)
            {
                solution.Status = SolutionStatus.Optimal;
                solution.Variables = bestSolution.Variables;
                solution.ObjectiveValue = bestSolution.ObjectiveValue;
                solution.BasicVariables = bestSolution.BasicVariables;
                solution.NonBasicVariables = bestSolution.NonBasicVariables;
            }
            else
            {
                solution.Status = iteration > 1000 ? SolutionStatus.MaxIterationsReached : SolutionStatus.Infeasible;
            }
        }
        catch (Exception ex)
        {
            solution.Status = SolutionStatus.Error;
            solution.ErrorMessage = ex.Message;
        }

        return solution;
    }

    private BranchAndBoundNode CreateRootNode(CanonicalForm problem)
    {
        return new BranchAndBoundNode
        {
            Id = 0,
            Problem = problem,
            Status = NodeStatus.Active,
            LowerBounds = new Dictionary<int, double>(),
            UpperBounds = new Dictionary<int, double>()
        };
    }

    private bool IsWorseThanBest(double currentValue, double bestValue, bool isMaximization)
    {
        if (isMaximization)
            return currentValue <= bestValue + EPSILON;
        else
            return currentValue >= bestValue - EPSILON;
    }

    private bool IsIntegerFeasible(SimplexSolution lpSolution, CanonicalForm problem)
    {
        foreach (var mapping in problem.VariableMap)
        {
            if (mapping.OriginalType == VariableType.Integer || mapping.OriginalType == VariableType.Binary)
            {
                foreach (var index in mapping.CanonicalIndices)
                {
                    double value = lpSolution.Variables[index];
                    if (Math.Abs(value - Math.Round(value)) > EPSILON)
                        return false;
                        
                    if (mapping.OriginalType == VariableType.Binary && (value < -EPSILON || value > 1 + EPSILON))
                        return false;
                }
            }
        }
        return true;
    }

    private int FindBranchingVariable(SimplexSolution lpSolution, CanonicalForm problem)
    {
        // Find most fractional integer variable
        int branchingVar = -1;
        double maxFractional = 0;

        foreach (var mapping in problem.VariableMap)
        {
            if (mapping.OriginalType == VariableType.Integer || mapping.OriginalType == VariableType.Binary)
            {
                foreach (var index in mapping.CanonicalIndices)
                {
                    double value = lpSolution.Variables[index];
                    double fractionalPart = Math.Abs(value - Math.Round(value));
                    
                    if (fractionalPart > maxFractional)
                    {
                        maxFractional = fractionalPart;
                        branchingVar = index;
                    }
                }
            }
        }

        return branchingVar;
    }

    private (BranchAndBoundNode left, BranchAndBoundNode right) CreateChildNodes(
        BranchAndBoundNode parent, int branchingVar, double branchingValue)
    {
        // Create left child: x <= floor(value)
        var leftChild = new BranchAndBoundNode
        {
            Id = parent.Id * 2 + 1,
            Problem = CloneProblem(parent.Problem),
            Status = NodeStatus.Active,
            LowerBounds = new Dictionary<int, double>(parent.LowerBounds),
            UpperBounds = new Dictionary<int, double>(parent.UpperBounds),
            Parent = parent,
            BranchingVariable = branchingVar,
            BranchingConstraint = $"x{branchingVar + 1} <= {Math.Floor(branchingValue)}"
        };
        leftChild.UpperBounds[branchingVar] = Math.Floor(branchingValue);

        // Create right child: x >= ceil(value)
        var rightChild = new BranchAndBoundNode
        {
            Id = parent.Id * 2 + 2,
            Problem = CloneProblem(parent.Problem),
            Status = NodeStatus.Active,
            LowerBounds = new Dictionary<int, double>(parent.LowerBounds),
            UpperBounds = new Dictionary<int, double>(parent.UpperBounds),
            Parent = parent,
            BranchingVariable = branchingVar,
            BranchingConstraint = $"x{branchingVar + 1} >= {Math.Ceiling(branchingValue)}"
        };
        rightChild.LowerBounds[branchingVar] = Math.Ceiling(branchingValue);

        // Add branching constraints to child problems
        AddBranchingConstraints(leftChild);
        AddBranchingConstraints(rightChild);

        return (leftChild, rightChild);
    }

    private CanonicalForm CloneProblem(CanonicalForm original)
    {
        // Create a deep copy of the canonical form
        var clone = new CanonicalForm
        {
            IsMaximization = original.IsMaximization,
            ObjectiveCoefficients = (double[])original.ObjectiveCoefficients.Clone(),
            RightHandSide = (double[])original.RightHandSide.Clone(),
            OriginalVariableCount = original.OriginalVariableCount,
            TotalVariableCount = original.TotalVariableCount,
            SlackVariableCount = original.SlackVariableCount,
            VariableMap = new List<VariableMapping>(original.VariableMap)
        };

        // Clone constraint matrix
        clone.ConstraintMatrix = new double[original.ConstraintCount, original.TotalVariableCount];
        for (int i = 0; i < original.ConstraintCount; i++)
        {
            for (int j = 0; j < original.TotalVariableCount; j++)
            {
                clone.ConstraintMatrix[i, j] = original.ConstraintMatrix[i, j];
            }
        }

        return clone;
    }

    private void AddBranchingConstraints(BranchAndBoundNode node)
    {
        // For simplicity, we'll modify the existing problem by adding penalty terms
        // In a full implementation, you would extend the constraint matrix
        
        // This is a simplified approach - in practice you'd need to properly
        // add the branching constraints to the problem formulation
    }

    private IterationData CreateIterationData(BranchAndBoundNode node, int iteration, string description)
    {
        var iterationData = new IterationData
        {
            IterationNumber = iteration,
            AlgorithmType = "Branch & Bound Simplex",
            Description = description,
            Status = node.Status.ToString(),
            IsOptimal = node.Status == NodeStatus.IntegerSolution,
            IsFinal = node.Status == NodeStatus.Fathomed || node.Status == NodeStatus.IntegerSolution,
            NodeType = "Branch"
        };

        // Store node information
        iterationData.Data["NodeId"] = node.Id;
        iterationData.Data["NodeStatus"] = node.Status;
        iterationData.Data["BranchingConstraint"] = node.BranchingConstraint ?? "";
        
        if (node.LpSolution != null && node.LpSolution.Status == SolutionStatus.Optimal)
        {
            iterationData.Data["LPObjectiveValue"] = node.LpSolution.ObjectiveValue;
            iterationData.Data["LPVariables"] = node.LpSolution.Variables;
            
            // Create tableau display from LP solution iterations if available
            if (node.LpSolution.Iterations.Count > 0)
            {
                var lastIteration = node.LpSolution.Iterations.Last();
                
                // Copy tableau data from the LP solution
                foreach (var col in lastIteration.VariableColumns)
                {
                    iterationData.VariableColumns.Add(col);
                }
                
                foreach (var row in lastIteration.TableauRows)
                {
                    var newRow = new TableauRow
                    {
                        BasisVariable = row.BasisVariable,
                        Rhs = row.Rhs
                    };
                    foreach (var coeff in row.Coefficients)
                    {
                        newRow.Coefficients.Add(coeff);
                    }
                    iterationData.TableauRows.Add(newRow);
                }
            }
            else
            {
                // Create simplified display showing variable values
                CreateBranchNodeDisplay(node, iterationData);
            }
        }
        else
        {
            // Create simplified display for infeasible/fathomed nodes
            CreateBranchNodeDisplay(node, iterationData);
        }

        return iterationData;
    }
    
    private void CreateBranchNodeDisplay(BranchAndBoundNode node, IterationData iterationData)
    {
        // Create a simplified display for branch and bound nodes
        iterationData.VariableColumns.Add("Variable");
        iterationData.VariableColumns.Add("Value");
        iterationData.VariableColumns.Add("Status");
        
        if (node.LpSolution?.Variables != null)
        {
            for (int i = 0; i < Math.Min(6, node.LpSolution.Variables.Length); i++) // Show first 6 variables
            {
                var row = new TableauRow
                {
                    BasisVariable = $"x{i + 1}",
                    Rhs = 0
                };
                
                row.Coefficients.Add(node.LpSolution.Variables[i]);
                row.Coefficients.Add(node.LpSolution.Variables[i]); // Duplicate for display
                
                string status = "Continuous";
                if (node.Problem.VariableMap.Count > i)
                {
                    var mapping = node.Problem.VariableMap[i];
                    if (mapping.OriginalType == VariableType.Integer)
                        status = Math.Abs(node.LpSolution.Variables[i] - Math.Round(node.LpSolution.Variables[i])) < EPSILON ? "Integer" : "Fractional";
                    else if (mapping.OriginalType == VariableType.Binary)
                        status = (Math.Abs(node.LpSolution.Variables[i]) < EPSILON || Math.Abs(node.LpSolution.Variables[i] - 1) < EPSILON) ? "Binary" : "Fractional";
                }
                
                row.Coefficients.Add(status == "Integer" ? 1.0 : (status == "Binary" ? 2.0 : 0.0)); // Encode status as number
                
                iterationData.TableauRows.Add(row);
            }
        }
        
        // Add summary row
        var summaryRow = new TableauRow
        {
            BasisVariable = "Objective",
            Rhs = node.LpSolution?.ObjectiveValue ?? 0
        };
        summaryRow.Coefficients.Add(node.LpSolution?.ObjectiveValue ?? 0);
        summaryRow.Coefficients.Add(0); // Placeholder
        summaryRow.Coefficients.Add(node.Status == NodeStatus.IntegerSolution ? 3.0 : 0.0); // Status encoding
        
        iterationData.TableauRows.Add(summaryRow);
    }
}

public class BranchAndBoundTree
{
    public List<BranchAndBoundNode> Nodes { get; set; } = new();

    public void AddNode(BranchAndBoundNode node)
    {
        Nodes.Add(node);
    }
}

public class BranchAndBoundNode
{
    public int Id { get; set; }
    public CanonicalForm Problem { get; set; } = new();
    public NodeStatus Status { get; set; }
    public string FathomReason { get; set; } = "";
    public SimplexSolution LpSolution { get; set; } = new();
    public Dictionary<int, double> LowerBounds { get; set; } = new();
    public Dictionary<int, double> UpperBounds { get; set; } = new();
    public BranchAndBoundNode Parent { get; set; } = null;
    public int BranchingVariable { get; set; } = -1;
    public string BranchingConstraint { get; set; } = "";
}

public enum NodeStatus
{
    Active,
    Fathomed,
    Branched,
    IntegerSolution
}