using System;
using System.Collections.Generic;
using System.Linq;

namespace linear_programming_solver.Models;

public class LinearProgramParser
{
    public static LinearProgram Parse(string content)
    {
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                          .Select(line => line.Trim())
                          .Where(line => !string.IsNullOrEmpty(line))
                          .ToArray();

        if (lines.Length < 3)
            throw new ArgumentException("Invalid input format: minimum 3 lines required");

        var program = new LinearProgram();
        
        // Parse objective function
        ParseObjectiveFunction(lines[0], program);
        
        // Parse constraints
        for (int i = 1; i < lines.Length - 1; i++)
        {
            ParseConstraint(lines[i], program);
        }
        
        // Parse variable restrictions
        ParseVariableRestrictions(lines[^1], program);
        
        return program;
    }

    private static void ParseObjectiveFunction(string line, LinearProgram program)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length < 2)
            throw new ArgumentException($"Invalid objective function format: {line}");
        
        // First part is max/min
        program.IsMaximization = parts[0].ToLower() == "max";
        
        // Parse coefficients - each part after "max"/"min" is a signed coefficient like "+2", "-3"
        var coefficients = new List<double>();
        for (int i = 1; i < parts.Length; i++)
        {
            if (!double.TryParse(parts[i], out double value))
                throw new ArgumentException($"Invalid coefficient value '{parts[i]}' in objective: {line}");
                
            coefficients.Add(value);
        }
        
        if (coefficients.Count == 0)
            throw new ArgumentException($"No valid coefficients found in objective: {line}");
        
        program.ObjectiveCoefficients = coefficients.ToArray();
        program.VariableCount = coefficients.Count;
    }

    private static void ParseConstraint(string line, LinearProgram program)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length < 3)
            throw new ArgumentException($"Invalid constraint format: {line}");
        
        var coefficients = new List<double>();
        int i = 0;
        
        // Parse technological coefficients - each part is a signed coefficient like "+11", "-8"
        while (i < parts.Length && coefficients.Count < program.VariableCount)
        {
            if (!double.TryParse(parts[i], out double value))
            {
                // If parsing fails, we've reached the constraint type (<=, >=, =)
                break;
            }
                
            coefficients.Add(value);
            i++;
        }
        
        if (coefficients.Count != program.VariableCount)
            throw new ArgumentException($"Expected {program.VariableCount} coefficients, found {coefficients.Count} in constraint: {line}");
        
        if (i >= parts.Length)
            throw new ArgumentException($"Missing constraint type in: {line}");
            
        // Parse constraint type
        var constraintType = parts[i];
        i++;
        
        if (i >= parts.Length)
            throw new ArgumentException($"Missing RHS value in: {line}");
            
        if (!double.TryParse(parts[i], out double rhs))
            throw new ArgumentException($"Invalid RHS value '{parts[i]}' in constraint: {line}");
        
        var constraint = new Constraint
        {
            Coefficients = coefficients.ToArray(),
            Type = constraintType switch
            {
                "<=" => ConstraintType.LessEqual,
                ">=" => ConstraintType.GreaterEqual,
                "=" => ConstraintType.Equal,
                _ => throw new ArgumentException($"Invalid constraint type: {constraintType}")
            },
            RightHandSide = rhs
        };
        
        program.Constraints.Add(constraint);
    }

    private static void ParseVariableRestrictions(string line, LinearProgram program)
    {
        var restrictions = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (restrictions.Length != program.VariableCount)
            throw new ArgumentException("Variable restrictions count doesn't match variable count");
        
        var variableTypes = new VariableType[program.VariableCount];
        
        for (int i = 0; i < restrictions.Length; i++)
        {
            variableTypes[i] = restrictions[i].ToLower() switch
            {
                "+" => VariableType.NonNegative,
                "-" => VariableType.NonPositive,
                "urs" => VariableType.Unrestricted,
                "int" => VariableType.Integer,
                "bin" => VariableType.Binary,
                _ => throw new ArgumentException($"Invalid variable restriction: {restrictions[i]}")
            };
        }
        
        program.VariableTypes = variableTypes;
    }
}

public class LinearProgram
{
    public bool IsMaximization { get; set; }
    public double[] ObjectiveCoefficients { get; set; } = Array.Empty<double>();
    public List<Constraint> Constraints { get; set; } = new();
    public VariableType[] VariableTypes { get; set; } = Array.Empty<VariableType>();
    public int VariableCount { get; set; }
    
    public bool IsIntegerProgram => VariableTypes.Any(t => t == VariableType.Integer || t == VariableType.Binary);
    public bool IsBinaryProgram => VariableTypes.All(t => t == VariableType.Binary);
}

public class Constraint
{
    public double[] Coefficients { get; set; } = Array.Empty<double>();
    public ConstraintType Type { get; set; }
    public double RightHandSide { get; set; }
}

public enum ConstraintType
{
    LessEqual,
    GreaterEqual,
    Equal
}

public enum VariableType
{
    NonNegative,
    NonPositive,
    Unrestricted,
    Integer,
    Binary
}