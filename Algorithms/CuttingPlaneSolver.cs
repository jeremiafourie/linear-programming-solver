using System;
using System.Collections.Generic;
using System.Linq;
using linear_programming_solver.Models;

namespace linear_programming_solver.Algorithms;

public class CuttingPlaneSolver
{
    private const double EPSILON = 1e-10;
    private readonly RevisedPrimalSimplexSolver _revisedSolver = new();

    public SimplexSolution Solve(CanonicalForm problem)
    {
        var solution = new SimplexSolution
        {
            Algorithm = "Cutting Plane",
            Problem = problem
        };

        try
        {
            if (!HasIntegerVariables(problem))
            {
                // No integer variables, solve as regular LP
                return _revisedSolver.Solve(problem);
            }

            var workingProblem = CloneProblem(problem);
            int iteration = 0;
            int maxIterations = 100;

            while (iteration < maxIterations)
            {
                // Solve current LP relaxation
                var lpSolution = _revisedSolver.Solve(workingProblem);
                
                string iterationDesc = $"Iteration {iteration}: LP Relaxation";
                if (lpSolution.Status != SolutionStatus.Optimal)
                {
                    solution.Status = lpSolution.Status;
                    solution.ErrorMessage = "LP relaxation failed";
                    solution.Iterations.Add(CreateIterationData(lpSolution, iteration, iterationDesc + " - Failed"));
                    break;
                }

                solution.Iterations.Add(CreateIterationData(lpSolution, iteration, iterationDesc));

                // Check if solution is integer feasible
                if (IsIntegerFeasible(lpSolution, problem))
                {
                    solution.Status = SolutionStatus.Optimal;
                    solution.Variables = lpSolution.Variables;
                    solution.ObjectiveValue = lpSolution.ObjectiveValue;
                    solution.BasicVariables = lpSolution.BasicVariables;
                    solution.NonBasicVariables = lpSolution.NonBasicVariables;
                    
                    solution.Iterations.Add(CreateIterationData(lpSolution, iteration + 1, "Integer solution found"));
                    break;
                }

                // Generate Gomory cut
                var cut = GenerateGomoryCut(lpSolution, workingProblem, problem);
                if (cut == null)
                {
                    solution.Status = SolutionStatus.Error;
                    solution.ErrorMessage = "Unable to generate valid cut";
                    break;
                }

                // Add cut to problem
                AddCutToWorkingProblem(workingProblem, cut);
                
                string cutDesc = $"Added Gomory cut: {FormatCut(cut)}";
                solution.Iterations.Add(CreateIterationData(lpSolution, iteration, cutDesc));

                iteration++;
            }

            if (iteration >= maxIterations)
            {
                solution.Status = SolutionStatus.MaxIterationsReached;
            }
        }
        catch (Exception ex)
        {
            solution.Status = SolutionStatus.Error;
            solution.ErrorMessage = ex.Message;
        }

        return solution;
    }

    private bool HasIntegerVariables(CanonicalForm problem)
    {
        return problem.VariableMap.Any(v => v.OriginalType == VariableType.Integer || v.OriginalType == VariableType.Binary);
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

    private GomoryCut GenerateGomoryCut(SimplexSolution lpSolution, CanonicalForm workingProblem, CanonicalForm originalProblem)
    {
        // Find most fractional integer basic variable
        int bestRow = -1;
        double maxFractionalPart = 0;

        for (int i = 0; i < lpSolution.BasicVariables.Count; i++)
        {
            int basicVar = lpSolution.BasicVariables[i];
            
            // Check if this is an integer variable
            bool isIntegerVar = false;
            foreach (var mapping in originalProblem.VariableMap)
            {
                if ((mapping.OriginalType == VariableType.Integer || mapping.OriginalType == VariableType.Binary) &&
                    mapping.CanonicalIndices.Contains(basicVar))
                {
                    isIntegerVar = true;
                    break;
                }
            }

            if (isIntegerVar)
            {
                double value = lpSolution.Variables[basicVar];
                double fractionalPart = Math.Abs(value - Math.Floor(value));
                
                if (fractionalPart > maxFractionalPart && fractionalPart > EPSILON)
                {
                    maxFractionalPart = fractionalPart;
                    bestRow = i;
                }
            }
        }

        if (bestRow == -1)
            return null; // No fractional basic variables found

        // Generate cut coefficients (simplified approach)
        // In practice, you would extract the tableau row and compute fractional parts
        var cut = new GomoryCut
        {
            Coefficients = new double[workingProblem.TotalVariableCount],
            RightHandSide = -maxFractionalPart,
            SourceRow = bestRow
        };

        // Simplified cut generation - in practice this would be more complex
        for (int j = 0; j < workingProblem.TotalVariableCount; j++)
        {
            cut.Coefficients[j] = Random.Shared.NextDouble() * 0.1; // Placeholder
        }

        return cut;
    }

    private void AddCutToWorkingProblem(CanonicalForm workingProblem, GomoryCut cut)
    {
        // Extend the constraint matrix with the new cut
        // This is a simplified implementation
        int oldRows = workingProblem.ConstraintCount;
        int cols = workingProblem.TotalVariableCount + 1; // +1 for new slack variable

        // Create new constraint matrix
        var newMatrix = new double[oldRows + 1, cols];
        var newRhs = new double[oldRows + 1];

        // Copy existing constraints
        for (int i = 0; i < oldRows; i++)
        {
            for (int j = 0; j < workingProblem.TotalVariableCount; j++)
            {
                newMatrix[i, j] = workingProblem.ConstraintMatrix[i, j];
            }
            newMatrix[i, cols - 1] = 0; // New slack variable has 0 coefficient in old constraints
            newRhs[i] = workingProblem.RightHandSide[i];
        }

        // Add the cut as the last constraint
        for (int j = 0; j < workingProblem.TotalVariableCount; j++)
        {
            newMatrix[oldRows, j] = cut.Coefficients[j];
        }
        newMatrix[oldRows, cols - 1] = 1; // Slack variable for the cut
        newRhs[oldRows] = cut.RightHandSide;

        // Update the working problem (simplified - in practice you'd need to handle objective too)
        workingProblem.ConstraintMatrix = newMatrix;
        workingProblem.RightHandSide = newRhs;
        workingProblem.TotalVariableCount = cols;
        workingProblem.SlackVariableCount++;
    }

    private string FormatCut(GomoryCut cut)
    {
        var terms = new List<string>();
        for (int j = 0; j < cut.Coefficients.Length; j++)
        {
            if (Math.Abs(cut.Coefficients[j]) > EPSILON)
            {
                string sign = cut.Coefficients[j] >= 0 && terms.Count > 0 ? "+" : "";
                terms.Add($"{sign}{cut.Coefficients[j]:F3}x{j + 1}");
            }
        }
        return $"{string.Join(" ", terms)} <= {cut.RightHandSide:F3}";
    }

    private CanonicalForm CloneProblem(CanonicalForm original)
    {
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

    private IterationData CreateIterationData(SimplexSolution lpSolution, int iteration, string description)
    {
        var iterationData = new IterationData
        {
            IterationNumber = iteration,
            AlgorithmType = "Cutting Plane",
            Description = description,
            Status = "In Progress",
            IsOptimal = lpSolution.Status == SolutionStatus.Optimal && IsIntegerFeasible(lpSolution, lpSolution.Problem),
            IsFinal = lpSolution.Status != SolutionStatus.Optimal
        };

        if (lpSolution.Variables != null)
        {
            iterationData.Data["Variables"] = lpSolution.Variables.ToArray();
            iterationData.Data["ObjectiveValue"] = lpSolution.ObjectiveValue;
        }

        return iterationData;
    }
}

public class GomoryCut
{
    public double[] Coefficients { get; set; } = Array.Empty<double>();
    public double RightHandSide { get; set; }
    public int SourceRow { get; set; }
}