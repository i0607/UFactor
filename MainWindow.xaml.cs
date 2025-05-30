using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Data;
using Microsoft.Win32;
using UFactor.Models;
using UFactor.Views;
using UFactor.Services;

namespace UFactor
{
    public partial class MainWindow : Window
    {
        private List<WallAssembly> _assemblies;
        private ObservableCollection<BuildingMaterials> _availableMaterials;

        // Project management
        private string _currentProjectFilePath;
        private bool _hasUnsavedChanges;
        private string _currentProjectName;

        // Multi-assembly management
        private int _nextAssemblyNumber = 1;

        public MainWindow()
        {
            InitializeComponent();
            InitializeApplication();
            SetupKeyboardShortcuts();
        }

        private void SetupKeyboardShortcuts()
        {
            // Add keyboard shortcuts
            var newProjectCommand = new RoutedCommand();
            var openProjectCommand = new RoutedCommand();
            var saveProjectCommand = new RoutedCommand();
            var addAssemblyCommand = new RoutedCommand();
            var closeAssemblyCommand = new RoutedCommand();

            newProjectCommand.InputGestures.Add(new KeyGesture(Key.N, ModifierKeys.Control));
            openProjectCommand.InputGestures.Add(new KeyGesture(Key.O, ModifierKeys.Control));
            saveProjectCommand.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control));
            addAssemblyCommand.InputGestures.Add(new KeyGesture(Key.T, ModifierKeys.Control));
            closeAssemblyCommand.InputGestures.Add(new KeyGesture(Key.W, ModifierKeys.Control));

            CommandBindings.Add(new CommandBinding(newProjectCommand, NewProject_Click));
            CommandBindings.Add(new CommandBinding(openProjectCommand, OpenProject_Click));
            CommandBindings.Add(new CommandBinding(saveProjectCommand, SaveProject_Click));
            CommandBindings.Add(new CommandBinding(addAssemblyCommand, AddAssembly_Click));
            CommandBindings.Add(new CommandBinding(closeAssemblyCommand, CloseCurrentAssembly_Click));
        }

        private void InitializeApplication()
        {
            try
            {
                // Initialize collections
                _assemblies = new List<WallAssembly>();
                _availableMaterials = new ObservableCollection<BuildingMaterials>();

                // Initialize project tracking
                _currentProjectFilePath = null;
                _currentProjectName = null;
                _hasUnsavedChanges = false;
                _nextAssemblyNumber = 1;

                // Load available materials
                LoadAvailableMaterials();

                // Create first assembly
                CreateNewAssemblyTab("Wall Assembly 1", "Wall");

                // Add the "+" tab
                AddPlusTab();

                // Update window title and status
                UpdateWindowTitle();
                UpdateAssemblyCount();
                UpdateStatus("Ready - Add materials to create your wall assembly");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing application: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAvailableMaterials()
        {
            try
            {
                _availableMaterials.Clear();
                if (App.MaterialDb?.Materials != null)
                {
                    foreach (var material in App.MaterialDb.Materials.OrderBy(m => m.Name))
                    {
                        _availableMaterials.Add(material);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading materials: {ex.Message}");
            }
        }

        private TabItem CreateNewAssemblyTab(string name, string type)
        {
            try
            {
                // Create new assembly
                var assembly = new WallAssembly(name, type);
                assembly.PropertyChanged += Assembly_PropertyChanged;
                _assemblies.Add(assembly);

                // Create tab item
                var tabItem = new TabItem();
                tabItem.Header = name;
                tabItem.Style = (Style)FindResource("ClosableTabItemStyle");
                tabItem.Tag = assembly; // Store reference to assembly

                // Create tab content
                var content = CreateAssemblyContent(assembly);
                tabItem.Content = content;

                // Insert before the "+" tab (always last)
                int insertIndex = MainTabControl.Items.Count > 0 ? MainTabControl.Items.Count - 1 : 0;
                MainTabControl.Items.Insert(insertIndex, tabItem);

                // Select the new tab
                MainTabControl.SelectedItem = tabItem;

                // Update counters
                _nextAssemblyNumber++;
                MarkAsModified();
                UpdateAssemblyCount();

                return tabItem;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating assembly tab: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private ScrollViewer CreateAssemblyContent(WallAssembly assembly)
        {
            var scrollViewer = new ScrollViewer();
            var mainGrid = new Grid();
            mainGrid.Margin = new Thickness(10);

            // Define row definitions
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Assembly Header
            var headerGrid = CreateAssemblyHeader(assembly);
            Grid.SetRow(headerGrid, 0);
            mainGrid.Children.Add(headerGrid);

            // Layer Management
            var layerManagementGrid = CreateLayerManagement(assembly);
            Grid.SetRow(layerManagementGrid, 1);
            mainGrid.Children.Add(layerManagementGrid);

            // Layers DataGrid
            var layersGrid = CreateLayersDataGrid(assembly);
            Grid.SetRow(layersGrid, 2);
            mainGrid.Children.Add(layersGrid);

            // Results Panel
            var resultsPanel = CreateResultsPanel(assembly);
            Grid.SetRow(resultsPanel, 3);
            mainGrid.Children.Add(resultsPanel);

            scrollViewer.Content = mainGrid;
            return scrollViewer;
        }

        private Grid CreateAssemblyHeader(WallAssembly assembly)
        {
            var grid = new Grid();
            grid.Margin = new Thickness(0, 0, 0, 20);

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

            // Assembly Name Label
            var nameLabel = new TextBlock();
            nameLabel.Text = "Assembly Name:";
            nameLabel.VerticalAlignment = VerticalAlignment.Center;
            nameLabel.Margin = new Thickness(0, 0, 10, 0);
            nameLabel.FontWeight = FontWeights.SemiBold;
            Grid.SetColumn(nameLabel, 0);
            grid.Children.Add(nameLabel);

            // Assembly Name TextBox
            var nameTextBox = new TextBox();
            nameTextBox.Text = assembly.Name;
            nameTextBox.VerticalAlignment = VerticalAlignment.Center;
            nameTextBox.DataContext = assembly;
            nameTextBox.SetBinding(TextBox.TextProperty, new Binding("Name")
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            nameTextBox.TextChanged += (s, e) => AssemblyNameChanged(assembly, nameTextBox.Text);
            Grid.SetColumn(nameTextBox, 1);
            grid.Children.Add(nameTextBox);

            // Type Label
            var typeLabel = new TextBlock();
            typeLabel.Text = "Type:";
            typeLabel.VerticalAlignment = VerticalAlignment.Center;
            typeLabel.Margin = new Thickness(20, 0, 10, 0);
            typeLabel.FontWeight = FontWeights.SemiBold;
            Grid.SetColumn(typeLabel, 2);
            grid.Children.Add(typeLabel);

            // Type ComboBox
            var typeComboBox = new ComboBox();
            typeComboBox.VerticalAlignment = VerticalAlignment.Center;
            typeComboBox.Items.Add(new ComboBoxItem { Content = "Wall" });
            typeComboBox.Items.Add(new ComboBoxItem { Content = "Roof" });
            typeComboBox.Items.Add(new ComboBoxItem { Content = "Floor" });

            // Set selected item based on assembly type
            foreach (ComboBoxItem item in typeComboBox.Items)
            {
                if (item.Content.ToString() == assembly.Type)
                {
                    typeComboBox.SelectedItem = item;
                    break;
                }
            }

            typeComboBox.SelectionChanged += (s, e) => AssemblyTypeChanged(assembly, typeComboBox);
            Grid.SetColumn(typeComboBox, 3);
            grid.Children.Add(typeComboBox);

            return grid;
        }

        private Grid CreateLayerManagement(WallAssembly assembly)
        {
            // Button Panel
            var buttonPanel = new StackPanel();
            buttonPanel.Orientation = Orientation.Horizontal;

            var addButton = new Button();
            addButton.Content = "Add Layer";
            addButton.Width = 100;
            addButton.Margin = new Thickness(5, 0);
            addButton.Background = Brushes.LightGreen;
            addButton.Click += (s, e) => AddLayer_Click(assembly);
            buttonPanel.Children.Add(addButton);

            var removeButton = new Button();
            removeButton.Content = "Remove Layer";
            removeButton.Width = 100;
            removeButton.Margin = new Thickness(5, 0);
            removeButton.Background = Brushes.LightCoral;
            removeButton.IsEnabled = false;
            removeButton.Tag = assembly; // Store assembly reference
            removeButton.Click += (s, e) => RemoveLayer_Click(assembly, removeButton);
            buttonPanel.Children.Add(removeButton);

            return grid;
        }

        private DataGrid CreateLayersDataGrid(WallAssembly assembly)
        {
            var dataGrid = new DataGrid();
            dataGrid.AutoGenerateColumns = false;
            dataGrid.CanUserAddRows = false;
            dataGrid.GridLinesVisibility = DataGridGridLinesVisibility.Horizontal;
            dataGrid.HeadersVisibility = DataGridHeadersVisibility.Column;
            dataGrid.Margin = new Thickness(0, 0, 0, 20);
            dataGrid.MinHeight = 200;
            dataGrid.ItemsSource = assembly.Layers;

            // Layer Number Column
            var numberColumn = new DataGridTextColumn();
            numberColumn.Header = "#";
            numberColumn.Width = new DataGridLength(40);
            numberColumn.IsReadOnly = true;
            numberColumn.Binding = new Binding("LayerNumber");
            dataGrid.Columns.Add(numberColumn);

            // Material Column
            var materialColumn = new DataGridComboBoxColumn();
            materialColumn.Header = "Material";
            materialColumn.Width = new DataGridLength(200);
            materialColumn.DisplayMemberPath = "Name";
            materialColumn.SelectedValuePath = "Id";
            materialColumn.ItemsSource = _availableMaterials;
            materialColumn.SelectedValueBinding = new Binding("MaterialId")
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            dataGrid.Columns.Add(materialColumn);

            // Thickness Column
            var thicknessColumn = new DataGridTextColumn();
            thicknessColumn.Header = "Thickness (mm)";
            thicknessColumn.Width = new DataGridLength(120);
            thicknessColumn.Binding = new Binding("Thickness")
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            dataGrid.Columns.Add(thicknessColumn);

            // R-Value Column
            var rvalueColumn = new DataGridTextColumn();
            rvalueColumn.Header = "R-Value";
            rvalueColumn.Width = new DataGridLength(100);
            rvalueColumn.IsReadOnly = true;
            rvalueColumn.Binding = new Binding("RValue")
            {
                StringFormat = "0.###"
            };
            dataGrid.Columns.Add(rvalueColumn);

            // Description Column
            var descriptionColumn = new DataGridTextColumn();
            descriptionColumn.Header = "Description";
            descriptionColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            descriptionColumn.Binding = new Binding("Description")
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            dataGrid.Columns.Add(descriptionColumn);

            // Handle selection changes
            dataGrid.SelectionChanged += (s, e) => LayerGrid_SelectionChanged(assembly, dataGrid);

            return dataGrid;
        }

        private Border CreateResultsPanel(WallAssembly assembly)
        {
            var border = new Border();
            border.BorderBrush = Brushes.DarkBlue;
            border.BorderThickness = new Thickness(2);
            border.Background = Brushes.LightBlue;
            border.Padding = new Thickness(15);
            border.CornerRadius = new CornerRadius(5);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Total R-Value
            var rValuePanel = new StackPanel();
            var rValueHeader = new TextBlock();
            rValueHeader.Text = "Total R-Value";
            rValueHeader.Style = (Style)FindResource("ResultHeaderStyle");
            rValuePanel.Children.Add(rValueHeader);

            var rValueText = new TextBlock();
            rValueText.Style = (Style)FindResource("ResultTextStyle");
            rValueText.SetBinding(TextBlock.TextProperty, new Binding("TotalRValue")
            {
                Source = assembly,
                StringFormat = "0.### m²·K/W"
            });
            rValuePanel.Children.Add(rValueText);
            Grid.SetColumn(rValuePanel, 0);
            grid.Children.Add(rValuePanel);

            // U-Factor
            var uFactorPanel = new StackPanel();
            var uFactorHeader = new TextBlock();
            uFactorHeader.Text = "U-Factor";
            uFactorHeader.Style = (Style)FindResource("ResultHeaderStyle");
            uFactorPanel.Children.Add(uFactorHeader);

            var uFactorText = new TextBlock();
            uFactorText.Style = (Style)FindResource("ResultTextStyle");
            uFactorText.SetBinding(TextBlock.TextProperty, new Binding("UFactor")
            {
                Source = assembly,
                StringFormat = "0.### W/(m²·K)"
            });
            uFactorPanel.Children.Add(uFactorText);
            Grid.SetColumn(uFactorPanel, 1);
            grid.Children.Add(uFactorPanel);

            // Total Thickness
            var thicknessPanel = new StackPanel();
            var thicknessHeader = new TextBlock();
            thicknessHeader.Text = "Total Thickness";
            thicknessHeader.Style = (Style)FindResource("ResultHeaderStyle");
            thicknessPanel.Children.Add(thicknessHeader);

            var thicknessText = new TextBlock();
            thicknessText.Style = (Style)FindResource("ResultTextStyle");
            thicknessText.SetBinding(TextBlock.TextProperty, new Binding("TotalThickness")
            {
                Source = assembly,
                StringFormat = "0 mm"
            });
            thicknessPanel.Children.Add(thicknessText);
            Grid.SetColumn(thicknessPanel, 2);
            grid.Children.Add(thicknessPanel);

            border.Child = grid;
            return border;
        }

        private void AddPlusTab()
        {
            var plusTab = new TabItem();
            plusTab.Header = "+";
            plusTab.Style = (Style)FindResource("AddTabStyle");
            plusTab.Content = new Grid(); // Empty content
            MainTabControl.Items.Add(plusTab);
        }

        private WallAssembly GetCurrentAssembly()
        {
            if (MainTabControl.SelectedItem is TabItem selectedTab && selectedTab.Tag is WallAssembly assembly)
            {
                return assembly;
            }
            return null;
        }

        private TabItem GetCurrentTab()
        {
            return MainTabControl.SelectedItem as TabItem;
        }

        #region Event Handlers

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (MainTabControl.SelectedItem is TabItem selectedTab)
                {
                    // Check if it's the "+" tab
                    if (selectedTab.Header.ToString() == "+")
                    {
                        // Create new assembly
                        var newAssemblyName = $"Wall Assembly {_nextAssemblyNumber}";
                        CreateNewAssemblyTab(newAssemblyName, "Wall");
                        return;
                    }

                    UpdateStatus($"Active: {selectedTab.Header}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in tab selection changed: {ex.Message}");
            }
        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button)
                {
                    // Find the TabItem that contains this button
                    var tabItem = FindParent<TabItem>(button);
                    if (tabItem != null && tabItem.Tag is WallAssembly assembly)
                    {
                        CloseAssemblyTab(tabItem, assembly);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error closing tab: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseAssemblyTab(TabItem tabItem, WallAssembly assembly)
        {
            // Don't close if it's the last assembly tab
            int assemblyTabCount = MainTabControl.Items.Cast<TabItem>().Count(tab => tab.Header.ToString() != "+");
            if (assemblyTabCount <= 1)
            {
                MessageBox.Show("Cannot close the last assembly. Create a new assembly first.",
                    "Cannot Close", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"Close assembly '{assembly.Name}'?",
                "Confirm Close", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Remove from collections
                _assemblies.Remove(assembly);
                MainTabControl.Items.Remove(tabItem);

                // Select another tab if this was selected
                if (MainTabControl.SelectedItem == null)
                {
                    MainTabControl.SelectedIndex = 0;
                }

                MarkAsModified();
                UpdateAssemblyCount();
                UpdateStatus($"Assembly '{assembly.Name}' closed");
            }
        }

        private void AssemblyNameChanged(WallAssembly assembly, string newName)
        {
            try
            {
                assembly.Name = newName;

                // Update tab header
                var tab = MainTabControl.Items.Cast<TabItem>().FirstOrDefault(t => t.Tag == assembly);
                if (tab != null)
                {
                    tab.Header = newName;
                }

                MarkAsModified();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating assembly name: {ex.Message}");
            }
        }

        private void AssemblyTypeChanged(WallAssembly assembly, ComboBox comboBox)
        {
            try
            {
                if (comboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    assembly.Type = selectedItem.Content.ToString();
                    MarkAsModified();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating assembly type: {ex.Message}");
            }
        }

        private void AddLayer_Click(WallAssembly assembly)
        {
            try
            {
                if (_availableMaterials.Count == 0)
                {
                    MessageBox.Show("No materials available. Please add materials first using Tools → Manage Materials.",
                        "No Materials", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var newLayer = new AssemblyLayer
                {
                    LayerNumber = assembly.Layers.Count + 1,
                    MaterialId = _availableMaterials.First().Id,
                    Thickness = 100,
                    Description = "New layer"
                };

                assembly.AddLayer(newLayer);
                MarkAsModified();
                UpdateStatus($"Layer added - Total layers: {assembly.Layers.Count}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding layer: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveLayer_Click(WallAssembly assembly, Button removeButton)
        {
            try
            {
                // Find the DataGrid for this assembly
                var tabItem = MainTabControl.Items.Cast<TabItem>().FirstOrDefault(t => t.Tag == assembly);
                if (tabItem?.Content is ScrollViewer scrollViewer &&
                    scrollViewer.Content is Grid grid)
                {
                    var dataGrid = FindChild<DataGrid>(grid);
                    if (dataGrid?.SelectedItem is AssemblyLayer selectedLayer)
                    {
                        var result = MessageBox.Show($"Remove layer {selectedLayer.LayerNumber} ({selectedLayer.MaterialName})?",
                            "Confirm Remove", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            assembly.RemoveLayer(selectedLayer);
                            MarkAsModified();
                            UpdateStatus($"Layer removed - Total layers: {assembly.Layers.Count}");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Please select a layer to remove.", "No Selection",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error removing layer: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LayerGrid_SelectionChanged(WallAssembly assembly, DataGrid dataGrid)
        {
            try
            {
                // Find the remove button for this assembly and enable/disable it
                var tabItem = MainTabControl.Items.Cast<TabItem>().FirstOrDefault(t => t.Tag == assembly);
                if (tabItem?.Content is ScrollViewer scrollViewer &&
                    scrollViewer.Content is Grid grid)
                {
                    var removeButton = FindChild<Button>(grid, b => b.Content.ToString() == "Remove Layer");
                    if (removeButton != null)
                    {
                        removeButton.IsEnabled = dataGrid.SelectedItem != null;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in layer selection changed: {ex.Message}");
            }
        }

        private void Assembly_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Mark project as modified for any assembly changes
            MarkAsModified();
        }

        #endregion

        #region Menu Events

        private void AddAssembly_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var newAssemblyName = $"Wall Assembly {_nextAssemblyNumber}";
                CreateNewAssemblyTab(newAssemblyName, "Wall");
                UpdateStatus($"New assembly '{newAssemblyName}' created");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding assembly: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseCurrentAssembly_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var currentTab = GetCurrentTab();
                var currentAssembly = GetCurrentAssembly();

                if (currentTab != null && currentAssembly != null)
                {
                    CloseAssemblyTab(currentTab, currentAssembly);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error closing assembly: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Utility Methods

        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            if (parent == null) return null;
            if (parent is T) return parent as T;
            return FindParent<T>(parent);
        }

        private T FindChild<T>(DependencyObject parent, Func<T, bool> predicate = null) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result && (predicate == null || predicate(result)))
                {
                    return result;
                }
                var childResult = FindChild<T>(child, predicate);
                if (childResult != null) return childResult;
            }
            return null;
        }

        private void UpdateAssemblyCount()
        {
            try
            {
                if (AssemblyCountText != null)
                {
                    int count = MainTabControl.Items.Cast<TabItem>().Count(tab => tab.Header.ToString() != "+");
                    AssemblyCountText.Text = $"Assemblies: {count}";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating assembly count: {ex.Message}");
            }
        }

        #endregion

        #region Project Management Methods

        private void NewProject_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_hasUnsavedChanges)
                {
                    var saveResult = MessageBox.Show("You have unsaved changes. Do you want to save before creating a new project?",
                        "Unsaved Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                    if (saveResult == MessageBoxResult.Yes)
                    {
                        if (!SaveCurrentProject())
                            return;
                    }
                    else if (saveResult == MessageBoxResult.Cancel)
                    {
                        return;
                    }
                }

                var result = MessageBox.Show("Create a new project? This will close all current assemblies.",
                    "New Project", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Clear all tabs except keep structure
                    MainTabControl.Items.Clear();
                    _assemblies.Clear();

                    // Reset project tracking
                    _currentProjectFilePath = null;
                    _currentProjectName = null;
                    _hasUnsavedChanges = false;
                    _nextAssemblyNumber = 1;

                    // Create new assembly and + tab
                    CreateNewAssemblyTab("Wall Assembly 1", "Wall");
                    AddPlusTab();

                    UpdateWindowTitle();
                    UpdateAssemblyCount();
                    UpdateStatus("New project created");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating new project: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProject(string filePath)
        {
            try
            {
                var project = ProjectService.LoadProject(filePath);

                // Clear current tabs and assemblies
                MainTabControl.Items.Clear();
                _assemblies.Clear();

                // Reset assembly counter
                _nextAssemblyNumber = 1;

                // Load assemblies from project
                bool hasAssemblies = false;
                foreach (var assemblyData in project.Assemblies)
                {
                    var assembly = assemblyData.ToWallAssembly();
                    assembly.PropertyChanged += Assembly_PropertyChanged;
                    _assemblies.Add(assembly);

                    // Create tab for each assembly
                    CreateNewAssemblyTab(assembly.Name, assembly.Type);
                    hasAssemblies = true;
                }

                // If no assemblies in project, create default one
                if (!hasAssemblies)
                {
                    CreateNewAssemblyTab("Wall Assembly 1", "Wall");
                }

                // Add the + tab
                AddPlusTab();

                // Update project tracking
                _currentProjectFilePath = filePath;
                _currentProjectName = project.ProjectName;
                _hasUnsavedChanges = false;

                // Update UI
                UpdateWindowTitle();
                UpdateAssemblyCount();
                UpdateStatus($"Project loaded: {_currentProjectName}");

                MessageBox.Show($"Project '{project.ProjectName}' loaded successfully!\n\n" +
                    $"Assemblies loaded: {project.Assemblies.Count}\n" +
                    $"Created: {project.CreatedDate:yyyy-MM-dd}\n" +
                    $"Last modified: {project.LastModified:yyyy-MM-dd HH:mm}",
                    "Project Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading project: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private ProjectData CreateProjectData()
        {
            var project = new ProjectData
            {
                ProjectName = !string.IsNullOrEmpty(_currentProjectName) ? _currentProjectName : "Untitled Project",
                Description = "UFactor building thermal analysis project",
                CreatedDate = DateTime.Now,
                LastModified = DateTime.Now,
                Version = "1.0"
            };

            // Convert assemblies to serializable format
            foreach (var assembly in _assemblies)
            {
                project.Assemblies.Add(WallAssemblyData.FromWallAssembly(assembly));
            }

            return project;
        }

        private bool SaveCurrentProject()
        {
            try
            {
                if (string.IsNullOrEmpty(_currentProjectFilePath))
                {
                    return SaveProjectAs();
                }
                else
                {
                    var project = CreateProjectData();
                    ProjectService.SaveProject(project, _currentProjectFilePath);

                    _hasUnsavedChanges = false;
                    UpdateWindowTitle();
                    UpdateStatus($"Project saved: {_currentProjectName}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving project: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private bool SaveProjectAs()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "Save UFactor Project As",
                    Filter = ProjectService.GetProjectFilter(),
                    DefaultExt = ProjectService.GetDefaultExtension(),
                    InitialDirectory = ProjectService.GetDefaultProjectsFolder(),
                    FileName = !string.IsNullOrEmpty(_currentProjectName) ? _currentProjectName : "New Project"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var project = CreateProjectData();
                    project.ProjectName = System.IO.Path.GetFileNameWithoutExtension(saveFileDialog.FileName);

                    ProjectService.SaveProject(project, saveFileDialog.FileName);

                    _currentProjectFilePath = saveFileDialog.FileName;
                    _currentProjectName = project.ProjectName;
                    _hasUnsavedChanges = false;

                    UpdateWindowTitle();
                    UpdateStatus($"Project saved as: {_currentProjectName}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving project: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_hasUnsavedChanges)
                {
                    var result = MessageBox.Show("You have unsaved changes. Do you want to save before opening a new project?",
                        "Unsaved Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        if (!SaveCurrentProject())
                            return;
                    }
                    else if (result == MessageBoxResult.Cancel)
                    {
                        return;
                    }
                }

                var openFileDialog = new OpenFileDialog
                {
                    Title = "Open UFactor Project",
                    Filter = ProjectService.GetProjectFilter(),
                    DefaultExt = ProjectService.GetDefaultExtension(),
                    InitialDirectory = ProjectService.GetDefaultProjectsFolder()
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    LoadProject(openFileDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening project: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveProject_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveCurrentProject();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving project: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveProjectAs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveProjectAs();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving project: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateWindowTitle()
        {
            try
            {
                string projectName = !string.IsNullOrEmpty(_currentProjectName) ? _currentProjectName : "Untitled Project";
                string unsavedIndicator = _hasUnsavedChanges ? "*" : "";
                this.Title = $"U-Factor Calculator - {projectName}{unsavedIndicator}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating window title: {ex.Message}");
            }
        }

        private void MarkAsModified()
        {
            if (!_hasUnsavedChanges)
            {
                _hasUnsavedChanges = true;
                UpdateWindowTitle();
            }
        }

        #endregion

        #region Other Menu Events

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exiting application: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show("Settings functionality will be implemented in a future version.",
                    "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening settings: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ManageMaterials_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var materialWindow = new MaterialManagementWindow();
                materialWindow.Owner = this;
                var result = materialWindow.ShowDialog();

                // Refresh materials after closing material management
                LoadAvailableMaterials();

                // Update material columns in all DataGrids
                UpdateAllMaterialColumns();

                UpdateStatus("Materials updated");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Material Management: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateAllMaterialColumns()
        {
            try
            {
                foreach (TabItem tab in MainTabControl.Items)
                {
                    if (tab.Header.ToString() != "+" && tab.Content is ScrollViewer scrollViewer &&
                        scrollViewer.Content is Grid grid)
                    {
                        var dataGrid = FindChild<DataGrid>(grid);
                        if (dataGrid != null && dataGrid.Columns.Count > 1 &&
                            dataGrid.Columns[1] is DataGridComboBoxColumn materialColumn)
                        {
                            materialColumn.ItemsSource = null;
                            materialColumn.ItemsSource = _availableMaterials;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating material columns: {ex.Message}");
            }
        }

        private void Documentation_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show("Documentation functionality will be implemented in a future version.",
                    "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening documentation: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show("U-Factor Calculator v1.0\n\n" +
                    "A professional building thermal performance analysis tool.\n\n" +
                    "Features:\n" +
                    "• Material database management\n" +
                    "• Multi-assembly thermal analysis\n" +
                    "• R-value and U-factor calculations\n" +
                    "• Complete project management\n" +
                    "• Professional tabbed interface\n" +
                    "• Building code compliance checking\n\n" +
                    "Built with WPF and .NET 8\n\n" +
                    "Keyboard Shortcuts:\n" +
                    "Ctrl+N - New Project\n" +
                    "Ctrl+O - Open Project\n" +
                    "Ctrl+S - Save Project\n" +
                    "Ctrl+T - Add Assembly\n" +
                    "Ctrl+W - Close Assembly",
                    "About U-Factor Calculator", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error showing about dialog: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatus(string message)
        {
            try
            {
                if (StatusText != null)
                {
                    StatusText.Text = message;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating status: {ex.Message}");
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (_hasUnsavedChanges)
                {
                    var result = MessageBox.Show("You have unsaved changes. Do you want to save before closing?",
                        "Unsaved Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        if (!SaveCurrentProject())
                        {
                            e.Cancel = true;
                            return;
                        }
                    }
                    else if (result == MessageBoxResult.Cancel)
                    {
                        e.Cancel = true;
                        return;
                    }
                }

                if (App.MaterialDb != null)
                {
                    App.MaterialDb.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving on close: {ex.Message}");
            }
            finally
            {
                if (!e.Cancel)
                {
                    base.OnClosing(e);
                }
            }
        }

        #endregion
    }
}