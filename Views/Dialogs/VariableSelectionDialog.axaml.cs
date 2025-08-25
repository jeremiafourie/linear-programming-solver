using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace linear_programming_solver.Views.Dialogs;

public partial class VariableSelectionDialog : Window
{
    public VariableSelectionDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}