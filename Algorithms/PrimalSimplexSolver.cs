using System;
using System.Collections.Generic;
using System.Linq;
using linear_programming_solver.Models;

namespace linear_programming_solver.Algorithms;

public class PrimalSimplexSolver
{
    private const double EPSILON = 1e-10;

    public SimplexSolution Solve(CanonicalForm problem)
    {
        var solution = new SimplexSolution
        {
            Algorithm = "Primal Simplex",
            Problem = problem
        };

        try
        {
            var tableau = InitializeTableau(problem);
            solution.Iterations.Add(CreateIterationData(tableau, 0, "Initial tableau"));

            int iteration = 1;
            while (!IsOptimal(tableau) && iteration <= 1000)
            {
                // Find entering variable (most negative in objective row for minimization)
                int enteringVar = FindEnteringVariable(tableau);
                if (enteringVar == -1)
                {
                    solution.Status = SolutionStatus.Optimal;
                    break;
                }

                // Find leaving variable (minimum ratio test)
                int leavingVar = FindLeavingVariable(tableau, enteringVar);
                if (leavingVar == -1)
                {
                    solution.Status = SolutionStatus.Unbounded;
                    solution.Iterations.Add(CreateIterationData(tableau, iteration, "Problem is unbounded"));
                    break;
                }

                // Store leaving variable before pivot
                int leavingVarIndex = tableau.BasicVariables[leavingVar];
                
                // Pivot operation
                Pivot(tableau, leavingVar, enteringVar);
                
                // Update basic variables
                tableau.BasicVariables[leavingVar] = enteringVar;

                string pivotInfo = $"Pivot: Enter x{enteringVar + 1}, Leave x{leavingVarIndex + 1}";
                solution.Iterations.Add(CreateIterationData(tableau, iteration, pivotInfo));

                iteration++;
            }

            if (iteration > 1000)
            {
                solution.Status = SolutionStatus.MaxIterationsReached;
            }
            else if (solution.Status != SolutionStatus.Unbounded)
            {
                solution.Status = SolutionStatus.Optimal;
                ExtractSolution(tableau, problem, solution);
            }
        }
        catch (Exception ex)
        {
            solution.Status = SolutionStatus.Error;
            solution.ErrorMessage = ex.Message;
        }

        return solution;
    }

    private SimplexTableau InitializeTableau(CanonicalForm problem)
    {
        int m = problem.ConstraintCount;
        int n = problem.TotalVariableCount;
        
        // Create augmented tableau: [A | I | b]
        //                           [c | 0 | 0]
        var tableau = new SimplexTableau
        {
            Matrix = new double[m + 1, n + 1],
            BasicVariables = new int[m],
            VariableCount = n,
            ConstraintCount = m
        };

        // Copy constraint matrix and RHS
        for (int i = 0; i < m; i++)
        {
            for (int j = 0; j < n; j++)
            {
                tableau.Matrix[i, j] = problem.ConstraintMatrix[i, j];
            }
            tableau.Matrix[i, n] = problem.RightHandSide[i];
        }

        // Copy objective coefficients (bottom row)
        for (int j = 0; j < n; j++)
        {
            // The canonical form already has the correct signs for the tableau
            // No additional negation needed
            tableau.Matrix[m, j] = problem.ObjectiveCoefficients[j];
        }
        tableau.Matrix[m, n] = 0; // Objective value starts at 0

        // Initialize basic variables (slack variables)
        for (int i = 0; i < m; i++)
        {
            tableau.BasicVariables[i] = n - problem.SlackVariableCount + i;
        }

        return tableau;
    }

    private bool IsOptimal(SimplexTableau tableau)
    {
        // For minimization: optimal if all coefficients in objective row >= 0
        int m = tableau.ConstraintCount;
        int n = tableau.VariableCount;
        
        for (int j = 0; j < n; j++)
        {
            if (tableau.Matrix[m, j] < -EPSILON)
                return false;
        }
        return true;
    }

    private int FindEnteringVariable(SimplexTableau tableau)
    {
        int m = tableau.ConstraintCount;
        int n = tableau.VariableCount;
        
        int enteringVar = -1;
        double mostNegative = 0;
        
        for (int j = 0; j < n; j++)
        {
            if (tableau.Matrix[m, j] < mostNegative)
            {
                mostNegative = tableau.Matrix[m, j];
                enteringVar = j;
            }
        }
        
        return enteringVar;
    }

    private int FindLeavingVariable(SimplexTableau tableau, int enteringVar)
    {
        int m = tableau.ConstraintCount;
        int n = tableau.VariableCount;
        
        int leavingVar = -1;
        double minRatio = double.MaxValue;
        
        for (int i = 0; i < m; i++)
        {
            double pivot = tableau.Matrix[i, enteringVar];
            if (pivot > EPSILON)
            {
                double ratio = tableau.Matrix[i, n] / pivot;
                if (ratio < minRatio)
                {
                    minRatio = ratio;
                    leavingVar = i;
                }
            }
        }
        
        return leavingVar;
    }

    private void Pivot(SimplexTableau tableau, int pivotRow, int pivotCol)
    {
        int m = tableau.ConstraintCount + 1; // Include objective row
        int n = tableau.VariableCount + 1;   // Include RHS column
        
        double pivotElement = tableau.Matrix[pivotRow, pivotCol];
        
        // Scale pivot row
        for (int j = 0; j < n; j++)
        {
            tableau.Matrix[pivotRow, j] /= pivotElement;
        }
        
        // Eliminate other rows
        for (int i = 0; i < m; i++)
        {
            if (i != pivotRow)
            {
                double multiplier = tableau.Matrix[i, pivotCol];
                for (int j = 0; j < n; j++)
                {
                    tableau.Matrix[i, j] -= multiplier * tableau.Matrix[pivotRow, j];
                }
            }
        }
    }

    private void ExtractSolution(SimplexTableau tableau, CanonicalForm problem, SimplexSolution solution)
    {
        int n = problem.TotalVariableCount;
        solution.Variables = new double[n];
        solution.BasicVariables = new List<int>();
        solution.NonBasicVariables = new List<int>();
        
        // Initialize all variables to 0
        for (int j = 0; j < n; j++)
        {
            solution.Variables[j] = 0;
        }
        
        // Set values for basic variables
        for (int i = 0; i < tableau.ConstraintCount; i++)
        {
            int basicVar = tableau.BasicVariables[i];
            solution.Variables[basicVar] = tableau.Matrix[i, tableau.VariableCount]; // RHS column
            solution.BasicVariables.Add(basicVar);
        }
        
        // Identify non-basic variables
        for (int j = 0; j < n; j++)
        {
            if (!solution.BasicVariables.Contains(j))
            {
                solution.NonBasicVariables.Add(j);
            }
        }
        
        // Calculate objective value
        // The tableau contains the negative of the objective value for maximization problems
        // due to canonical form conversion
        solution.ObjectiveValue = problem.IsMaximization ? 
            -tableau.Matrix[tableau.ConstraintCount, tableau.VariableCount] : 
            tableau.Matrix[tableau.ConstraintCount, tableau.VariableCount];
    }

    private IterationData CreateIterationData(SimplexTableau tableau, int iterationNumber, string description)
    {
        var iteration = new IterationData
        {
            IterationNumber = iterationNumber,
            AlgorithmType = "Primal Simplex",
            Description = description,
            Status = iterationNumber == 0 ? "Initial" : "In Progress",
            IsOptimal = IsOptimal(tableau),
            IsFinal = IsOptimal(tableau)
        };

        // Convert tableau to display format
        CreateTableauRows(tableau, iteration);
        
        return iteration;
    }

    private void CreateTableauRows(SimplexTableau tableau, IterationData iteration)
    {
        int m = tableau.ConstraintCount;
        int n = tableau.VariableCount;
        
        // Create variable column headers
        for (int j = 0; j < n; j++)
        {
            iteration.VariableColumns.Add($"x{j + 1}");
        }
        iteration.VariableColumns.Add("RHS");
        
        // Create constraint rows
        for (int i = 0; i < m; i++)
        {
            var row = new TableauRow
            {
                BasisVariable = $"x{tableau.BasicVariables[i] + 1}",
                Rhs = tableau.Matrix[i, n]
            };
            
            for (int j = 0; j < n; j++)
            {
                row.Coefficients.Add(tableau.Matrix[i, j]);
            }
            
            iteration.TableauRows.Add(row);
        }
        
        // Add objective row
        var objRow = new TableauRow
        {
            BasisVariable = "Z",
            Rhs = tableau.Matrix[m, n]
        };
        
        for (int j = 0; j < n; j++)
        {
            objRow.Coefficients.Add(tableau.Matrix[m, j]);
        }
        
        iteration.TableauRows.Add(objRow);
    }
}

public class SimplexTableau
{
    public double[,] Matrix { get; set; } = new double[0, 0];
    public int[] BasicVariables { get; set; } = Array.Empty<int>();
    public int VariableCount { get; set; }
    public int ConstraintCount { get; set; }
}

public class SimplexSolution
{
    public string Algorithm { get; set; } = "";
    public CanonicalForm Problem { get; set; } = new();
    public SolutionStatus Status { get; set; }
    public string ErrorMessage { get; set; } = "";
    public double[] Variables { get; set; } = Array.Empty<double>();
    public double ObjectiveValue { get; set; }
    public List<int> BasicVariables { get; set; } = new();
    public List<int> NonBasicVariables { get; set; } = new();
    public List<IterationData> Iterations { get; set; } = new();
}

public enum SolutionStatus
{
    Optimal,
    Infeasible,
    Unbounded,
    MaxIterationsReached,
    Error
}