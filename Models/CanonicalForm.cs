using System;
using System.Collections.Generic;
using System.Linq;

namespace linear_programming_solver.Models;

public class CanonicalFormConverter
{
    public static CanonicalForm Convert(LinearProgram program)
    {
        var canonical = new CanonicalForm();
        
        // Convert to minimization if maximization
        var objectiveCoeff = program.ObjectiveCoefficients.ToArray();
        if (program.IsMaximization)
        {
            for (int i = 0; i < objectiveCoeff.Length; i++)
                objectiveCoeff[i] = -objectiveCoeff[i];
        }
        canonical.IsMaximization = program.IsMaximization;
        
        // Handle variable restrictions and count total variables
        var variableMap = ProcessVariableRestrictions(program, out int totalVars);
        canonical.OriginalVariableCount = program.VariableCount;
        
        // Expand objective coefficients for unrestricted variables
        var expandedObjective = ExpandObjectiveCoefficients(objectiveCoeff, variableMap, totalVars);
        
        // Convert constraints and add slack/surplus variables
        var (constraintMatrix, rhsVector, slackCount) = ConvertConstraints(program, variableMap, totalVars);
        
        // Final objective with slack variables (coefficient 0)
        var finalObjective = expandedObjective.Concat(Enumerable.Repeat(0.0, slackCount)).ToArray();
        
        canonical.ObjectiveCoefficients = finalObjective;
        canonical.ConstraintMatrix = constraintMatrix;
        canonical.RightHandSide = rhsVector;
        canonical.TotalVariableCount = totalVars + slackCount;
        canonical.SlackVariableCount = slackCount;
        canonical.VariableMap = variableMap;
        
        return canonical;
    }

    private static List<VariableMapping> ProcessVariableRestrictions(LinearProgram program, out int totalVars)
    {
        var variableMap = new List<VariableMapping>();
        int currentIndex = 0;
        
        for (int i = 0; i < program.VariableCount; i++)
        {
            var mapping = new VariableMapping
            {
                OriginalIndex = i,
                OriginalType = program.VariableTypes[i]
            };
            
            switch (program.VariableTypes[i])
            {
                case VariableType.NonNegative:
                case VariableType.Integer:
                case VariableType.Binary:
                    // Single variable
                    mapping.CanonicalIndices = new[] { currentIndex++ };
                    break;
                    
                case VariableType.NonPositive:
                    // Single variable (will negate coefficients)
                    mapping.CanonicalIndices = new[] { currentIndex++ };
                    break;
                    
                case VariableType.Unrestricted:
                    // Split into x_i^+ - x_i^- where both >= 0
                    mapping.CanonicalIndices = new[] { currentIndex++, currentIndex++ };
                    break;
            }
            
            variableMap.Add(mapping);
        }
        
        totalVars = currentIndex;
        return variableMap;
    }

    private static double[] ExpandObjectiveCoefficients(double[] original, List<VariableMapping> variableMap, int totalVars)
    {
        var expanded = new double[totalVars];
        
        foreach (var mapping in variableMap)
        {
            var origCoeff = original[mapping.OriginalIndex];
            
            switch (mapping.OriginalType)
            {
                case VariableType.NonNegative:
                case VariableType.Integer:
                case VariableType.Binary:
                    expanded[mapping.CanonicalIndices[0]] = origCoeff;
                    break;
                    
                case VariableType.NonPositive:
                    // Negate coefficient for non-positive variable
                    expanded[mapping.CanonicalIndices[0]] = -origCoeff;
                    break;
                    
                case VariableType.Unrestricted:
                    // x_i = x_i^+ - x_i^-, so coeff * x_i = coeff * x_i^+ - coeff * x_i^-
                    expanded[mapping.CanonicalIndices[0]] = origCoeff;  // x_i^+
                    expanded[mapping.CanonicalIndices[1]] = -origCoeff; // x_i^-
                    break;
            }
        }
        
        return expanded;
    }

    private static (double[,] matrix, double[] rhs, int slackCount) ConvertConstraints(
        LinearProgram program, List<VariableMapping> variableMap, int totalVars)
    {
        var constraints = program.Constraints;
        int slackCount = 0;
        
        // Count slack/surplus variables needed
        foreach (var constraint in constraints)
        {
            if (constraint.Type != ConstraintType.Equal)
                slackCount++;
        }
        
        var matrix = new double[constraints.Count, totalVars + slackCount];
        var rhs = new double[constraints.Count];
        int slackIndex = 0;
        
        for (int row = 0; row < constraints.Count; row++)
        {
            var constraint = constraints[row];
            rhs[row] = constraint.RightHandSide;
            
            // Expand constraint coefficients based on variable mapping
            for (int origVar = 0; origVar < program.VariableCount; origVar++)
            {
                var mapping = variableMap[origVar];
                var origCoeff = constraint.Coefficients[origVar];
                
                switch (mapping.OriginalType)
                {
                    case VariableType.NonNegative:
                    case VariableType.Integer:
                    case VariableType.Binary:
                        matrix[row, mapping.CanonicalIndices[0]] = origCoeff;
                        break;
                        
                    case VariableType.NonPositive:
                        // Negate coefficient for non-positive variable
                        matrix[row, mapping.CanonicalIndices[0]] = -origCoeff;
                        break;
                        
                    case VariableType.Unrestricted:
                        // x_i = x_i^+ - x_i^-
                        matrix[row, mapping.CanonicalIndices[0]] = origCoeff;   // x_i^+
                        matrix[row, mapping.CanonicalIndices[1]] = -origCoeff;  // x_i^-
                        break;
                }
            }
            
            // Add slack/surplus variable
            if (constraint.Type == ConstraintType.LessEqual)
            {
                matrix[row, totalVars + slackIndex] = 1.0; // slack variable
                slackIndex++;
            }
            else if (constraint.Type == ConstraintType.GreaterEqual)
            {
                matrix[row, totalVars + slackIndex] = -1.0; // surplus variable
                slackIndex++;
            }
        }
        
        return (matrix, rhs, slackCount);
    }
}

public class CanonicalForm
{
    public bool IsMaximization { get; set; }
    public double[] ObjectiveCoefficients { get; set; } = Array.Empty<double>();
    public double[,] ConstraintMatrix { get; set; } = new double[0, 0];
    public double[] RightHandSide { get; set; } = Array.Empty<double>();
    public int OriginalVariableCount { get; set; }
    public int TotalVariableCount { get; set; }
    public int SlackVariableCount { get; set; }
    public List<VariableMapping> VariableMap { get; set; } = new();
    
    public int ConstraintCount => RightHandSide.Length;
    
    public string GetVariableName(int index)
    {
        if (index < TotalVariableCount - SlackVariableCount)
        {
            // Find original variable this maps to
            foreach (var mapping in VariableMap)
            {
                for (int i = 0; i < mapping.CanonicalIndices.Length; i++)
                {
                    if (mapping.CanonicalIndices[i] == index)
                    {
                        if (mapping.OriginalType == VariableType.Unrestricted)
                            return i == 0 ? $"x{mapping.OriginalIndex + 1}+" : $"x{mapping.OriginalIndex + 1}-";
                        else
                            return $"x{mapping.OriginalIndex + 1}";
                    }
                }
            }
        }
        
        // Slack variable
        int slackIndex = index - (TotalVariableCount - SlackVariableCount) + 1;
        return $"s{slackIndex}";
    }
}

public class VariableMapping
{
    public int OriginalIndex { get; set; }
    public VariableType OriginalType { get; set; }
    public int[] CanonicalIndices { get; set; } = Array.Empty<int>();
}