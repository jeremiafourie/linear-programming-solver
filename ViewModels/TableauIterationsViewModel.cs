using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace linear_programming_solver.ViewModels;

public partial class TableauIterationsViewModel : ViewModelBase
{
    [ObservableProperty]
    private int _currentIteration = 0;

    [ObservableProperty]
    private int _totalIterations = 0;

    [ObservableProperty]
    private string _tableauDisplay = "";

    [ObservableProperty]
    private string _pivotInfo = "";

    public ObservableCollection<string> Iterations { get; } = new();

    public TableauIterationsViewModel()
    {
        LoadSampleIterations();
    }

    public void LoadIterations(/* List<TableauIteration> iterations */)
    {
        StatusMessage = "Loading tableau iterations...";
        Iterations.Clear();
        
        // Example data
        Iterations.Add("Iteration 0: Initial tableau");
        Iterations.Add("Iteration 1: After pivot on (1,2)");
        Iterations.Add("Iteration 2: Optimal solution reached");
        
        TotalIterations = Iterations.Count;
        CurrentIteration = 0;
        ShowIteration(0);
    }

    private void LoadSampleIterations()
    {
        Iterations.Add("Initial Tableau");
        Iterations.Add("Iteration 1");
        Iterations.Add("Final Tableau");
        TotalIterations = Iterations.Count;
        ShowIteration(0);
    }

    [RelayCommand]
    private void PreviousIteration()
    {
        if (CurrentIteration > 0)
        {
            CurrentIteration--;
            ShowIteration(CurrentIteration);
        }
    }

    [RelayCommand]
    private void NextIteration()
    {
        if (CurrentIteration < TotalIterations - 1)
        {
            CurrentIteration++;
            ShowIteration(CurrentIteration);
        }
    }

    [RelayCommand]
    private void JumpToFinal()
    {
        if (TotalIterations > 0)
        {
            CurrentIteration = TotalIterations - 1;
            ShowIteration(CurrentIteration);
        }
    }

    [RelayCommand]
    private void JumpToFirst()
    {
        CurrentIteration = 0;
        ShowIteration(CurrentIteration);
    }

    private void ShowIteration(int iteration)
    {
        if (iteration < 0 || iteration >= TotalIterations) return;

        // Sample tableau display based on iteration
        TableauDisplay = iteration switch
        {
            0 => "   Basis │  x1    x2    x3    s1    s2  │  RHS   \n" +
                 "   ─────────────────────────────────────────────── \n" +
                 "    s1   │ 1.000 2.000 1.000 1.000 0.000│ 8.000  \n" +
                 "    s2   │ 2.000 1.000 0.000 0.000 1.000│10.000  \n" +
                 "   ─────────────────────────────────────────────── \n" +
                 "     Z   │-2.000-3.000-1.000 0.000 0.000│ 0.000  ",
            
            1 => "   Basis │  x1    x2    x3    s1    s2  │  RHS   \n" +
                 "   ─────────────────────────────────────────────── \n" +
                 "    x2   │ 0.500 1.000 0.500 0.500 0.000│ 4.000  \n" +
                 "    s2   │ 1.500 0.000-0.500-0.500 1.000│ 6.000  \n" +
                 "   ─────────────────────────────────────────────── \n" +
                 "     Z   │-0.500 0.000 0.500 1.500 0.000│12.000  ",
            
            2 => "   Basis │  x1    x2    x3    s1    s2  │  RHS   \n" +
                 "   ─────────────────────────────────────────────── \n" +
                 "    x2   │ 0.000 1.000 2.000 1.000-1.000│ 2.000  \n" +
                 "    x1   │ 1.000 0.000-1.000-1.000 2.000│ 4.000  \n" +
                 "   ─────────────────────────────────────────────── \n" +
                 "     Z   │ 0.000 0.000 0.000 1.000 1.000│14.000  ",
            
            _ => "No tableau data available"
        };

        PivotInfo = iteration switch
        {
            0 => "Entering Variable: x2, Leaving Variable: s1, Pivot Element: (1,2) = 2.000",
            1 => "Entering Variable: x1, Leaving Variable: s2, Pivot Element: (2,1) = 1.500",
            2 => "Optimal solution reached",
            _ => ""
        };

        StatusMessage = $"Viewing iteration {iteration + 1} of {TotalIterations}";
    }

    public bool CanGoPrevious => CurrentIteration > 0;
    public bool CanGoNext => CurrentIteration < TotalIterations - 1;
}