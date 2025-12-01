using System.Windows;
using iRacingOverlay.ViewModels;

namespace iRacingOverlay.Views;

public partial class OverlayWindow : Window
{
    public OverlayWindow(OverlayViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Make window draggable
        MouseLeftButtonDown += (s, e) => DragMove();
    }
}

