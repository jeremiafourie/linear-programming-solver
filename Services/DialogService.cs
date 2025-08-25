using System.Threading.Tasks;
using Avalonia.Controls;
using linear_programming_solver.Views.Dialogs;
using linear_programming_solver.ViewModels.Dialogs;

namespace linear_programming_solver.Services;

public class DialogService
{
    public async Task<bool> ShowVariableSelectionDialogAsync(VariableSelectionDialogViewModel viewModel, Window owner)
    {
        var dialog = new VariableSelectionDialog
        {
            DataContext = viewModel
        };
        
        viewModel.OwnerWindow = dialog;
        var result = await dialog.ShowDialog<bool>(owner);
        return result;
    }

    public async Task<bool> ShowConstraintSelectionDialogAsync(ConstraintSelectionDialogViewModel viewModel, Window owner)
    {
        var dialog = new ConstraintSelectionDialog
        {
            DataContext = viewModel
        };
        
        viewModel.OwnerWindow = dialog;
        var result = await dialog.ShowDialog<bool>(owner);
        return result;
    }

    public async Task ShowSensitivityResultDialogAsync(SensitivityResultDialogViewModel viewModel, Window owner)
    {
        var dialog = new SensitivityResultDialog
        {
            DataContext = viewModel
        };
        
        viewModel.OwnerWindow = dialog;
        await dialog.ShowDialog(owner);
    }
}