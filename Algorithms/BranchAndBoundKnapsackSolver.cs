using System;
using System.Collections.Generic;
using System.Linq;
using linear_programming_solver.Models;

namespace linear_programming_solver.Algorithms;

public class BranchAndBoundKnapsackSolver
{
    private const double EPSILON = 1e-10;

    public SimplexSolution Solve(CanonicalForm problem)
    {
        var solution = new SimplexSolution
        {
            Algorithm = "Branch & Bound Knapsack",
            Problem = problem
        };

        try
        {
            // Convert to knapsack format
            var knapsack = ConvertToKnapsack(problem);
            if (knapsack == null)
            {
                throw new ArgumentException("Problem is not a valid knapsack problem");
            }

            solution.Iterations.Add(CreateIterationData(0, "Problem converted to knapsack format", knapsack));

            // Solve using branch and bound
            var result = SolveKnapsack(knapsack);
            
            // Convert result back to canonical form variables
            ConvertKnapsackSolution(result, problem, solution);

            // Add final iteration
            solution.Iterations.Add(CreateIterationData(result.Iterations.Count + 1, 
                $"Optimal solution found: Value = {solution.ObjectiveValue:F3}", knapsack));

        }
        catch (Exception ex)
        {
            solution.Status = SolutionStatus.Error;
            solution.ErrorMessage = ex.Message;
        }

        return solution;
    }

    private KnapsackProblem ConvertToKnapsack(CanonicalForm problem)
    {
        // Check if this is a knapsack problem (single constraint, binary variables, maximization)
        if (problem.ConstraintCount != 1 || !problem.IsMaximization)
            return null;

        // Check if all original variables are binary
        bool allBinary = problem.VariableMap.All(v => v.OriginalType == VariableType.Binary);
        if (!allBinary)
            return null;

        var knapsack = new KnapsackProblem
        {
            ItemCount = problem.OriginalVariableCount,
            Capacity = problem.RightHandSide[0],
            Values = new double[problem.OriginalVariableCount],
            Weights = new double[problem.OriginalVariableCount]
        };

        // Extract values and weights
        for (int i = 0; i < problem.OriginalVariableCount; i++)
        {
            var mapping = problem.VariableMap[i];
            int canonicalIndex = mapping.CanonicalIndices[0];
            
            knapsack.Values[i] = problem.ObjectiveCoefficients[canonicalIndex];
            knapsack.Weights[i] = problem.ConstraintMatrix[0, canonicalIndex];
        }

        return knapsack;
    }

    private KnapsackSolution SolveKnapsack(KnapsackProblem knapsack)
    {
        var solution = new KnapsackSolution
        {
            Items = new bool[knapsack.ItemCount],
            Iterations = new List<KnapsackIteration>()
        };

        // Calculate efficiency ratios and sort items
        var items = Enumerable.Range(0, knapsack.ItemCount)
            .Select(i => new { Index = i, Efficiency = knapsack.Values[i] / knapsack.Weights[i] })
            .OrderByDescending(x => x.Efficiency)
            .ToArray();

        // Get upper bound using fractional knapsack
        double upperBound = GetFractionalKnapsackUpperBound(knapsack, items);
        
        solution.Iterations.Add(new KnapsackIteration
        {
            IterationNumber = 0,
            Description = $"Upper bound (fractional): {upperBound:F3}",
            CurrentBound = upperBound
        });

        // Branch and bound
        double bestValue = 0;
        var bestSolution = new bool[knapsack.ItemCount];
        var stack = new Stack<KnapsackNode>();
        
        // Root node
        stack.Push(new KnapsackNode
        {
            Level = 0,
            Value = 0,
            Weight = 0,
            UpperBound = upperBound,
            Items = new bool[knapsack.ItemCount]
        });

        int iteration = 1;

        while (stack.Count > 0 && iteration <= 1000)
        {
            var node = stack.Pop();

            if (node.UpperBound <= bestValue + EPSILON)
            {
                // Prune by bound
                solution.Iterations.Add(new KnapsackIteration
                {
                    IterationNumber = iteration,
                    Description = $"Node pruned (bound: {node.UpperBound:F3} <= {bestValue:F3})",
                    CurrentBound = node.UpperBound
                });
                iteration++;
                continue;
            }

            if (node.Level == knapsack.ItemCount)
            {
                // Leaf node - complete solution
                if (node.Value > bestValue)
                {
                    bestValue = node.Value;
                    Array.Copy(node.Items, bestSolution, knapsack.ItemCount);
                    
                    solution.Iterations.Add(new KnapsackIteration
                    {
                        IterationNumber = iteration,
                        Description = $"New best solution found: {bestValue:F3}",
                        CurrentBound = bestValue,
                        Solution = (bool[])bestSolution.Clone()
                    });
                }
                iteration++;
                continue;
            }

            // Branch on next item
            int currentItem = items[node.Level].Index;
            
            // Try including current item
            if (node.Weight + knapsack.Weights[currentItem] <= knapsack.Capacity)
            {
                var includeNode = new KnapsackNode
                {
                    Level = node.Level + 1,
                    Value = node.Value + knapsack.Values[currentItem],
                    Weight = node.Weight + knapsack.Weights[currentItem],
                    Items = (bool[])node.Items.Clone()
                };
                includeNode.Items[currentItem] = true;
                includeNode.UpperBound = GetNodeUpperBound(knapsack, includeNode, items);
                
                stack.Push(includeNode);
            }

            // Try excluding current item
            var excludeNode = new KnapsackNode
            {
                Level = node.Level + 1,
                Value = node.Value,
                Weight = node.Weight,
                Items = (bool[])node.Items.Clone(),
            };
            excludeNode.UpperBound = GetNodeUpperBound(knapsack, excludeNode, items);
            
            stack.Push(excludeNode);

            double maxBound = excludeNode.UpperBound;
            if (node.Weight + knapsack.Weights[currentItem] <= knapsack.Capacity)
            {
                var includeNodeBound = GetNodeUpperBound(knapsack, new KnapsackNode
                {
                    Level = node.Level + 1,
                    Value = node.Value + knapsack.Values[currentItem],
                    Weight = node.Weight + knapsack.Weights[currentItem],
                    Items = (bool[])node.Items.Clone()
                }, items);
                maxBound = Math.Max(maxBound, includeNodeBound);
            }
            
            solution.Iterations.Add(new KnapsackIteration
            {
                IterationNumber = iteration,
                Description = $"Branched on item {currentItem + 1}",
                CurrentBound = maxBound
            });
            
            iteration++;
        }

        solution.Items = bestSolution;
        solution.OptimalValue = bestValue;
        solution.Status = iteration > 1000 ? "Max iterations reached" : "Optimal";

        return solution;
    }

    private double GetFractionalKnapsackUpperBound(KnapsackProblem knapsack, dynamic[] sortedItems)
    {
        double value = 0;
        double remainingCapacity = knapsack.Capacity;

        foreach (var item in sortedItems)
        {
            if (knapsack.Weights[item.Index] <= remainingCapacity)
            {
                value += knapsack.Values[item.Index];
                remainingCapacity -= knapsack.Weights[item.Index];
            }
            else if (remainingCapacity > 0)
            {
                value += knapsack.Values[item.Index] * (remainingCapacity / knapsack.Weights[item.Index]);
                break;
            }
        }

        return value;
    }

    private double GetNodeUpperBound(KnapsackProblem knapsack, KnapsackNode node, dynamic[] sortedItems)
    {
        double value = node.Value;
        double remainingCapacity = knapsack.Capacity - node.Weight;

        for (int i = node.Level; i < sortedItems.Length; i++)
        {
            int itemIndex = sortedItems[i].Index;
            
            if (knapsack.Weights[itemIndex] <= remainingCapacity)
            {
                value += knapsack.Values[itemIndex];
                remainingCapacity -= knapsack.Weights[itemIndex];
            }
            else if (remainingCapacity > 0)
            {
                value += knapsack.Values[itemIndex] * (remainingCapacity / knapsack.Weights[itemIndex]);
                break;
            }
        }

        return value;
    }

    private void ConvertKnapsackSolution(KnapsackSolution knapsackSolution, CanonicalForm problem, SimplexSolution solution)
    {
        solution.Variables = new double[problem.TotalVariableCount];
        solution.BasicVariables = new List<int>();
        solution.NonBasicVariables = new List<int>();

        // Set binary variable values
        for (int i = 0; i < knapsackSolution.Items.Length; i++)
        {
            var mapping = problem.VariableMap[i];
            int canonicalIndex = mapping.CanonicalIndices[0];
            
            solution.Variables[canonicalIndex] = knapsackSolution.Items[i] ? 1.0 : 0.0;
            
            if (knapsackSolution.Items[i])
                solution.BasicVariables.Add(canonicalIndex);
            else
                solution.NonBasicVariables.Add(canonicalIndex);
        }

        // Set slack variable values
        double totalWeight = 0;
        for (int i = 0; i < knapsackSolution.Items.Length; i++)
        {
            if (knapsackSolution.Items[i])
            {
                var mapping = problem.VariableMap[i];
                int canonicalIndex = mapping.CanonicalIndices[0];
                totalWeight += problem.ConstraintMatrix[0, canonicalIndex];
            }
        }

        // Slack variable
        int slackIndex = problem.TotalVariableCount - 1;
        solution.Variables[slackIndex] = problem.RightHandSide[0] - totalWeight;
        solution.NonBasicVariables.Add(slackIndex);

        solution.ObjectiveValue = knapsackSolution.OptimalValue;
        solution.Status = knapsackSolution.Status == "Optimal" ? SolutionStatus.Optimal : SolutionStatus.MaxIterationsReached;
    }

    private IterationData CreateIterationData(int iteration, string description, KnapsackProblem knapsack)
    {
        return new IterationData
        {
            IterationNumber = iteration,
            AlgorithmType = "Branch & Bound Knapsack",
            Description = description,
            Status = "In Progress",
            Data = new Dictionary<string, object>
            {
                ["ItemCount"] = knapsack.ItemCount,
                ["Capacity"] = knapsack.Capacity,
                ["Values"] = knapsack.Values,
                ["Weights"] = knapsack.Weights
            }
        };
    }
}

public class KnapsackProblem
{
    public int ItemCount { get; set; }
    public double Capacity { get; set; }
    public double[] Values { get; set; } = Array.Empty<double>();
    public double[] Weights { get; set; } = Array.Empty<double>();
}

public class KnapsackSolution
{
    public bool[] Items { get; set; } = Array.Empty<bool>();
    public double OptimalValue { get; set; }
    public string Status { get; set; } = "";
    public List<KnapsackIteration> Iterations { get; set; } = new();
}

public class KnapsackIteration
{
    public int IterationNumber { get; set; }
    public string Description { get; set; } = "";
    public double CurrentBound { get; set; }
    public bool[] Solution { get; set; } = Array.Empty<bool>();
}

public class KnapsackNode
{
    public int Level { get; set; }
    public double Value { get; set; }
    public double Weight { get; set; }
    public double UpperBound { get; set; }
    public bool[] Items { get; set; } = Array.Empty<bool>();
}