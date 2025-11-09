using Phyrexia.ScamDetectorWpf.ViewModels;

namespace Phyrexia.ScamDetectorWpf.Xaml;

public partial class MainWindow
{
    public MainWindow()
    {
        DataContext = new MainWindowViewModel();
        InitializeComponent();
    }
}