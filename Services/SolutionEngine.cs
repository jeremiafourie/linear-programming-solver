using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using linear_programming_solver.Models;
using linear_programming_solver.Algorithms;

namespace linear_programming_solver.Services;

public class SolutionEngine
{
    public async Task<SolutionResult> SolveAsync(string inputContent, AlgorithmType algorithm)
    {
        try
        {
            // Parse input
            var linearProgram = LinearProgramParser.Parse(inputContent);
            
            // Convert to canonical form
            var canonicalForm = CanonicalFormConverter.Convert(linearProgram);
            
            // Solve using selected algorithm
            SimplexSolution solution = algorithm switch
            {
                AlgorithmType.PrimalSimplex => new PrimalSimplexSolver().Solve(canonicalForm),
                AlgorithmType.RevisedPrimalSimplex => new RevisedPrimalSimplexSolver().Solve(canonicalForm),
                AlgorithmType.BranchAndBoundSimplex => new BranchAndBoundSimplexSolver().Solve(canonicalForm),
                AlgorithmType.CuttingPlane => new CuttingPlaneSolver().Solve(canonicalForm),
                AlgorithmType.BranchAndBoundKnapsack => new BranchAndBoundKnapsackSolver().Solve(canonicalForm),
                _ => throw new ArgumentException($"Unsupported algorithm: {algorithm}")
            };
            
            return new SolutionResult
            {
                Success = true,
                OriginalProblem = linearProgram,
                CanonicalForm = canonicalForm,
                Solution = solution,
                Algorithm = algorithm
            };
        }
        catch (Exception ex)
        {
            return new SolutionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
    
    public async Task ExportResultsAsync(SolutionResult result, string outputPath)
    {
        if (!result.Success || result.Solution == null)
        {
            throw new InvalidOperationException("Cannot export invalid or incomplete solution");
        }
        
        var output = GenerateOutputText(result);
        await File.WriteAllTextAsync(outputPath, output);
    }
    
    private string GenerateOutputText(SolutionResult result)
    {
        var output = new StringBuilder();
        
        // Header
        output.AppendLine("LINEAR PROGRAMMING SOLVER RESULTS");
        output.AppendLine($"Algorithm: {result.Solution.Algorithm}");
        output.AppendLine($"Status: {result.Solution.Status}");
        output.AppendLine();
        
        // Original problem
        output.AppendLine("ORIGINAL PROBLEM:");
        output.AppendLine($"Type: {(result.OriginalProblem.IsMaximization ? "Maximization" : "Minimization")}");
        output.AppendLine($"Variables: {result.OriginalProblem.VariableCount}");
        output.AppendLine($"Constraints: {result.OriginalProblem.Constraints.Count}");
        output.AppendLine();
        
        // Canonical form
        output.AppendLine("CANONICAL FORM:");
        AppendCanonicalForm(output, result.CanonicalForm);
        output.AppendLine();
        
        // Solution iterations
        output.AppendLine("SOLUTION ITERATIONS:");
        foreach (var iteration in result.Solution.Iterations)
        {
            AppendIteration(output, iteration);
            output.AppendLine();
        }
        
        // Final solution
        if (result.Solution.Status == SolutionStatus.Optimal)
        {
            output.AppendLine("OPTIMAL SOLUTION:");
            output.AppendLine($"Objective Value: {result.Solution.ObjectiveValue:F3}");
            output.AppendLine("Variable Values:");
            
            for (int i = 0; i < result.Solution.Variables.Length; i++)
            {
                var name = result.CanonicalForm.GetVariableName(i);
                output.AppendLine($"  {name} = {result.Solution.Variables[i]:F3}");
            }
            
            output.AppendLine($"Basic Variables: {string.Join(", ", result.Solution.BasicVariables.Select(i => result.CanonicalForm.GetVariableName(i)))}");
            output.AppendLine($"Non-Basic Variables: {string.Join(", ", result.Solution.NonBasicVariables.Select(i => result.CanonicalForm.GetVariableName(i)))}");
        }
        
        return output.ToString();
    }
    
    private void AppendCanonicalForm(StringBuilder output, CanonicalForm canonical)
    {
        // Objective function
        output.Append($"{(canonical.IsMaximization ? "Maximize" : "Minimize")} Z = ");
        var objTerms = new List<string>();
        for (int j = 0; j < canonical.ObjectiveCoefficients.Length; j++)
        {
            var coeff = canonical.ObjectiveCoefficients[j];
            if (Math.Abs(coeff) > 1e-10)
            {
                var sign = coeff >= 0 ? "+" : "";
                objTerms.Add($"{sign}{coeff:F3}{canonical.GetVariableName(j)}");
            }
        }
        output.AppendLine(string.Join(" ", objTerms));
        
        output.AppendLine("Subject to:");
        
        // Constraints
        for (int i = 0; i < canonical.ConstraintCount; i++)
        {
            var terms = new List<string>();
            for (int j = 0; j < canonical.TotalVariableCount; j++)
            {
                var coeff = canonical.ConstraintMatrix[i, j];
                if (Math.Abs(coeff) > 1e-10)
                {
                    var sign = coeff >= 0 && terms.Count > 0 ? "+" : "";
                    terms.Add($"{sign}{coeff:F3}{canonical.GetVariableName(j)}");
                }
            }
            output.AppendLine($"  {string.Join(" ", terms)} = {canonical.RightHandSide[i]:F3}");
        }
        
        // Non-negativity constraints
        var nonNegVars = new List<string>();
        for (int j = 0; j < canonical.TotalVariableCount; j++)
        {
            nonNegVars.Add(canonical.GetVariableName(j));
        }
        output.AppendLine($"  {string.Join(", ", nonNegVars)} >= 0");
    }
    
    private void AppendIteration(StringBuilder output, IterationData iteration)
    {
        output.AppendLine($"ITERATION {iteration.IterationNumber}: {iteration.Description}");
        
        if (iteration.TableauRows.Count > 0 && iteration.VariableColumns.Count > 0)
        {
            // Header row
            output.Append("Basis".PadRight(8));
            foreach (var col in iteration.VariableColumns)
            {
                output.Append(col.PadLeft(10));
            }
            output.AppendLine();
            
            // Data rows
            foreach (var row in iteration.TableauRows)
            {
                output.Append(row.BasisVariable.PadRight(8));
                
                for (int j = 0; j < row.Coefficients.Count; j++)
                {
                    output.Append($"{row.Coefficients[j]:F3}".PadLeft(10));
                }
                output.Append($"{row.Rhs:F3}".PadLeft(10));
                output.AppendLine();
            }
        }
        
        if (iteration.IsOptimal)
            output.AppendLine("*** OPTIMAL SOLUTION FOUND ***");
    }
}

public class SolutionResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = "";
    public LinearProgram OriginalProblem { get; set; } = new();
    public CanonicalForm CanonicalForm { get; set; } = new();
    public SimplexSolution Solution { get; set; } = new();
    public AlgorithmType Algorithm { get; set; }
}

public enum AlgorithmType
{
    PrimalSimplex,
    RevisedPrimalSimplex,
    BranchAndBoundSimplex,
    CuttingPlane,
    BranchAndBoundKnapsack
}