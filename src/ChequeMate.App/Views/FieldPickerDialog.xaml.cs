using System.Windows;
using System.Windows.Controls;
using ChequeMate.Core.Enums;

namespace ChequeMate.App.Views;

public partial class FieldPickerDialog : Window
{
    public FieldType? SelectedFieldType { get; private set; }

    public FieldPickerDialog()
    {
        InitializeComponent();
    }

    private void FieldButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag &&
            Enum.TryParse<FieldType>(tag, out var fieldType))
        {
            SelectedFieldType = fieldType;
            DialogResult = true;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
