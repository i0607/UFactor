using System.Windows;
using UFactor.Views;

namespace UFactor
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ManageMaterials_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var materialWindow = new MaterialManagementWindow();
                materialWindow.Owner = this;
                materialWindow.ShowDialog();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error opening Material Management: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}