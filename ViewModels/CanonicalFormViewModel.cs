using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using linear_programming_solver.Models;

namespace linear_programming_solver.ViewModels;

public partial class CanonicalFormViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _originalProblemText = "";
    
    [ObservableProperty]
    private string _canonicalFormText = "";
    
    [ObservableProperty]
    private string _objectiveFunction = "";
    
    [ObservableProperty]
    private string _problemType = "";
    
    [ObservableProperty]
    private int _originalVariableCount;
    
    [ObservableProperty]
    private int _totalVariableCount;
    
    [ObservableProperty]
    private int _constraintCount;
    
    [ObservableProperty]
    private int _slackVariableCount;
    
    public ObservableCollection<string> Constraints { get; } = new();
    public ObservableCollection<VariableMappingDisplay> VariableMappings { get; } = new();
    
    public void LoadCanonicalForm(LinearProgram originalProblem, CanonicalForm canonicalForm)
    {
        // Clear previous data
        Constraints.Clear();
        VariableMappings.Clear();
        
        // Set basic information
        ProblemType = originalProblem.IsMaximization ? "Maximization" : "Minimization";
        OriginalVariableCount = originalProblem.VariableCount;
        TotalVariableCount = canonicalForm.TotalVariableCount;
        ConstraintCount = canonicalForm.ConstraintCount;
        SlackVariableCount = canonicalForm.SlackVariableCount;
        
        // Format original problem
        OriginalProblemText = FormatOriginalProblem(originalProblem);
        
        // Format canonical form
        CanonicalFormText = FormatCanonicalForm(canonicalForm);
        
        // Format objective function
        ObjectiveFunction = FormatObjectiveFunction(canonicalForm);
        
        // Format constraints
        FormatConstraints(canonicalForm);
        
        // Format variable mappings
        FormatVariableMappings(canonicalForm);
    }
    
    private string FormatOriginalProblem(LinearProgram problem)
    {
        var lines = new List<string>();
        
        // Objective function
        var objTerms = new List<string>();
        for (int i = 0; i < problem.VariableCount; i++)
        {
            var coeff = problem.ObjectiveCoefficients[i];
            var sign = coeff >= 0 ? "+" : "";
            objTerms.Add($"{sign}{coeff:F1}x{i + 1}");
        }
        lines.Add($"{(problem.IsMaximization ? "Maximize" : "Minimize")} Z = {string.Join(" ", objTerms)}");
        
        lines.Add("");
        lines.Add("Subject to:");
        
        // Constraints
        for (int i = 0; i < problem.Constraints.Count; i++)
        {
            var constraint = problem.Constraints[i];
            var terms = new List<string>();
            
            for (int j = 0; j < problem.VariableCount; j++)
            {
                var coeff = constraint.Coefficients[j];
                if (Math.Abs(coeff) > 1e-10)
                {
                    var sign = coeff >= 0 && terms.Count > 0 ? "+" : "";
                    terms.Add($"{sign}{coeff:F1}x{j + 1}");
                }
            }
            
            var constraintType = constraint.Type switch
            {
                ConstraintType.LessEqual => "≤",
                ConstraintType.GreaterEqual => "≥",
                ConstraintType.Equal => "=",
                _ => "="
            };
            
            lines.Add($"  {string.Join(" ", terms)} {constraintType} {constraint.RightHandSide:F1}");
        }
        
        lines.Add("");
        lines.Add("Variable restrictions:");
        var restrictions = new List<string>();
        for (int i = 0; i < problem.VariableCount; i++)
        {
            var restriction = problem.VariableTypes[i] switch
            {
                VariableType.NonNegative => "x" + (i + 1) + " ≥ 0",
                VariableType.NonPositive => "x" + (i + 1) + " ≤ 0", 
                VariableType.Unrestricted => "x" + (i + 1) + " urs",
                VariableType.Integer => "x" + (i + 1) + " ∈ Z+",
                VariableType.Binary => "x" + (i + 1) + " ∈ {0,1}",
                _ => "x" + (i + 1) + " ≥ 0"
            };
            restrictions.Add(restriction);
        }
        lines.Add($"  {string.Join(", ", restrictions)}");
        
        return string.Join("\n", lines);
    }
    
    private string FormatCanonicalForm(CanonicalForm canonical)
    {
        var lines = new List<string>();
        
        lines.Add("CANONICAL FORM:");
        lines.Add("(Standard form with slack variables and non-negativity constraints)");
        lines.Add("");
        
        // Summary
        lines.Add($"Original variables: {canonical.OriginalVariableCount}");
        lines.Add($"Total variables (after conversion): {canonical.TotalVariableCount}");
        lines.Add($"Slack variables added: {canonical.SlackVariableCount}");
        lines.Add($"Constraints: {canonical.ConstraintCount}");
        lines.Add("");
        
        return string.Join("\n", lines);
    }
    
    private string FormatObjectiveFunction(CanonicalForm canonical)
    {
        var objTerms = new List<string>();
        
        for (int j = 0; j < canonical.TotalVariableCount; j++)
        {
            var coeff = canonical.ObjectiveCoefficients[j];
            if (Math.Abs(coeff) > 1e-10)
            {
                var sign = coeff >= 0 && objTerms.Count > 0 ? "+" : "";
                var varName = canonical.GetVariableName(j);
                objTerms.Add($"{sign}{coeff:F3}{varName}");
            }
        }
        
        var problemType = canonical.IsMaximization ? "Maximize" : "Minimize";
        return $"{problemType} Z = {string.Join(" ", objTerms)}";
    }
    
    private void FormatConstraints(CanonicalForm canonical)
    {
        for (int i = 0; i < canonical.ConstraintCount; i++)
        {
            var terms = new List<string>();
            
            for (int j = 0; j < canonical.TotalVariableCount; j++)
            {
                var coeff = canonical.ConstraintMatrix[i, j];
                if (Math.Abs(coeff) > 1e-10)
                {
                    var sign = coeff >= 0 && terms.Count > 0 ? "+" : "";
                    var varName = canonical.GetVariableName(j);
                    terms.Add($"{sign}{coeff:F3}{varName}");
                }
            }
            
            var constraint = $"{string.Join(" ", terms)} = {canonical.RightHandSide[i]:F3}";
            Constraints.Add(constraint);
        }
        
        // Add non-negativity constraints
        var nonNegTerms = new List<string>();
        for (int j = 0; j < canonical.TotalVariableCount; j++)
        {
            nonNegTerms.Add(canonical.GetVariableName(j));
        }
        Constraints.Add($"{string.Join(", ", nonNegTerms)} ≥ 0");
    }
    
    private void FormatVariableMappings(CanonicalForm canonical)
    {
        foreach (var mapping in canonical.VariableMap)
        {
            var originalVar = $"x{mapping.OriginalIndex + 1}";
            var canonicalVars = mapping.CanonicalIndices.Select(i => canonical.GetVariableName(i));
            var typeDesc = mapping.OriginalType.ToString();
            
            VariableMappings.Add(new VariableMappingDisplay
            {
                OriginalVariable = originalVar,
                CanonicalVariables = string.Join(", ", canonicalVars),
                VariableType = typeDesc,
                Description = GetVariableDescription(mapping.OriginalType)
            });
        }
    }
    
    private string GetVariableDescription(VariableType type) => type switch
    {
        VariableType.NonNegative => "Non-negative (x ≥ 0)",
        VariableType.NonPositive => "Non-positive (x ≤ 0)", 
        VariableType.Unrestricted => "Unrestricted (split into x+ - x-)",
        VariableType.Integer => "Integer variable",
        VariableType.Binary => "Binary variable (0 or 1)",
        _ => "Unknown type"
    };
}

public class VariableMappingDisplay
{
    public string OriginalVariable { get; set; } = "";
    public string CanonicalVariables { get; set; } = "";
    public string VariableType { get; set; } = "";
    public string Description { get; set; } = "";
}