using System.Windows;
using System.Windows.Controls;
using ChequeMate.App.ViewModels;

namespace ChequeMate.App.Views;

public partial class PrintQueueView : UserControl
{
    public PrintQueueView()
    {
        InitializeComponent();
        DataContextChanged += (_, e) =>
        {
            if (e.NewValue is PrintQueueViewModel viewModel)
            {
                viewModel.ConfirmAction = ConfirmAction;
                viewModel.ShowErrorAction = ShowError;
            }
        };
    }

    private bool ConfirmAction(string message) =>
        MessageBox.Show(
            Window.GetWindow(this),
            message,
            "ChequeMate",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes;

    private void ShowError(string message) =>
        MessageBox.Show(
            Window.GetWindow(this),
            message,
            "ChequeMate - Print failed",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
}
