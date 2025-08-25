using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using linear_programming_solver.ViewModels.Dialogs;

namespace linear_programming_solver.Views.Dialogs;

public partial class BaseDialog : Window
{
    public BaseDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}