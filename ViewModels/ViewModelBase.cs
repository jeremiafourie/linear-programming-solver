using CommunityToolkit.Mvvm.ComponentModel;

namespace linear_programming_solver.ViewModels;

public class ViewModelBase : ObservableObject
{
    private string _statusMessage = "Ready";
    
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
}