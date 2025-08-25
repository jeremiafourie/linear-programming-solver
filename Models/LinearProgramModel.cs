using CommunityToolkit.Mvvm.ComponentModel;

namespace linear_programming_solver.Models;

public partial class LinearProgramModel : ObservableObject
{
    [ObservableProperty]
    private string _fileName = "";

    [ObservableProperty]
    private string _fileContent = "";

    [ObservableProperty]
    private bool _isLoaded = false;
}