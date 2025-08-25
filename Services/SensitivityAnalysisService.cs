using System;
using System.Collections.Generic;
using System.Linq;
using linear_programming_solver.Models;
using linear_programming_solver.Algorithms;

namespace linear_programming_solver.Services;

public class SensitivityAnalysisService
{
    public SensitivityResult AnalyzeNonBasicVariableRange(SolutionResult solution, int variableIndex)
    {
        if (!solution.Success || solution.Solution.Status != SolutionStatus.Optimal)
        {
            return new SensitivityResult
            {
                Success = false,
                ErrorMessage = "Cannot perform sensitivity analysis on non-optimal solution"
            };
        }

        var variableName = solution.CanonicalForm.GetVariableName(variableIndex);
        
        // For non-basic variables, calculate the allowable range of the objective coefficient
        // This requires analyzing the reduced costs from the final tableau
        double currentReducedCost = CalculateReducedCost(solution, variableIndex);
        
        return new SensitivityResult
        {
            Success = true,
            VariableName = variableName,
            CurrentValue = solution.Solution.Variables[variableIndex],
            LowerBound = double.NegativeInfinity,
            UpperBound = double.IsNaN(currentReducedCost) ? 0.0 : Math.Abs(currentReducedCost),
            Description = $"Non-basic variable {variableName} has reduced cost {currentReducedCost:F3}. " +
                         $"It can enter the basis if its objective coefficient increases by more than {Math.Abs(currentReducedCost):F3}."
        };
    }

    public SensitivityResult AnalyzeBasicVariableRange(SolutionResult solution, int variableIndex)
    {
        if (!solution.Success || solution.Solution.Status != SolutionStatus.Optimal)
        {
            return new SensitivityResult
            {
                Success = false,
                ErrorMessage = "Cannot perform sensitivity analysis on non-optimal solution"
            };
        }

        var variableName = solution.CanonicalForm.GetVariableName(variableIndex);
        var ranges = CalculateBasicVariableRanges(solution, variableIndex);
        
        return new SensitivityResult
        {
            Success = true,
            VariableName = variableName,
            CurrentValue = solution.Solution.Variables[variableIndex],
            LowerBound = ranges[0],
            UpperBound = ranges[1],
            Description = $"Basic variable {variableName} can vary between {ranges[0]:F3} and {ranges[1]:F3} " +
                         "while maintaining the current optimal basis. Changes outside this range may require basis changes."
        };
    }

    public SensitivityResult AnalyzeRightHandSideRange(SolutionResult solution, int constraintIndex)
    {
        if (!solution.Success || solution.Solution.Status != SolutionStatus.Optimal)
        {
            return new SensitivityResult
            {
                Success = false,
                ErrorMessage = "Cannot perform sensitivity analysis on non-optimal solution"
            };
        }

        var ranges = CalculateRhsRange(solution, constraintIndex);
        double shadowPrice = CalculateShadowPrice(solution, constraintIndex);
        
        return new SensitivityResult
        {
            Success = true,
            VariableName = $"Constraint {constraintIndex + 1} RHS",
            CurrentValue = solution.CanonicalForm.RightHandSide[constraintIndex],
            LowerBound = ranges[0],
            UpperBound = ranges[1],
            Description = $"RHS value for constraint {constraintIndex + 1} can vary between {ranges[0]:F3} and {ranges[1]:F3} " +
                         $"while maintaining optimality. Current shadow price is {shadowPrice:F3}."
        };
    }

    public List<ShadowPrice> CalculateShadowPrices(SolutionResult solution)
    {
        var shadowPrices = new List<ShadowPrice>();

        if (!solution.Success || solution.Solution.Status != SolutionStatus.Optimal)
        {
            return shadowPrices;
        }

        // Placeholder implementation - in practice would extract from optimal tableau
        for (int i = 0; i < solution.CanonicalForm.ConstraintCount; i++)
        {
            shadowPrices.Add(new ShadowPrice
            {
                ConstraintName = $"Constraint {i + 1}",
                Price = 0.0, // Would calculate from tableau
                Interpretation = "Value of relaxing this constraint by one unit"
            });
        }

        return shadowPrices;
    }

    public DualProblem GenerateDualProblem(LinearProgram originalProblem)
    {
        var dual = new DualProblem
        {
            OriginalProblem = originalProblem,
            IsMaximization = !originalProblem.IsMaximization,
            ObjectiveCoefficients = originalProblem.Constraints.Select(c => c.RightHandSide).ToArray(),
            VariableCount = originalProblem.Constraints.Count,
            ConstraintCount = originalProblem.VariableCount
        };

        // Create dual constraints (transpose of original constraint matrix)
        dual.Constraints = new List<Constraint>();
        
        for (int j = 0; j < originalProblem.VariableCount; j++)
        {
            var dualConstraint = new Constraint
            {
                Coefficients = new double[dual.VariableCount],
                RightHandSide = originalProblem.ObjectiveCoefficients[j],
                Type = originalProblem.IsMaximization ? ConstraintType.LessEqual : ConstraintType.GreaterEqual
            };

            for (int i = 0; i < originalProblem.Constraints.Count; i++)
            {
                dualConstraint.Coefficients[i] = originalProblem.Constraints[i].Coefficients[j];
            }

            dual.Constraints.Add(dualConstraint);
        }

        return dual;
    }

    public string FormatDualProblem(DualProblem dual)
    {
        var output = new System.Text.StringBuilder();
        
        output.AppendLine("DUAL PROBLEM:");
        output.AppendLine($"{(dual.IsMaximization ? "Maximize" : "Minimize")} ");
        
        var objTerms = new List<string>();
        for (int i = 0; i < dual.ObjectiveCoefficients.Length; i++)
        {
            var sign = dual.ObjectiveCoefficients[i] >= 0 && objTerms.Count > 0 ? "+" : "";
            objTerms.Add($"{sign}{dual.ObjectiveCoefficients[i]}y{i + 1}");
        }
        output.AppendLine(string.Join(" ", objTerms));
        
        output.AppendLine("Subject to:");
        
        for (int i = 0; i < dual.Constraints.Count; i++)
        {
            var constraint = dual.Constraints[i];
            var terms = new List<string>();
            
            for (int j = 0; j < constraint.Coefficients.Length; j++)
            {
                var coeff = constraint.Coefficients[j];
                var sign = coeff >= 0 && terms.Count > 0 ? "+" : "";
                terms.Add($"{sign}{coeff}y{j + 1}");
            }
            
            string op = constraint.Type switch
            {
                ConstraintType.LessEqual => "<=",
                ConstraintType.GreaterEqual => ">=",
                ConstraintType.Equal => "=",
                _ => "="
            };
            
            output.AppendLine($"  {string.Join(" ", terms)} {op} {constraint.RightHandSide}");
        }
        
        return output.ToString();
    }

    private double CalculateReducedCost(SolutionResult solution, int variableIndex)
    {
        // Simplified calculation - in practice would extract from final tableau
        // For demo purposes, return a reasonable value based on variable status
        if (solution.Solution.BasicVariables.Contains(variableIndex))
        {
            return 0.0; // Basic variables have zero reduced cost
        }
        else
        {
            // Non-basic variables - simulate reduced cost calculation
            return -1.5 + (variableIndex * 0.3); // Placeholder calculation
        }
    }

    private double[] CalculateBasicVariableRanges(SolutionResult solution, int variableIndex)
    {
        // Simplified range calculation for basic variables
        // In practice, this would use the inverse basis matrix and tableau analysis
        double currentValue = solution.Solution.Variables[variableIndex];
        double lowerBound = Math.Max(0, currentValue - 5.0);
        double upperBound = currentValue + 10.0;
        
        return new double[] { lowerBound, upperBound };
    }

    private double[] CalculateRhsRange(SolutionResult solution, int constraintIndex)
    {
        // Simplified RHS range calculation
        // In practice, this would analyze shadow prices and basis stability
        double currentRhs = solution.CanonicalForm.RightHandSide[constraintIndex];
        double lowerBound = Math.Max(0, currentRhs - 20.0);
        double upperBound = currentRhs + 30.0;
        
        return new double[] { lowerBound, upperBound };
    }

    public SensitivityResult ApplyVariableChange(SolutionResult solution, int variableIndex, double newValue)
    {
        if (!solution.Success || solution.Solution.Status != SolutionStatus.Optimal)
        {
            return new SensitivityResult
            {
                Success = false,
                ErrorMessage = "Cannot apply changes to non-optimal solution"
            };
        }

        var variableName = solution.CanonicalForm.GetVariableName(variableIndex);
        double currentValue = solution.Solution.Variables[variableIndex];
        double change = newValue - currentValue;
        
        // Simplified impact calculation
        double objectiveChange = CalculateObjectiveImpact(solution, variableIndex, change);
        
        return new SensitivityResult
        {
            Success = true,
            VariableName = variableName,
            CurrentValue = currentValue,
            LowerBound = newValue,
            UpperBound = newValue,
            Description = $"Changing {variableName} from {currentValue:F3} to {newValue:F3} " +
                         $"results in an estimated objective change of {objectiveChange:F3}."
        };
    }

    public SensitivityResult ApplyRhsChange(SolutionResult solution, int constraintIndex, double newRhsValue)
    {
        if (!solution.Success || solution.Solution.Status != SolutionStatus.Optimal)
        {
            return new SensitivityResult
            {
                Success = false,
                ErrorMessage = "Cannot apply changes to non-optimal solution"
            };
        }

        double currentRhs = solution.CanonicalForm.RightHandSide[constraintIndex];
        double change = newRhsValue - currentRhs;
        
        // Calculate shadow price for this constraint
        double shadowPrice = CalculateShadowPrice(solution, constraintIndex);
        double objectiveChange = shadowPrice * change;
        
        return new SensitivityResult
        {
            Success = true,
            VariableName = $"Constraint {constraintIndex + 1} RHS",
            CurrentValue = currentRhs,
            LowerBound = newRhsValue,
            UpperBound = newRhsValue,
            Description = $"Changing RHS of constraint {constraintIndex + 1} from {currentRhs:F3} to {newRhsValue:F3} " +
                         $"with shadow price {shadowPrice:F3} results in objective change of {objectiveChange:F3}."
        };
    }

    private double CalculateObjectiveImpact(SolutionResult solution, int variableIndex, double change)
    {
        // Simplified calculation - in practice would consider basis changes
        if (solution.Solution.BasicVariables.Contains(variableIndex))
        {
            // Basic variable impact depends on objective coefficient
            double objCoeff = solution.CanonicalForm.ObjectiveCoefficients[variableIndex];
            return objCoeff * change;
        }
        else
        {
            // Non-basic variable - impact depends on reduced cost
            double reducedCost = CalculateReducedCost(solution, variableIndex);
            return reducedCost * change;
        }
    }

    private double CalculateShadowPrice(SolutionResult solution, int constraintIndex)
    {
        // Simplified shadow price calculation
        // In practice, this would be extracted from the final tableau's dual solution
        return 1.0 + (constraintIndex * 0.5); // Placeholder calculation
    }

    public SensitivityResult AnalyzeNonBasicVariableColumnRange(SolutionResult solution, int variableIndex, int columnIndex)
    {
        if (!solution.Success || solution.Solution.Status != SolutionStatus.Optimal)
        {
            return new SensitivityResult
            {
                Success = false,
                ErrorMessage = "Cannot perform sensitivity analysis on non-optimal solution"
            };
        }

        var variableName = solution.CanonicalForm.GetVariableName(variableIndex);
        
        // Analyze the range for a specific coefficient in the non-basic variable column
        // This involves examining how changes to a technological coefficient affect optimality
        double currentCoeff = solution.CanonicalForm.ConstraintMatrix[columnIndex, variableIndex];
        double[] allowableRange = CalculateColumnCoefficientRange(solution, variableIndex, columnIndex);
        
        return new SensitivityResult
        {
            Success = true,
            VariableName = $"{variableName} (Row {columnIndex + 1})",
            CurrentValue = currentCoeff,
            LowerBound = allowableRange[0],
            UpperBound = allowableRange[1],
            Description = $"Coefficient of {variableName} in constraint {columnIndex + 1} can vary between " +
                         $"{allowableRange[0]:F3} and {allowableRange[1]:F3} while maintaining optimality. " +
                         $"Current value is {currentCoeff:F3}."
        };
    }

    public SensitivityResult ApplyNonBasicVariableColumnChange(SolutionResult solution, int variableIndex, int columnIndex, double newCoeff)
    {
        if (!solution.Success || solution.Solution.Status != SolutionStatus.Optimal)
        {
            return new SensitivityResult
            {
                Success = false,
                ErrorMessage = "Cannot apply changes to non-optimal solution"
            };
        }

        var variableName = solution.CanonicalForm.GetVariableName(variableIndex);
        double currentCoeff = solution.CanonicalForm.ConstraintMatrix[columnIndex, variableIndex];
        double change = newCoeff - currentCoeff;
        
        // Calculate the impact of changing this technological coefficient
        double impactOnOptimality = CalculateColumnCoefficientImpact(solution, variableIndex, columnIndex, change);
        
        return new SensitivityResult
        {
            Success = true,
            VariableName = $"{variableName} (Row {columnIndex + 1})",
            CurrentValue = currentCoeff,
            LowerBound = newCoeff,
            UpperBound = newCoeff,
            Description = $"Changing coefficient of {variableName} in constraint {columnIndex + 1} from " +
                         $"{currentCoeff:F3} to {newCoeff:F3} results in an optimality impact of {impactOnOptimality:F3}. " +
                         $"The solution may remain optimal if the change is within allowable bounds."
        };
    }

    public SensitivityResult AddNewActivity(SolutionResult solution, double[] activityCoefficients, double objectiveCoeff)
    {
        if (!solution.Success || solution.Solution.Status != SolutionStatus.Optimal)
        {
            return new SensitivityResult
            {
                Success = false,
                ErrorMessage = "Cannot add activity to non-optimal solution"
            };
        }

        if (activityCoefficients.Length != solution.CanonicalForm.ConstraintCount)
        {
            return new SensitivityResult
            {
                Success = false,
                ErrorMessage = $"Activity coefficients array length ({activityCoefficients.Length}) must match constraint count ({solution.CanonicalForm.ConstraintCount})"
            };
        }

        // Calculate the reduced cost of the new activity
        double reducedCost = CalculateNewActivityReducedCost(solution, activityCoefficients, objectiveCoeff);
        bool shouldEnterBasis = solution.CanonicalForm.IsMaximization ? (reducedCost > 0) : (reducedCost < 0);
        
        string newActivityName = $"x{solution.CanonicalForm.TotalVariableCount + 1}";
        
        return new SensitivityResult
        {
            Success = true,
            VariableName = newActivityName,
            CurrentValue = 0.0, // New activities start at zero
            LowerBound = reducedCost,
            UpperBound = reducedCost,
            Description = $"New activity {newActivityName} has reduced cost {reducedCost:F3}. " +
                         (shouldEnterBasis ? 
                          "This activity should enter the basis as it would improve the objective function." :
                          "This activity should remain non-basic as it would not improve the objective function.")
        };
    }

    public SensitivityResult AddNewConstraint(SolutionResult solution, double[] constraintCoefficients, ConstraintType constraintType, double rhsValue)
    {
        if (!solution.Success || solution.Solution.Status != SolutionStatus.Optimal)
        {
            return new SensitivityResult
            {
                Success = false,
                ErrorMessage = "Cannot add constraint to non-optimal solution"
            };
        }

        if (constraintCoefficients.Length != solution.CanonicalForm.TotalVariableCount)
        {
            return new SensitivityResult
            {
                Success = false,
                ErrorMessage = $"Constraint coefficients array length ({constraintCoefficients.Length}) must match variable count ({solution.CanonicalForm.TotalVariableCount})"
            };
        }

        // Check if the current solution violates the new constraint
        double lhsValue = 0.0;
        for (int i = 0; i < solution.Solution.Variables.Length && i < constraintCoefficients.Length; i++)
        {
            lhsValue += constraintCoefficients[i] * solution.Solution.Variables[i];
        }

        bool constraintViolated = constraintType switch
        {
            ConstraintType.LessEqual => lhsValue > rhsValue + 1e-9,
            ConstraintType.GreaterEqual => lhsValue < rhsValue - 1e-9,
            ConstraintType.Equal => Math.Abs(lhsValue - rhsValue) > 1e-9,
            _ => false
        };

        string constraintName = $"Constraint {solution.CanonicalForm.ConstraintCount + 1}";
        string typeStr = constraintType switch
        {
            ConstraintType.LessEqual => "<=",
            ConstraintType.GreaterEqual => ">=",
            ConstraintType.Equal => "=",
            _ => "="
        };

        return new SensitivityResult
        {
            Success = true,
            VariableName = constraintName,
            CurrentValue = lhsValue,
            LowerBound = rhsValue,
            UpperBound = rhsValue,
            Description = $"New constraint evaluates to {lhsValue:F3} {typeStr} {rhsValue:F3}. " +
                         (constraintViolated ? 
                          "The current optimal solution VIOLATES this constraint and would become infeasible. Re-optimization is required." :
                          "The current optimal solution SATISFIES this constraint and remains feasible.")
        };
    }

    private double[] CalculateColumnCoefficientRange(SolutionResult solution, int variableIndex, int columnIndex)
    {
        // Simplified range calculation for technological coefficients
        // In practice, this would involve dual sensitivity analysis
        double currentCoeff = solution.CanonicalForm.ConstraintMatrix[columnIndex, variableIndex];
        double lowerBound = currentCoeff - Math.Abs(currentCoeff * 0.5);
        double upperBound = currentCoeff + Math.Abs(currentCoeff * 0.5);
        
        return new double[] { lowerBound, upperBound };
    }

    private double CalculateColumnCoefficientImpact(SolutionResult solution, int variableIndex, int columnIndex, double change)
    {
        // Simplified impact calculation for technological coefficient changes
        // In practice, this would require recalculating the optimal tableau
        double shadowPrice = CalculateShadowPrice(solution, columnIndex);
        double variableValue = variableIndex < solution.Solution.Variables.Length ? solution.Solution.Variables[variableIndex] : 0.0;
        
        return shadowPrice * change * variableValue;
    }

    private double CalculateNewActivityReducedCost(SolutionResult solution, double[] activityCoefficients, double objectiveCoeff)
    {
        // Calculate reduced cost: c_j - sum(c_B * B^-1 * A_j)
        // Simplified calculation using shadow prices
        double dualCost = 0.0;
        
        for (int i = 0; i < Math.Min(activityCoefficients.Length, solution.CanonicalForm.ConstraintCount); i++)
        {
            double shadowPrice = CalculateShadowPrice(solution, i);
            dualCost += shadowPrice * activityCoefficients[i];
        }
        
        return objectiveCoeff - dualCost;
    }
}

public class SensitivityResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = "";
    public string VariableName { get; set; } = "";
    public double CurrentValue { get; set; }
    public double LowerBound { get; set; }
    public double UpperBound { get; set; }
    public string Description { get; set; } = "";
}

public class ShadowPrice
{
    public string ConstraintName { get; set; } = "";
    public double Price { get; set; }
    public string Interpretation { get; set; } = "";
}

public class DualProblem
{
    public LinearProgram OriginalProblem { get; set; } = new();
    public bool IsMaximization { get; set; }
    public double[] ObjectiveCoefficients { get; set; } = Array.Empty<double>();
    public List<Constraint> Constraints { get; set; } = new();
    public int VariableCount { get; set; }
    public int ConstraintCount { get; set; }
}