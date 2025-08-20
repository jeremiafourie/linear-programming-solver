using linear_programming_solver.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace linear_programming_solver.ViewModels;

public partial class ProblemEditorViewModel : ViewModelBase
{
    [ObservableProperty]
    private LinearProgramModel _currentModel = new();

    public void LoadProblem(LinearProgramModel model)
    {
        CurrentModel = model;
        StatusMessage = $"Editing: {model.FileName}";
    }

    [RelayCommand]
    private void ValidateProblem()
    {
        StatusMessage = "Problem validation started...";
        // Add validation logic here
    }

    [RelayCommand]
    private void FormatContent()
    {
        StatusMessage = "Content formatted";
        // Add formatting logic here
    }

    [RelayCommand]
    private void ClearContent()
    {
        CurrentModel.FileContent = "";
        StatusMessage = "Content cleared";
    }
}