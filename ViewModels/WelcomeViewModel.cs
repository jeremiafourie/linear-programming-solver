using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace linear_programming_solver.ViewModels;

public partial class WelcomeViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;

    public WelcomeViewModel(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;
    }

    [RelayCommand]
    private async Task NewProblemAsync()
    {
        await _mainWindowViewModel.NewProblemAsync();
    }

    [RelayCommand]
    private async Task OpenProblemAsync()
    {
        await _mainWindowViewModel.OpenProblemAsync();
    }
}