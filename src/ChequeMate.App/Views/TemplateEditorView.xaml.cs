using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ChequeMate.App.ViewModels;
using ChequeMate.Core.Enums;

namespace ChequeMate.App.Views;

public partial class TemplateEditorView : UserControl
{
    // Must match MmToPixelConverter (96 DPI screen).
    private const double PixelsPerMm = 96.0 / 25.4;

    // How close (in px) to an edge counts as a resize grab zone.
    private const double EdgeThreshold = 6.0;

    private const double MinSizeMm = 3.0;

    // Active interaction on an existing field.
    private bool _fieldActive;
    private bool _isResizing;
    private bool _grabLeft, _grabRight, _grabTop, _grabBottom;
    private TemplateFieldViewModel? _activeField;
    private Border? _activeBorder;
    private Point _lastCanvasPoint;

    // Rubber-band drawing of a new field.
    private bool _isDrawing;
    private Point _drawStart;

    public TemplateEditorView()
    {
        InitializeComponent();
    }

    private TemplateEditorViewModel? Vm => DataContext as TemplateEditorViewModel;

    // ---------- Existing field: select / drag / resize ----------

    private void Field_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border || border.DataContext is not TemplateFieldViewModel field)
            return;

        if (Vm != null)
            Vm.SelectedField = field;

        var local = e.GetPosition(border);
        UpdateGrabZones(border, local);
        _isResizing = _grabLeft || _grabRight || _grabTop || _grabBottom;

        _fieldActive = true;
        _activeField = field;
        _activeBorder = border;
        _lastCanvasPoint = e.GetPosition(ChequeCanvas);

        border.CaptureMouse();
        e.Handled = true; // don't let the canvas start drawing a new area
    }

    private void Field_MouseMove(object sender, MouseEventArgs e)
    {
        if (sender is not Border border || border.DataContext is not TemplateFieldViewModel field)
            return;

        // Not dragging: just reflect the correct cursor for the hovered zone.
        if (!_fieldActive)
        {
            var hover = e.GetPosition(border);
            UpdateGrabZones(border, hover);
            border.Cursor = CursorForZones();
            return;
        }

        if (e.LeftButton != MouseButtonState.Pressed || _activeField == null)
            return;

        var current = e.GetPosition(ChequeCanvas);
        double dxMm = (current.X - _lastCanvasPoint.X) / PixelsPerMm;
        double dyMm = (current.Y - _lastCanvasPoint.Y) / PixelsPerMm;

        if (_isResizing)
        {
            if (_grabRight)
                _activeField.WidthMm = Math.Max(MinSizeMm, _activeField.WidthMm + dxMm);

            if (_grabBottom)
                _activeField.HeightMm = Math.Max(MinSizeMm, _activeField.HeightMm + dyMm);

            if (_grabLeft)
            {
                double newWidth = _activeField.WidthMm - dxMm;
                if (newWidth >= MinSizeMm)
                {
                    _activeField.PositionXMm = Math.Max(0, _activeField.PositionXMm + dxMm);
                    _activeField.WidthMm = newWidth;
                }
            }

            if (_grabTop)
            {
                double newHeight = _activeField.HeightMm - dyMm;
                if (newHeight >= MinSizeMm)
                {
                    _activeField.PositionYMm = Math.Max(0, _activeField.PositionYMm + dyMm);
                    _activeField.HeightMm = newHeight;
                }
            }
        }
        else
        {
            _activeField.PositionXMm = Math.Max(0, _activeField.PositionXMm + dxMm);
            _activeField.PositionYMm = Math.Max(0, _activeField.PositionYMm + dyMm);
        }

        _lastCanvasPoint = current;
        e.Handled = true;
    }

    private void Field_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_fieldActive)
            return;

        _activeBorder?.ReleaseMouseCapture();
        _fieldActive = false;
        _isResizing = false;
        _activeField = null;
        _activeBorder = null;
        e.Handled = true;
    }

    private void UpdateGrabZones(Border border, Point local)
    {
        _grabLeft = local.X <= EdgeThreshold;
        _grabRight = local.X >= border.ActualWidth - EdgeThreshold;
        _grabTop = local.Y <= EdgeThreshold;
        _grabBottom = local.Y >= border.ActualHeight - EdgeThreshold;
    }

    private Cursor CursorForZones()
    {
        if ((_grabLeft && _grabTop) || (_grabRight && _grabBottom))
            return Cursors.SizeNWSE;
        if ((_grabRight && _grabTop) || (_grabLeft && _grabBottom))
            return Cursors.SizeNESW;
        if (_grabLeft || _grabRight)
            return Cursors.SizeWE;
        if (_grabTop || _grabBottom)
            return Cursors.SizeNS;
        return Cursors.SizeAll;
    }

    // ---------- Empty canvas: draw a new field area ----------

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDrawing = true;
        _drawStart = e.GetPosition(ChequeCanvas);

        Canvas.SetLeft(SelectionRectangle, _drawStart.X);
        Canvas.SetTop(SelectionRectangle, _drawStart.Y);
        SelectionRectangle.Width = 0;
        SelectionRectangle.Height = 0;
        SelectionRectangle.Visibility = Visibility.Visible;

        ChequeCanvas.CaptureMouse();
        e.Handled = true;
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDrawing)
            return;

        var current = e.GetPosition(ChequeCanvas);
        double left = Math.Min(_drawStart.X, current.X);
        double top = Math.Min(_drawStart.Y, current.Y);

        Canvas.SetLeft(SelectionRectangle, left);
        Canvas.SetTop(SelectionRectangle, top);
        SelectionRectangle.Width = Math.Abs(current.X - _drawStart.X);
        SelectionRectangle.Height = Math.Abs(current.Y - _drawStart.Y);
    }

    private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDrawing)
            return;

        _isDrawing = false;
        ChequeCanvas.ReleaseMouseCapture();
        SelectionRectangle.Visibility = Visibility.Collapsed;

        var current = e.GetPosition(ChequeCanvas);
        double left = Math.Min(_drawStart.X, current.X);
        double top = Math.Min(_drawStart.Y, current.Y);
        double width = Math.Abs(current.X - _drawStart.X);
        double height = Math.Abs(current.Y - _drawStart.Y);

        // A tiny drag is really just a click on empty space: deselect.
        if (width < EdgeThreshold || height < EdgeThreshold)
        {
            if (Vm != null)
                Vm.SelectedField = null;
            return;
        }

        if (Vm == null)
            return;

        var field = new TemplateFieldViewModel
        {
            FieldType = FieldType.Payee,
            PositionXMm = Math.Round(left / PixelsPerMm, 1),
            PositionYMm = Math.Round(top / PixelsPerMm, 1),
            WidthMm = Math.Round(width / PixelsPerMm, 1),
            HeightMm = Math.Round(height / PixelsPerMm, 1),
            FontFamily = "Arial",
            FontSizePt = 10
        };

        Vm.Fields.Add(field);
        Vm.SelectedField = field;
    }
}
