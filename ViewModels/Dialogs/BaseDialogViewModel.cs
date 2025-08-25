using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Controls;

namespace linear_programming_solver.ViewModels.Dialogs;

public partial class BaseDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _dialogTitle = "Dialog";

    [ObservableProperty]
    private object? _dialogContent;

    [ObservableProperty]
    private bool _dialogResult = false;

    public Window? OwnerWindow { get; set; }

    [RelayCommand]
    private void Ok()
    {
        if (ValidateInput())
        {
            DialogResult = true;
            OwnerWindow?.Close(true);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        DialogResult = false;
        OwnerWindow?.Close(false);
    }

    protected virtual bool ValidateInput()
    {
        return true;
    }
}