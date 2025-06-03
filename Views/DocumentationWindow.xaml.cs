using System;
using System.Windows;
using System.Windows.Input;

namespace UFactor
{
    /// <summary>
    /// Interaction logic for DocumentationWindow.xaml
    /// </summary>
    public partial class DocumentationWindow : Window
    {
        public DocumentationWindow()
        {
            InitializeComponent();
            SetupKeyboardShortcuts();
        }

        private void SetupKeyboardShortcuts()
        {
            // Allow ESC to close the window
            this.KeyDown += DocumentationWindow_KeyDown;
        }

        private void DocumentationWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
                e.Handled = true;
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Center the window relative to owner
            if (this.Owner != null)
            {
                this.Left = this.Owner.Left + (this.Owner.Width - this.Width) / 2;
                this.Top = this.Owner.Top + (this.Owner.Height - this.Height) / 2;
            }
        }
    }
}