using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using linear_programming_solver.Models;
using linear_programming_solver.Services;

namespace linear_programming_solver.ViewModels;

public class AlgorithmSelectionConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is AlgorithmType selectedAlgorithm && parameter is string algorithmName)
        {
            bool isSelected = algorithmName.ToLower() switch
            {
                "primalsimplex" => selectedAlgorithm == AlgorithmType.PrimalSimplex,
                "revisedprimalsimplex" => selectedAlgorithm == AlgorithmType.RevisedPrimalSimplex,
                "branchandboundsimplex" => selectedAlgorithm == AlgorithmType.BranchAndBoundSimplex,
                "cuttingplane" => selectedAlgorithm == AlgorithmType.CuttingPlane,
                "branchandboundknapsack" => selectedAlgorithm == AlgorithmType.BranchAndBoundKnapsack,
                _ => false
            };
            
            return isSelected ? "● " : "○ ";
        }
        
        return "○ ";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToColorConverter : IValueConverter
{
    public static readonly BoolToColorConverter Instance = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Brushes.LightGreen : Brushes.LightCoral;
        }
        return Brushes.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToBorderConverter : IValueConverter
{
    public static readonly BoolToBorderConverter Instance = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Brushes.Green : Brushes.Red;
        }
        return Brushes.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StatusToColorConverter : IValueConverter
{
    public static readonly StatusToColorConverter Instance = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string status)
        {
            return status.ToLower() switch
            {
                "optimal" => Brushes.Green,
                "feasible" => Brushes.Blue,
                "infeasible" => Brushes.Red,
                "unbounded" => Brushes.Orange,
                _ => Brushes.Gray
            };
        }
        return Brushes.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToHighlightConverter : IValueConverter
{
    public static readonly BoolToHighlightConverter Instance = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && boolValue)
        {
            return new SolidColorBrush(Color.FromRgb(255, 255, 0)); // Yellow highlight
        }
        return new SolidColorBrush(Colors.Transparent);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToFontWeightConverter : IValueConverter
{
    public static readonly BoolToFontWeightConverter Instance = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && boolValue)
        {
            return FontWeight.Bold;
        }
        return FontWeight.Normal;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

