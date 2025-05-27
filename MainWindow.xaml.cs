using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using UFactor.Models;
using UFactor.Views;

namespace UFactor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _currentProjectPath;
        private bool _projectModified = false;
        private ObservableCollection<AssemblyLayer> _currentLayers;

        public MainWindow()
        {
            InitializeComponent();
            InitializeDataGrid();
            CreateNewProject();
        }

        #region Initialization

        private void InitializeDataGrid()
        {
            _currentLayers = new ObservableCollection<AssemblyLayer>();
            LayersDataGrid.ItemsSource = _currentLayers;
            
            // Set up the material combo box column
            var materialColumn = LayersDataGrid.Columns[1] as DataGridComboBoxColumn;
            if (materialColumn != null)
            {
                materialColumn.ItemsSource = App.MaterialDb.Materials;
            }
        }

        #endregion

        #region Menu Event Handlers

        private void NewProject_Click(object sender, RoutedEventArgs e)
        {
            if (_projectModified)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Do you want to save changes to the current project?",
                    "Save Changes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
                else if (result == MessageBoxResult.Yes)
                {
                    SaveProject_Click(sender, e);
                }
            }

            CreateNewProject();
        }

        private void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            if (_projectModified)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Do you want to save changes to the current project?",
                    "Save Changes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
                else if (result == MessageBoxResult.Yes)
                {
                    SaveProject_Click(sender, e);
                }
            }

            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "U-Factor Project Files (*.ufp)|*.ufp|All Files (*.*)|*.*",
                Title = "Open Project"
            };

            if (dialog.ShowDialog() == true)
            {
                OpenProject(dialog.FileName);
            }
        }

        private void SaveProject_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentProjectPath))
            {
                SaveProjectAs_Click(sender, e);
            }
            else
            {
                SaveProject(_currentProjectPath);
            }
        }

        private void SaveProjectAs_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "U-Factor Project Files (*.ufp)|*.ufp|All Files (*.*)|*.*",
                Title = "Save Project As"
            };

            if (dialog.ShowDialog() == true)
            {
                SaveProject(dialog.FileName);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ManageMaterials_Click(object sender, RoutedEventArgs e)
        {
            MaterialManagementWindow materialWindow = new MaterialManagementWindow();
            materialWindow.Owner = this;
            materialWindow.ShowDialog();
            
            // Refresh the material combo box after closing material management
            var materialColumn = LayersDataGrid.Columns[1] as DataGridComboBoxColumn;
            if (materialColumn != null)
            {
                materialColumn.ItemsSource = null;
                materialColumn.ItemsSource = App.MaterialDb.Materials;
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Settings functionality will be implemented in a future update.", 
                "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Documentation_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Documentation will be available in a future update.",
                "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("U-Factor Calculator\nVersion 1.0\n\nA tool for calculating thermal performance of building assemblies.",
                "About U-Factor Calculator", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Tab Management

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainTabControl.SelectedItem is TabItem selectedTab && selectedTab.Header.ToString() == "+")
            {
                // User clicked the "+" tab, add a new assembly
                AddNewAssemblyTab();
                
                // Select the newly added tab (second to last)
                MainTabControl.SelectedIndex = MainTabControl.Items.Count - 2;
            }
        }

        private void AddNewAssemblyTab()
        {
            // Create a name for the new assembly
            string assemblyName = "Assembly " + MainTabControl.Items.Count;
            
            // Create new tab (duplicate the structure of the first tab for now)
            TabItem newTab = new TabItem
            {
                Header = assemblyName
            };
            
            // For now, just add a placeholder - in a full implementation, 
            // you'd create the same structure as the first tab
            TextBlock placeholder = new TextBlock
            {
                Text = $"New {assemblyName} tab.\n\nThis would contain the same assembly editor as the first tab.",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 16
            };
            
            newTab.Content = placeholder;
            
            // Insert before the "+" tab
            MainTabControl.Items.Insert(MainTabControl.Items.Count - 1, newTab);
            
            _projectModified = true;
            UpdateTitle();
        }

        #endregion

        #region Layer Management

        private void AddLayerButton_Click(object sender, RoutedEventArgs e)
        {
            var newLayer = new AssemblyLayer
            {
                LayerNumber = _currentLayers.Count + 1,
                Thickness = 100, // Default 100mm
                Description = "New Layer"
            };
            
            _currentLayers.Add(newLayer);
            UpdateCalculations();
            _projectModified = true;
            UpdateTitle();
        }

        private void RemoveLayerButton_Click(object sender, RoutedEventArgs e)
        {
            if (LayersDataGrid.SelectedItem is AssemblyLayer selectedLayer)
            {
                _currentLayers.Remove(selectedLayer);
                
                // Renumber the layers
                for (int i = 0; i < _currentLayers.Count; i++)
                {
                    _currentLayers[i].LayerNumber = i + 1;
                }
                
                UpdateCalculations();
                _projectModified = true;
                UpdateTitle();
            }
        }

        private void LayersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RemoveLayerButton.IsEnabled = LayersDataGrid.SelectedItem != null;
        }

        #endregion

        #region Project Methods

        private void CreateNewProject()
        {
            // Clear existing data
            _currentLayers.Clear();
            AssemblyNameTextBox.Text = "Wall Assembly 1";
            AssemblyTypeComboBox.SelectedIndex = 0;
            
            // Add a default layer
            AddLayerButton_Click(null, null);
            
            // Reset project state
            _currentProjectPath = null;
            _projectModified = false;
            UpdateTitle();
            
            StatusText.Text = "New project created";
            UpdateCalculations();
        }

        private void OpenProject(string filePath)
        {
            try
            {
                // This is a simplified implementation
                // In a full version, you'd load the complete project structure
                StatusText.Text = $"Project opened: {Path.GetFileName(filePath)}";
                _currentProjectPath = filePath;
                _projectModified = false;
                UpdateTitle();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening project: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Error opening project";
            }
        }

        private void SaveProject(string filePath)
        {
            try
            {
                // Create a simple project structure
                var project = new
                {
                    Name = Path.GetFileNameWithoutExtension(filePath),
                    Created = File.Exists(filePath) ? File.GetCreationTime(filePath) : DateTime.Now,
                    LastModified = DateTime.Now,
                    Assembly = new
                    {
                        Name = AssemblyNameTextBox.Text,
                        Type = (AssemblyTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString(),
                        Layers = _currentLayers.Select(l => new
                        {
                            l.LayerNumber,
                            l.MaterialId,
                            l.Thickness,
                            l.Description
                        }).ToList()
                    }
                };
                
                // Serialize and save
                string json = JsonSerializer.Serialize(project, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);
                
                // Update state
                _currentProjectPath = filePath;
                _projectModified = false;
                UpdateTitle();
                
                StatusText.Text = $"Project saved: {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving project: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Error saving project";
            }
        }

        private void UpdateTitle()
        {
            string projectName = string.IsNullOrEmpty(_currentProjectPath) 
                ? "Untitled Project" 
                : Path.GetFileNameWithoutExtension(_currentProjectPath);
                
            Title = $"{projectName}{(_projectModified ? "*" : "")} - U-Factor Calculator";
        }

        #endregion

        #region Calculations

        public void UpdateCalculations()
        {
            double totalRValue = 0;
            double totalThickness = 0;

            foreach (var layer in _currentLayers)
            {
                if (layer.MaterialId != Guid.Empty)
                {
                    var material = App.MaterialDb.GetMaterial(layer.MaterialId);
                    if (material != null)
                    {
                        // Calculate R-value for this layer
                        double layerRValue = (layer.Thickness / 1000.0) / material.ThermalConductivity;
                        layer.RValue = layerRValue;
                        totalRValue += layerRValue;
                    }
                }
                totalThickness += layer.Thickness;
            }

            // Update display
            TotalRValueText.Text = $"{totalRValue:0.###} m²·K/W";
            UFactorText.Text = totalRValue > 0 ? $"{(1.0 / totalRValue):0.###} W/(m²·K)" : "∞ W/(m²·K)";
            TotalThicknessText.Text = $"{totalThickness:0} mm";

            // Refresh the data grid to show updated R-values
            LayersDataGrid.Items.Refresh();

            _projectModified = true;
            UpdateTitle();
        }

        #endregion

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_projectModified)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Do you want to save changes to the current project?",
                    "Save Changes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                }
                else if (result == MessageBoxResult.Yes)
                {
                    SaveProject_Click(null, null);
                }
            }
            
            base.OnClosing(e);
        }
    }

    // Simple class to represent assembly layers
    public class AssemblyLayer : INotifyPropertyChanged
    {
        private int _layerNumber;
        private Guid _materialId;
        private double _thickness;
        private string _description;
        private double _rValue;

        public int LayerNumber
        {
            get => _layerNumber;
            set { _layerNumber = value; OnPropertyChanged(); }
        }

        public Guid MaterialId
        {
            get => _materialId;
            set 
            { 
                _materialId = value; 
                OnPropertyChanged();
                // Trigger calculation update when material changes
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.UpdateCalculations();
                }
            }
        }

        public double Thickness
        {
            get => _thickness;
            set 
            { 
                _thickness = value; 
                OnPropertyChanged();
                // Trigger calculation update when thickness changes
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.UpdateCalculations();
                }
            }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public double RValue
        {
            get => _rValue;
            set { _rValue = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}