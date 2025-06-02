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
using System.Windows.Controls.Primitives;

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
        private bool _isLoading = false;

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
                _isLoading = true;

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

                _isLoading = false;
                _hasUnsavedChanges = false;
            }
            catch (Exception ex)
            {
                _isLoading = false;
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

                var tabItem = CreateAssemblyTab(assembly);

                // Only mark as modified if this is a genuine user action
                if (!_isLoading && _assemblies.Count > 1)
                {
                    MarkAsModified();
                }

                return tabItem;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating assembly tab: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private TabItem CreateAssemblyTab(WallAssembly assembly)
        {
            try
            {
                // Create tab item
                var tabItem = new TabItem();
                tabItem.Header = assembly.Name;
                tabItem.Style = (Style)FindResource("ClosableTabItemStyle");
                tabItem.Tag = assembly;

                // Create tab content
                var content = CreateAssemblyContent(assembly);
                tabItem.Content = content;

                // Insert before the "+" tab
                int insertIndex = MainTabControl.Items.Count > 0 ? MainTabControl.Items.Count - 1 : 0;
                MainTabControl.Items.Insert(insertIndex, tabItem);

                // Select the new tab
                MainTabControl.SelectedItem = tabItem;

                _nextAssemblyNumber++;
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

        #region Enhanced UI Creation Methods

        private ScrollViewer CreateAssemblyContent(WallAssembly assembly)
        {
            var scrollViewer = new ScrollViewer();

            // Enhanced scrolling properties
            scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            scrollViewer.CanContentScroll = false;
            scrollViewer.PanningMode = PanningMode.Both;
            scrollViewer.IsManipulationEnabled = true;
            scrollViewer.PreviewMouseWheel += ScrollViewer_PreviewMouseWheel;
            scrollViewer.Background = Brushes.Transparent;

            var mainGrid = new Grid();
            mainGrid.Margin = new Thickness(20);

            // Define row definitions
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Assembly Header Card
            var headerCard = CreateAssemblyHeaderCard(assembly);
            Grid.SetRow(headerCard, 0);
            mainGrid.Children.Add(headerCard);

            // Layer Management Card
            var layerManagementCard = CreateLayerManagementCard(assembly);
            Grid.SetRow(layerManagementCard, 1);
            mainGrid.Children.Add(layerManagementCard);

            // Layers DataGrid Card
            var layersCard = CreateLayersDataGridCard(assembly);
            Grid.SetRow(layersCard, 2);
            mainGrid.Children.Add(layersCard);

            // Results Panel Card
            var resultsCard = CreateResultsCard(assembly);
            Grid.SetRow(resultsCard, 3);
            mainGrid.Children.Add(resultsCard);

            scrollViewer.Content = mainGrid;
            return scrollViewer;
        }

        private Border CreateAssemblyHeaderCard(WallAssembly assembly)
        {
            var card = new Border();
            card.Style = (Style)FindResource("CardPanel");
            card.Margin = new Thickness(0, 0, 0, 16);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });

            // Header Title
            var titleBlock = new TextBlock();
            titleBlock.Text = "Assembly Configuration";
            titleBlock.FontSize = 18;
            titleBlock.FontWeight = FontWeights.SemiBold;
            titleBlock.Foreground = (SolidColorBrush)FindResource("TextPrimary");
            titleBlock.Margin = new Thickness(0, 0, 0, 16);
            Grid.SetColumnSpan(titleBlock, 4);
            grid.Children.Add(titleBlock);

            // Create a sub-grid for the form elements
            var formGrid = new Grid();
            formGrid.Margin = new Thickness(0, 40, 0, 0); // Offset for title
            formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });

            // Assembly Name Label
            var nameLabel = new TextBlock();
            nameLabel.Text = "Assembly Name:";
            nameLabel.VerticalAlignment = VerticalAlignment.Center;
            nameLabel.Margin = new Thickness(0, 0, 12, 0);
            nameLabel.FontWeight = FontWeights.SemiBold;
            nameLabel.Foreground = (SolidColorBrush)FindResource("TextPrimary");
            Grid.SetColumn(nameLabel, 0);
            formGrid.Children.Add(nameLabel);

            // Assembly Name TextBox
            var nameTextBox = new TextBox();
            nameTextBox.Text = assembly.Name;
            nameTextBox.VerticalAlignment = VerticalAlignment.Center;
            nameTextBox.Style = (Style)FindResource("ModernTextBox");
            nameTextBox.Margin = new Thickness(0, 0, 20, 0);
            nameTextBox.DataContext = assembly;
            nameTextBox.SetBinding(TextBox.TextProperty, new Binding("Name")
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            nameTextBox.TextChanged += (s, e) => AssemblyNameChanged(assembly, nameTextBox.Text);
            Grid.SetColumn(nameTextBox, 1);
            formGrid.Children.Add(nameTextBox);

            // Type Label
            var typeLabel = new TextBlock();
            typeLabel.Text = "Type:";
            typeLabel.VerticalAlignment = VerticalAlignment.Center;
            typeLabel.Margin = new Thickness(0, 0, 12, 0);
            typeLabel.FontWeight = FontWeights.SemiBold;
            typeLabel.Foreground = (SolidColorBrush)FindResource("TextPrimary");
            Grid.SetColumn(typeLabel, 2);
            formGrid.Children.Add(typeLabel);

            // Type ComboBox
            var typeComboBox = new ComboBox();
            typeComboBox.VerticalAlignment = VerticalAlignment.Center;
            typeComboBox.Style = CreateModernComboBoxStyle();
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
            formGrid.Children.Add(typeComboBox);

            Grid.SetColumnSpan(formGrid, 4);
            grid.Children.Add(formGrid);

            card.Child = grid;
            return card;
        }

        private Border CreateLayerManagementCard(WallAssembly assembly)
        {
            var card = new Border();
            card.Style = (Style)FindResource("CardPanel");
            card.Margin = new Thickness(0, 0, 0, 16);

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Title
            var titleText = new TextBlock();
            titleText.Text = "Layer Management";
            titleText.FontSize = 18;
            titleText.FontWeight = FontWeights.SemiBold;
            titleText.Foreground = (SolidColorBrush)FindResource("TextPrimary");
            titleText.Margin = new Thickness(0, 0, 0, 8);
            Grid.SetRow(titleText, 0);
            grid.Children.Add(titleText);

            var subtitleText = new TextBlock();
            subtitleText.Text = "Assembly Layers (Outside to Inside)";
            subtitleText.FontSize = 13;
            subtitleText.Foreground = (SolidColorBrush)FindResource("TextSecondary");
            subtitleText.Margin = new Thickness(0, 30, 0, 16); // Changed from 0,0,0,16 to 0,30,0,16
            Grid.SetRow(subtitleText, 1); // Changed from row 0 to row 1
            grid.Children.Add(subtitleText);

            // Button Panel with enhanced styling
            var buttonPanel = new WrapPanel();
            buttonPanel.Orientation = Orientation.Horizontal;
            buttonPanel.Margin = new Thickness(0, 40, 0, 0);

            // Add Layer Button
            var addButton = new Button();
            addButton.Content = "Add Layer";
            addButton.Style = (Style)FindResource("SuccessButton");
            addButton.Width = 120;
            addButton.Tag = DateTime.Now;
            addButton.Click += (s, e) => AddLayer_Click_Safe(assembly, addButton);
            buttonPanel.Children.Add(addButton);

            // Remove Layer Button
            var removeButton = new Button();
            removeButton.Content = "Remove Layer";
            removeButton.Style = (Style)FindResource("ErrorButton");
            removeButton.Width = 120;
            removeButton.IsEnabled = false;
            removeButton.Tag = assembly;
            removeButton.Click += (s, e) => RemoveLayer_Click(assembly, removeButton);
            buttonPanel.Children.Add(removeButton);

            // Clear All Layers Button
            var clearAllButton = new Button();
            clearAllButton.Content = "Clear All";
            clearAllButton.Style = (Style)FindResource("WarningButton");
            clearAllButton.Width = 120;
            clearAllButton.Click += (s, e) => ClearAllLayers_Click(assembly);
            buttonPanel.Children.Add(clearAllButton);

            // Move Layer Up Button
            var moveUpButton = new Button();
            moveUpButton.Content = "Move ↑";
            moveUpButton.Style = (Style)FindResource("ModernButton");
            moveUpButton.Width = 100;
            moveUpButton.IsEnabled = false;
            moveUpButton.Click += (s, e) => MoveLayerUp_Click(assembly, moveUpButton);
            buttonPanel.Children.Add(moveUpButton);

            // Move Layer Down Button
            var moveDownButton = new Button();
            moveDownButton.Content = "Move ↓";
            moveDownButton.Style = (Style)FindResource("ModernButton");
            moveDownButton.Width = 100;
            moveDownButton.IsEnabled = false;
            moveDownButton.Click += (s, e) => MoveLayerDown_Click(assembly, moveDownButton);
            buttonPanel.Children.Add(moveDownButton);

            Grid.SetRow(buttonPanel, 1);
            grid.Children.Add(buttonPanel);

            card.Child = grid;
            return card;
        }

        private Border CreateLayersDataGridCard(WallAssembly assembly)
        {
            var card = new Border();
            card.Style = (Style)FindResource("CardPanel");
            card.Margin = new Thickness(0, 0, 0, 16);

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Title
            var titleText = new TextBlock();
            titleText.Text = "Assembly Layers";
            titleText.FontSize = 18;
            titleText.FontWeight = FontWeights.SemiBold;
            titleText.Foreground = (SolidColorBrush)FindResource("TextPrimary");
            titleText.Margin = new Thickness(0, 0, 0, 16);
            Grid.SetRow(titleText, 0);
            grid.Children.Add(titleText);

            // Enhanced DataGrid
            var dataGrid = CreateEnhancedDataGrid(assembly);
            Grid.SetRow(dataGrid, 1);
            grid.Children.Add(dataGrid);

            card.Child = grid;
            return card;
        }

        private DataGrid CreateEnhancedDataGrid(WallAssembly assembly)
        {
            var dataGrid = new DataGrid();
            dataGrid.AutoGenerateColumns = false;
            dataGrid.CanUserAddRows = false;
            dataGrid.CanUserDeleteRows = false;
            dataGrid.CanUserResizeColumns = true;
            dataGrid.CanUserReorderColumns = false;
            dataGrid.CanUserSortColumns = false;
            dataGrid.GridLinesVisibility = DataGridGridLinesVisibility.None;
            dataGrid.HeadersVisibility = DataGridHeadersVisibility.Column;
            dataGrid.MinHeight = 250;
            dataGrid.ItemsSource = assembly.Layers;

            // Enhanced styling
            dataGrid.Background = Brushes.White;
            dataGrid.RowBackground = Brushes.White;
            dataGrid.AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(248, 250, 252));
            dataGrid.SelectionMode = DataGridSelectionMode.Single;
            dataGrid.SelectionUnit = DataGridSelectionUnit.FullRow;
            dataGrid.BorderThickness = new Thickness(1);
            dataGrid.BorderBrush = (SolidColorBrush)FindResource("BorderLight");
            dataGrid.RowHeight = 40;

            // Enhanced column header style
            var headerStyle = new Style(typeof(DataGridColumnHeader));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, FindResource("PrimaryBlue")));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty, Brushes.White));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.SemiBold));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.PaddingProperty, new Thickness(12, 8, 12, 8)));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderThicknessProperty, new Thickness(0, 0, 1, 0)));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderBrushProperty, FindResource("PrimaryBlueDark")));
            dataGrid.ColumnHeaderStyle = headerStyle;

            // Layer Number Column
            var numberColumn = new DataGridTextColumn();
            numberColumn.Header = "#";
            numberColumn.Width = new DataGridLength(60, DataGridLengthUnitType.Pixel);
            numberColumn.IsReadOnly = true;
            numberColumn.Binding = new Binding("LayerNumber");

            var numberStyle = new Style(typeof(TextBlock));
            numberStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center));
            numberStyle.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.SemiBold));
            numberStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, FindResource("PrimaryBlue")));
            numberColumn.ElementStyle = numberStyle;

            dataGrid.Columns.Add(numberColumn);

            // Material Column
            // Material Column
            var materialColumn = new DataGridComboBoxColumn();
            materialColumn.Header = "Material";
            materialColumn.Width = new DataGridLength(400, DataGridLengthUnitType.Pixel); // Changed from 300 to 400
            materialColumn.MinWidth = 300; // Added minimum width
            materialColumn.DisplayMemberPath = "Name";
            materialColumn.SelectedValuePath = "Id";
            materialColumn.ItemsSource = _availableMaterials;
            materialColumn.SelectedValueBinding = new Binding("MaterialId")
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            dataGrid.Columns.Add(materialColumn);

            // Thickness Column
            // Thickness Column
            var thicknessColumn = new DataGridTextColumn();
            thicknessColumn.Header = "Thickness (mm)";
            thicknessColumn.Width = new DataGridLength(130, DataGridLengthUnitType.Pixel); // Slightly smaller to make room
            thicknessColumn.MinWidth = 100; 
            thicknessColumn.Binding = new Binding("Thickness")
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };

            var thicknessDisplayStyle = new Style(typeof(TextBlock));
            thicknessDisplayStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
            thicknessDisplayStyle.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(8, 0,8,0)));
            thicknessColumn.ElementStyle = thicknessDisplayStyle;

            var thicknessEditStyle = new Style(typeof(TextBox));
            thicknessEditStyle.Setters.Add(new Setter(TextBox.TextAlignmentProperty, TextAlignment.Right));
            thicknessEditStyle.Setters.Add(new Setter(TextBox.PaddingProperty, new Thickness(8, 4, 8, 4)));
            thicknessColumn.EditingElementStyle = thicknessEditStyle;

            dataGrid.Columns.Add(thicknessColumn);

            // R-Value Column
            // R-Value Column
            var rvalueColumn = new DataGridTextColumn();
            rvalueColumn.Header = "R-Value";
            rvalueColumn.Width = new DataGridLength(100, DataGridLengthUnitType.Pixel); // Slightly smaller
            rvalueColumn.MinWidth = 80;
            rvalueColumn.IsReadOnly = true;
            rvalueColumn.Binding = new Binding("RValue")
            {
                StringFormat = "0.###"
            };

            var rvalueStyle = new Style(typeof(TextBlock));
            rvalueStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
            rvalueStyle.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(8, 0, 8, 0)));
            rvalueStyle.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.SemiBold));
            rvalueStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, FindResource("Success")));
            rvalueColumn.ElementStyle = rvalueStyle;

            dataGrid.Columns.Add(rvalueColumn);

            // Description Column
            var descriptionColumn = new DataGridTextColumn();
            descriptionColumn.Header = "Description";
            descriptionColumn.Width = new DataGridLength(150, DataGridLengthUnitType.Star);
            descriptionColumn.MinWidth = 100;
            descriptionColumn.Binding = new Binding("Description")
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };

            var descriptionStyle = new Style(typeof(TextBlock));
            descriptionStyle.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(8, 0, 8, 0)));
            descriptionColumn.ElementStyle = descriptionStyle;

            dataGrid.Columns.Add(descriptionColumn);

            dataGrid.SelectionChanged += (s, e) => LayerGrid_SelectionChanged(assembly, dataGrid);

            return dataGrid;
        }

        private Border CreateResultsCard(WallAssembly assembly)
        {
            var card = new Border();
            card.Style = (Style)FindResource("CardPanel");

            // Create gradient brush correctly
            var gradientBrush = new LinearGradientBrush();
            gradientBrush.StartPoint = new Point(0, 0);
            gradientBrush.EndPoint = new Point(1, 1);
            gradientBrush.GradientStops.Add(new GradientStop(Color.FromRgb(227, 242, 253), 0.0)); // Light blue
            gradientBrush.GradientStops.Add(new GradientStop(Color.FromRgb(187, 222, 251), 1.0)); // Darker blue

            card.Background = gradientBrush;
            card.BorderBrush = (SolidColorBrush)FindResource("PrimaryBlue");
            card.BorderThickness = new Thickness(2);

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Title
            var titleText = new TextBlock();
            titleText.Text = "Thermal Performance Results";
            titleText.FontSize = 20;
            titleText.FontWeight = FontWeights.Bold;
            titleText.HorizontalAlignment = HorizontalAlignment.Center;
            titleText.Foreground = (SolidColorBrush)FindResource("PrimaryBlueDark");
            titleText.Margin = new Thickness(0, 0, 0, 20);
            Grid.SetRow(titleText, 0);
            mainGrid.Children.Add(titleText);

            // Results Grid
            var resultsGrid = new Grid();
            resultsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            resultsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            resultsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Total R-Value
            var rValuePanel = CreateResultPanel("Total R-Value", "TotalRValue", "0.### m²·K/W", assembly);
            Grid.SetColumn(rValuePanel, 0);
            resultsGrid.Children.Add(rValuePanel);

            // U-Factor
            var uFactorPanel = CreateResultPanel("U-Factor", "UFactor", "0.### W/(m²·K)", assembly);
            Grid.SetColumn(uFactorPanel, 1);
            resultsGrid.Children.Add(uFactorPanel);

            // Total Thickness
            var thicknessPanel = CreateResultPanel("Total Thickness", "TotalThickness", "0 mm", assembly);
            Grid.SetColumn(thicknessPanel, 2);
            resultsGrid.Children.Add(thicknessPanel);

            Grid.SetRow(resultsGrid, 1);
            mainGrid.Children.Add(resultsGrid);

            card.Child = mainGrid;
            return card;
        }

        private Border CreateResultPanel(string title, string propertyName, string format, WallAssembly assembly)
        {
            var panel = new Border();
            panel.Background = Brushes.White;
            panel.BorderBrush = (SolidColorBrush)FindResource("BorderLight");
            panel.BorderThickness = new Thickness(1);
            panel.CornerRadius = new CornerRadius(8);
            panel.Margin = new Thickness(8);
            panel.Padding = new Thickness(16);
            panel.Effect = new DropShadowEffect
            {
                BlurRadius = 4,
                ShadowDepth = 1,
                Color = Colors.Gray,
                Opacity = 0.3
            };

            var stackPanel = new StackPanel();

            var headerText = new TextBlock();
            headerText.Text = title;
            headerText.Style = (Style)FindResource("ResultHeaderStyle");
            stackPanel.Children.Add(headerText);

            var valueText = new TextBlock();
            valueText.Style = (Style)FindResource("ResultTextStyle");
            valueText.SetBinding(TextBlock.TextProperty, new Binding(propertyName)
            {
                Source = assembly,
                StringFormat = format
            });
            stackPanel.Children.Add(valueText);

            panel.Child = stackPanel;
            return panel;
        }

        private Style CreateModernComboBoxStyle()
        {
            var style = new Style(typeof(ComboBox));
            style.Setters.Add(new Setter(ComboBox.BackgroundProperty, FindResource("SurfaceWhite")));
            style.Setters.Add(new Setter(ComboBox.BorderBrushProperty, FindResource("BorderLight")));
            style.Setters.Add(new Setter(ComboBox.BorderThicknessProperty, new Thickness(1)));
            style.Setters.Add(new Setter(ComboBox.PaddingProperty, new Thickness(8, 6, 8, 6)));
            style.Setters.Add(new Setter(ComboBox.FontSizeProperty, 12.0));

            return (Style)FindResource("ModernComboBox");
        }

        #endregion

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta / 3.0);
                e.Handled = true;
            }
        }

        private void AddPlusTab()
        {
            var plusTab = new TabItem();
            plusTab.Header = "+";
            plusTab.Style = (Style)FindResource("AddTabStyle");
            plusTab.Content = new Grid();
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
                if (_isLoading)
                {
                    return;
                }

                if (MainTabControl.SelectedItem is TabItem selectedTab)
                {
                    if (selectedTab.Header.ToString() == "+")
                    {
                        var newAssemblyName = $"Wall Assembly {_nextAssemblyNumber}";
                        CreateNewAssemblyTab(newAssemblyName, "Wall");
                        MarkAsModified();
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
            try
            {
                // Don't close if it's the last assembly tab
                int assemblyTabCount = MainTabControl.Items.Cast<TabItem>().Count(tab => tab.Header.ToString() != "+");

                if (assemblyTabCount <= 1)
                {
                    MessageBox.Show("Cannot close the last assembly. Create a new assembly first.",
                        "Cannot Close", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Check for unsaved changes before closing
                if (_hasUnsavedChanges)
                {
                    var saveResult = MessageBox.Show(
                        $"You have unsaved changes in the project.\n\n" +
                        $"Do you want to save the project before closing assembly '{assembly.Name}'?",
                        "Unsaved Changes",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    switch (saveResult)
                    {
                        case MessageBoxResult.Yes:
                            if (!SaveCurrentProject())
                            {
                                return;
                            }
                            break;

                        case MessageBoxResult.Cancel:
                            return;

                        case MessageBoxResult.No:
                            break;
                    }
                }

                _isLoading = true;

                // Store current selection info before removal
                bool wasSelected = MainTabControl.SelectedItem == tabItem;
                int tabIndex = MainTabControl.Items.IndexOf(tabItem);

                // Remove from collections
                _assemblies.Remove(assembly);
                MainTabControl.Items.Remove(tabItem);

                // Select another tab if this was selected
                if (wasSelected && MainTabControl.Items.Count > 0)
                {
                    int newIndex = Math.Min(tabIndex, MainTabControl.Items.Count - 1);
                    if (newIndex == MainTabControl.Items.Count - 1 && MainTabControl.Items.Count > 1)
                    {
                        newIndex = MainTabControl.Items.Count - 2;
                    }
                    MainTabControl.SelectedIndex = newIndex;
                }

                UpdateAssemblyCount();
                UpdateStatus($"Assembly '{assembly.Name}' closed");

                _isLoading = false;
            }
            catch (Exception ex)
            {
                _isLoading = false;
                MessageBox.Show($"Error closing assembly: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AssemblyNameChanged(WallAssembly assembly, string newName)
        {
            try
            {
                if (!_isLoading)
                {
                    assembly.Name = newName;

                    var tab = MainTabControl.Items.Cast<TabItem>().FirstOrDefault(t => t.Tag == assembly);
                    if (tab != null)
                    {
                        tab.Header = newName;
                    }

                    MarkAsModified();
                }
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
                if (comboBox.SelectedItem is ComboBoxItem selectedItem && !_isLoading)
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

        private void AddLayer_Click_Safe(WallAssembly assembly, Button addButton)
        {
            try
            {
                // Prevent double-clicking
                if (addButton.Tag is DateTime lastClick)
                {
                    if ((DateTime.Now - lastClick).TotalMilliseconds < 500)
                    {
                        return;
                    }
                }
                addButton.Tag = DateTime.Now;

                addButton.IsEnabled = false;

                if (_availableMaterials.Count == 0)
                {
                    MessageBox.Show("No materials available. Please add materials first using Tools → Manage Materials.",
                        "No Materials", MessageBoxButton.OK, MessageBoxImage.Information);
                    addButton.IsEnabled = true;
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

                if (!_isLoading)
                {
                    MarkAsModified();
                }

                UpdateStatus($"Layer added - Total layers: {assembly.Layers.Count}");

                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(300);
                timer.Tick += (s, e) => {
                    addButton.IsEnabled = true;
                    timer.Stop();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                addButton.IsEnabled = true;
                MessageBox.Show($"Error adding layer: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearAllLayers_Click(WallAssembly assembly)
        {
            try
            {
                if (assembly.Layers.Count == 0)
                {
                    MessageBox.Show("No layers to clear.", "No Layers",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show(
                    $"Are you sure you want to remove all {assembly.Layers.Count} layers from '{assembly.Name}'?\n\nThis action cannot be undone.",
                    "Clear All Layers",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    assembly.Layers.Clear();
                    MarkAsModified();
                    UpdateStatus($"All layers cleared from '{assembly.Name}'");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error clearing layers: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MoveLayerUp_Click(WallAssembly assembly, Button moveUpButton)
        {
            try
            {
                var tabItem = MainTabControl.Items.Cast<TabItem>().FirstOrDefault(t => t.Tag == assembly);
                if (tabItem?.Content is ScrollViewer scrollViewer &&
                    scrollViewer.Content is Grid grid)
                {
                    var dataGrid = FindChild<DataGrid>(grid);
                    if (dataGrid?.SelectedItem is AssemblyLayer selectedLayer)
                    {
                        int currentIndex = assembly.Layers.IndexOf(selectedLayer);
                        if (currentIndex > 0)
                        {
                            assembly.Layers.Move(currentIndex, currentIndex - 1);
                            MarkAsModified();
                            UpdateStatus($"Layer moved up");
                            dataGrid.SelectedItem = selectedLayer;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Please select a layer to move.", "No Selection",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error moving layer: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MoveLayerDown_Click(WallAssembly assembly, Button moveDownButton)
        {
            try
            {
                var tabItem = MainTabControl.Items.Cast<TabItem>().FirstOrDefault(t => t.Tag == assembly);
                if (tabItem?.Content is ScrollViewer scrollViewer &&
                    scrollViewer.Content is Grid grid)
                {
                    var dataGrid = FindChild<DataGrid>(grid);
                    if (dataGrid?.SelectedItem is AssemblyLayer selectedLayer)
                    {
                        int currentIndex = assembly.Layers.IndexOf(selectedLayer);
                        if (currentIndex < assembly.Layers.Count - 1)
                        {
                            assembly.Layers.Move(currentIndex, currentIndex + 1);
                            MarkAsModified();
                            UpdateStatus($"Layer moved down");
                            dataGrid.SelectedItem = selectedLayer;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Please select a layer to move.", "No Selection",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error moving layer: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveLayer_Click(WallAssembly assembly, Button removeButton)
        {
            try
            {
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
                var tabItem = MainTabControl.Items.Cast<TabItem>().FirstOrDefault(t => t.Tag == assembly);
                if (tabItem?.Content is ScrollViewer scrollViewer &&
                    scrollViewer.Content is Grid grid)
                {
                    var removeButton = FindChild<Button>(grid, b => b.Content.ToString() == "Remove Layer");
                    var moveUpButton = FindChild<Button>(grid, b => b.Content.ToString() == "Move ↑");
                    var moveDownButton = FindChild<Button>(grid, b => b.Content.ToString() == "Move ↓");

                    bool hasSelection = dataGrid.SelectedItem != null;

                    if (removeButton != null)
                        removeButton.IsEnabled = hasSelection;

                    if (hasSelection && dataGrid.SelectedItem is AssemblyLayer selectedLayer)
                    {
                        int selectedIndex = assembly.Layers.IndexOf(selectedLayer);

                        if (moveUpButton != null)
                            moveUpButton.IsEnabled = selectedIndex > 0;

                        if (moveDownButton != null)
                            moveDownButton.IsEnabled = selectedIndex < assembly.Layers.Count - 1;
                    }
                    else
                    {
                        if (moveUpButton != null)
                            moveUpButton.IsEnabled = false;
                        if (moveDownButton != null)
                            moveDownButton.IsEnabled = false;
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
            if (!_isLoading)
            {
                MarkAsModified();
            }
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
                MarkAsModified();
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

                if (currentTab != null && currentAssembly != null && currentTab.Header.ToString() != "+")
                {
                    CloseAssemblyTab(currentTab, currentAssembly);
                }
                else
                {
                    MessageBox.Show("No assembly selected to close.", "No Selection",
                        MessageBoxButton.OK, MessageBoxImage.Information);
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
                    _isLoading = true;

                    MainTabControl.Items.Clear();
                    _assemblies.Clear();

                    _currentProjectFilePath = null;
                    _currentProjectName = null;
                    _nextAssemblyNumber = 1;

                    CreateNewAssemblyTab("Wall Assembly 1", "Wall");
                    AddPlusTab();

                    UpdateWindowTitle();
                    UpdateAssemblyCount();
                    UpdateStatus("New project created");

                    _isLoading = false;
                    _hasUnsavedChanges = false;
                }
            }
            catch (Exception ex)
            {
                _isLoading = false;
                MessageBox.Show($"Error creating new project: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProject(string filePath)
        {
            try
            {
                _isLoading = true;

                var project = ProjectService.LoadProject(filePath);

                MainTabControl.Items.Clear();
                _assemblies.Clear();

                _nextAssemblyNumber = 1;

                bool hasAssemblies = false;
                foreach (var assemblyData in project.Assemblies)
                {
                    var assembly = assemblyData.ToWallAssembly();
                    assembly.PropertyChanged += Assembly_PropertyChanged;
                    _assemblies.Add(assembly);

                    CreateAssemblyTab(assembly);
                    hasAssemblies = true;
                }

                if (!hasAssemblies)
                {
                    CreateNewAssemblyTab("Wall Assembly 1", "Wall");
                }

                AddPlusTab();

                _currentProjectFilePath = filePath;
                _currentProjectName = project.ProjectName;

                UpdateWindowTitle();
                UpdateAssemblyCount();
                UpdateStatus($"Project loaded: {_currentProjectName}");

                _isLoading = false;
                _hasUnsavedChanges = false;

                MessageBox.Show($"Project '{project.ProjectName}' loaded successfully!\n\n" +
                    $"Assemblies loaded: {project.Assemblies.Count}\n" +
                    $"Created: {project.CreatedDate:yyyy-MM-dd}\n" +
                    $"Last modified: {project.LastModified:yyyy-MM-dd HH:mm}",
                    "Project Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _isLoading = false;
                MessageBox.Show($"Error loading project: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private ProjectData CreateProjectData()
        {
            try
            {
                var project = new ProjectData
                {
                    ProjectName = !string.IsNullOrEmpty(_currentProjectName) ? _currentProjectName : "Untitled Project",
                    Description = "UFactor building thermal analysis project",
                    CreatedDate = DateTime.Now,
                    LastModified = DateTime.Now,
                    Version = "1.0"
                };

                if (_assemblies != null)
                {
                    foreach (var assembly in _assemblies)
                    {
                        var assemblyData = WallAssemblyData.FromWallAssembly(assembly);
                        project.Assemblies.Add(assemblyData);
                    }
                }

                return project;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating project data: {ex.Message}", "Data Creation Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private void OpenProjectInNewWindow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "Open UFactor Project in New Window",
                    Filter = ProjectService.GetProjectFilter(),
                    DefaultExt = ProjectService.GetDefaultExtension(),
                    InitialDirectory = ProjectService.GetDefaultProjectsFolder()
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    // Create and show new window
                    var newWindow = new MainWindow();
                    newWindow.LoadProjectDirectly(openFileDialog.FileName);
                    newWindow.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening project in new window: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadProjectDirectly(string filePath)
        {
            try
            {
                LoadProject(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading project: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "Open UFactor Project",
                    Filter = ProjectService.GetProjectFilter(),
                    DefaultExt = ProjectService.GetDefaultExtension(),
                    InitialDirectory = ProjectService.GetDefaultProjectsFolder()
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    // Ask user how they want to open the project
                    var choice = MessageBox.Show(
                        "How would you like to open this project?\n\n" +
                        "Yes = Replace current project\n" +
                        "No = Open in new window\n" +
                        "Cancel = Don't open",
                        "Open Project",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    switch (choice)
                    {
                        case MessageBoxResult.Yes:
                            // Replace current project (existing behavior)
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
                            LoadProject(openFileDialog.FileName);
                            break;

                        case MessageBoxResult.No:
                            // Open in new window
                            var newWindow = new MainWindow();
                            newWindow.LoadProjectDirectly(openFileDialog.FileName);
                            newWindow.Show();
                            break;

                        case MessageBoxResult.Cancel:
                            // Do nothing
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening project: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                    if (project == null)
                    {
                        return false;
                    }

                    ProjectService.SaveProject(project, _currentProjectFilePath);

                    ResetUnsavedState();
                    UpdateStatus($"Project saved: {_currentProjectName}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving project: {ex.Message}", "Save Error",
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
                    if (project == null)
                    {
                        return false;
                    }

                    project.ProjectName = System.IO.Path.GetFileNameWithoutExtension(saveFileDialog.FileName);

                    ProjectService.SaveProject(project, saveFileDialog.FileName);

                    _currentProjectFilePath = saveFileDialog.FileName;
                    _currentProjectName = project.ProjectName;
                    ResetUnsavedState();

                    UpdateWindowTitle();
                    UpdateStatus($"Project saved as: {_currentProjectName}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving project: {ex.Message}", "Save Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void SaveProject_Click(object sender, RoutedEventArgs e)
        {
           // MessageBox.Show("Save button clicked!", "Debug");
            try
            {
                if (_assemblies == null || _assemblies.Count == 0)
                {
                    MessageBox.Show("No assemblies to save. Please create at least one assembly before saving.",
                        "Nothing to Save", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

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
                if (_assemblies == null || _assemblies.Count == 0)
                {
                    MessageBox.Show("No assemblies to save. Please create at least one assembly before saving.",
                        "Nothing to Save", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

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
            if (!_isLoading && !_hasUnsavedChanges)
            {
                _hasUnsavedChanges = true;
                UpdateWindowTitle();
            }
        }

        private void ResetUnsavedState()
        {
            _hasUnsavedChanges = false;
            UpdateWindowTitle();
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

                LoadAvailableMaterials();
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