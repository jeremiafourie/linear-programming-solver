using System;
using System.Collections.Generic;
using System.Linq;
using linear_programming_solver.Models;

namespace linear_programming_solver.Algorithms;

public class RevisedPrimalSimplexSolver
{
    private const double EPSILON = 1e-10;

    public SimplexSolution Solve(CanonicalForm problem)
    {
        var solution = new SimplexSolution
        {
            Algorithm = "Revised Primal Simplex",
            Problem = problem
        };

        try
        {
            var data = InitializeRevisedSimplex(problem);
            solution.Iterations.Add(CreateIterationData(data, 0, "Initial setup"));

            int iteration = 1;
            while (!IsOptimal(data) && iteration <= 1000)
            {
                // Price out step
                var (reducedCosts, enteringVar) = PriceOut(data);
                if (enteringVar == -1)
                {
                    solution.Status = SolutionStatus.Optimal;
                    break;
                }

                // Minimum ratio test
                var (direction, leavingVar) = MinimumRatioTest(data, enteringVar);
                if (leavingVar == -1)
                {
                    solution.Status = SolutionStatus.Unbounded;
                    solution.Iterations.Add(CreateIterationData(data, iteration, "Problem is unbounded"));
                    break;
                }

                // Update basis
                UpdateBasis(data, enteringVar, leavingVar, direction);

                string iterationDesc = $"Price Out & Update: Enter x{enteringVar + 1}, Leave x{data.BasicVariables[leavingVar] + 1}";
                solution.Iterations.Add(CreateIterationData(data, iteration, iterationDesc));

                iteration++;
            }

            if (iteration > 1000)
            {
                solution.Status = SolutionStatus.MaxIterationsReached;
            }
            else if (solution.Status != SolutionStatus.Unbounded)
            {
                solution.Status = SolutionStatus.Optimal;
                ExtractSolution(data, problem, solution);
            }
        }
        catch (Exception ex)
        {
            solution.Status = SolutionStatus.Error;
            solution.ErrorMessage = ex.Message;
        }

        return solution;
    }

    private RevisedSimplexData InitializeRevisedSimplex(CanonicalForm problem)
    {
        int m = problem.ConstraintCount;
        int n = problem.TotalVariableCount;
        
        var data = new RevisedSimplexData
        {
            A = new double[m, n],
            c = new double[n],
            b = new double[m],
            BasicVariables = new int[m],
            BasisInverse = new double[m, m],
            BasicSolution = new double[m],
            VariableCount = n,
            ConstraintCount = m
        };

        // Copy constraint matrix
        for (int i = 0; i < m; i++)
        {
            for (int j = 0; j < n; j++)
            {
                data.A[i, j] = problem.ConstraintMatrix[i, j];
            }
        }

        // Copy objective coefficients and RHS
        for (int j = 0; j < n; j++)
        {
            // Use the canonical form coefficients directly - they already have correct signs
            data.c[j] = problem.ObjectiveCoefficients[j];
        }
        
        for (int i = 0; i < m; i++)
        {
            data.b[i] = problem.RightHandSide[i];
        }

        // Initialize with slack variables as basic
        for (int i = 0; i < m; i++)
        {
            data.BasicVariables[i] = n - problem.SlackVariableCount + i;
            data.BasicSolution[i] = data.b[i];
            
            // Identity matrix for slack variables
            for (int j = 0; j < m; j++)
            {
                data.BasisInverse[i, j] = (i == j) ? 1.0 : 0.0;
            }
        }

        return data;
    }

    private bool IsOptimal(RevisedSimplexData data)
    {
        var (reducedCosts, _) = PriceOut(data);
        return reducedCosts.All(rc => rc >= -EPSILON);
    }

    private (double[] reducedCosts, int enteringVar) PriceOut(RevisedSimplexData data)
    {
        int m = data.ConstraintCount;
        int n = data.VariableCount;
        
        // Calculate dual variables: π = c_B * B^(-1)
        var dualVars = new double[m];
        for (int i = 0; i < m; i++)
        {
            dualVars[i] = 0;
            for (int j = 0; j < m; j++)
            {
                dualVars[i] += data.c[data.BasicVariables[j]] * data.BasisInverse[j, i];
            }
        }

        // Calculate reduced costs: c_j - π * A_j
        var reducedCosts = new double[n];
        int enteringVar = -1;
        double mostNegative = 0;

        for (int j = 0; j < n; j++)
        {
            reducedCosts[j] = data.c[j];
            for (int i = 0; i < m; i++)
            {
                reducedCosts[j] -= dualVars[i] * data.A[i, j];
            }

            if (reducedCosts[j] < mostNegative)
            {
                mostNegative = reducedCosts[j];
                enteringVar = j;
            }
        }

        return (reducedCosts, enteringVar);
    }

    private (double[] direction, int leavingVar) MinimumRatioTest(RevisedSimplexData data, int enteringVar)
    {
        int m = data.ConstraintCount;
        
        // Calculate direction: d = B^(-1) * A_j
        var direction = new double[m];
        for (int i = 0; i < m; i++)
        {
            direction[i] = 0;
            for (int k = 0; k < m; k++)
            {
                direction[i] += data.BasisInverse[i, k] * data.A[k, enteringVar];
            }
        }

        // Find minimum ratio
        int leavingVar = -1;
        double minRatio = double.MaxValue;

        for (int i = 0; i < m; i++)
        {
            if (direction[i] > EPSILON)
            {
                double ratio = data.BasicSolution[i] / direction[i];
                if (ratio < minRatio)
                {
                    minRatio = ratio;
                    leavingVar = i;
                }
            }
        }

        return (direction, leavingVar);
    }

    private void UpdateBasis(RevisedSimplexData data, int enteringVar, int leavingVar, double[] direction)
    {
        int m = data.ConstraintCount;
        
        // Update basic solution
        double pivotElement = direction[leavingVar];
        
        for (int i = 0; i < m; i++)
        {
            if (i != leavingVar)
            {
                data.BasicSolution[i] -= (direction[i] / pivotElement) * data.BasicSolution[leavingVar];
            }
        }
        data.BasicSolution[leavingVar] /= pivotElement;
        
        // Update basis inverse using product form
        var eta = new double[m, m];
        for (int i = 0; i < m; i++)
        {
            for (int j = 0; j < m; j++)
            {
                eta[i, j] = (i == j) ? 1.0 : 0.0;
            }
        }
        
        // Set eta column
        for (int i = 0; i < m; i++)
        {
            eta[i, leavingVar] = (i == leavingVar) ? 1.0 / pivotElement : -direction[i] / pivotElement;
        }
        
        // B^(-1) = eta * B^(-1)
        var newBasisInverse = new double[m, m];
        for (int i = 0; i < m; i++)
        {
            for (int j = 0; j < m; j++)
            {
                newBasisInverse[i, j] = 0;
                for (int k = 0; k < m; k++)
                {
                    newBasisInverse[i, j] += eta[i, k] * data.BasisInverse[k, j];
                }
            }
        }
        
        data.BasisInverse = newBasisInverse;
        data.BasicVariables[leavingVar] = enteringVar;
    }

    private void ExtractSolution(RevisedSimplexData data, CanonicalForm problem, SimplexSolution solution)
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
        for (int i = 0; i < data.ConstraintCount; i++)
        {
            int basicVar = data.BasicVariables[i];
            solution.Variables[basicVar] = data.BasicSolution[i];
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
        double objValue = 0;
        for (int i = 0; i < data.ConstraintCount; i++)
        {
            objValue += data.c[data.BasicVariables[i]] * data.BasicSolution[i];
        }
        
        // Since canonical form negates coefficients for maximization, we need to negate back
        solution.ObjectiveValue = problem.IsMaximization ? -objValue : objValue;
    }

    private IterationData CreateIterationData(RevisedSimplexData data, int iterationNumber, string description)
    {
        var iteration = new IterationData
        {
            IterationNumber = iterationNumber,
            AlgorithmType = "Revised Primal Simplex",
            Description = description,
            Status = iterationNumber == 0 ? "Initial" : "In Progress",
            IsOptimal = IsOptimal(data),
            IsFinal = IsOptimal(data)
        };

        // Store reduced costs and basic solution information
        iteration.Data["BasicVariables"] = data.BasicVariables.ToArray();
        iteration.Data["BasicSolution"] = data.BasicSolution.ToArray();
        
        if (iterationNumber > 0)
        {
            var (reducedCosts, _) = PriceOut(data);
            iteration.Data["ReducedCosts"] = reducedCosts;
        }
        
        // Convert to tableau display format for UI
        CreateTableauRows(data, iteration);
        
        return iteration;
    }
    
    private void CreateTableauRows(RevisedSimplexData data, IterationData iteration)
    {
        int m = data.ConstraintCount;
        int n = data.VariableCount;
        
        // Create variable column headers
        for (int j = 0; j < n; j++)
        {
            iteration.VariableColumns.Add($"x{j + 1}");
        }
        iteration.VariableColumns.Add("RHS");
        
        // Create constraint rows from basic solution and basis inverse
        for (int i = 0; i < m; i++)
        {
            var row = new TableauRow
            {
                BasisVariable = $"x{data.BasicVariables[i] + 1}",
                Rhs = data.BasicSolution[i]
            };
            
            // Calculate tableau coefficients: B^(-1) * A
            for (int j = 0; j < n; j++)
            {
                double coefficient = 0;
                for (int k = 0; k < m; k++)
                {
                    coefficient += data.BasisInverse[i, k] * data.A[k, j];
                }
                row.Coefficients.Add(coefficient);
            }
            
            iteration.TableauRows.Add(row);
        }
        
        // Add objective row (reduced costs)
        var objRow = new TableauRow
        {
            BasisVariable = "Z",
            Rhs = 0 // Calculate objective value
        };
        
        // Calculate current objective value
        double objValue = 0;
        for (int i = 0; i < m; i++)
        {
            objValue += data.c[data.BasicVariables[i]] * data.BasicSolution[i];
        }
        objRow.Rhs = objValue;
        
        // Calculate reduced costs
        if (iteration.Data.ContainsKey("ReducedCosts"))
        {
            var reducedCosts = (double[])iteration.Data["ReducedCosts"];
            for (int j = 0; j < Math.Min(n, reducedCosts.Length); j++)
            {
                objRow.Coefficients.Add(reducedCosts[j]);
            }
        }
        else
        {
            // For initial iteration, just use original costs
            for (int j = 0; j < n; j++)
            {
                objRow.Coefficients.Add(data.c[j]);
            }
        }
        
        iteration.TableauRows.Add(objRow);
    }
}

public class RevisedSimplexData
{
    public double[,] A { get; set; } = new double[0, 0];      // Constraint matrix
    public double[] c { get; set; } = Array.Empty<double>(); // Objective coefficients
    public double[] b { get; set; } = Array.Empty<double>(); // RHS
    public int[] BasicVariables { get; set; } = Array.Empty<int>();
    public double[,] BasisInverse { get; set; } = new double[0, 0];
    public double[] BasicSolution { get; set; } = Array.Empty<double>();
    public int VariableCount { get; set; }
    public int ConstraintCount { get; set; }
}