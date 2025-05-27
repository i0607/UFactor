using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using UFactor.Models;

namespace UFactor.Views
{
    /// <summary>
    /// Interaction logic for MaterialManagementWindow.xaml
    /// </summary>
    public partial class MaterialManagementWindow : Window
    {
        private BuildingMaterials _currentMaterial;
        private bool _isNewMaterial = false;
        private bool _isLoading = false;
        private bool _hasChanges = false;
        private Dictionary<string, Control> _validationControls = new Dictionary<string, Control>();
        private void ClearValidationErrors()
        {
            ValidationErrorPanel.Visibility = Visibility.Collapsed;

            foreach (var control in _validationControls.Values)
            {
                // Use SystemColors.ControlBorder or a default brush instead of trying to find a resource
                control.BorderBrush = SystemColors.ControlDarkBrush;
                control.ToolTip = null;
            }
        }
        public MaterialManagementWindow()
        {
            InitializeComponent();

            // Setup validation controls dictionary
            _validationControls["Name"] = NameTextBox;
            _validationControls["Category"] = CategoryComboBox;
            _validationControls["ThermalConductivity"] = ConductivityTextBox;
            _validationControls["Density"] = DensityTextBox;
            _validationControls["SpecificHeat"] = SpecificHeatTextBox;
            _validationControls["VaporResistance"] = VaporResistanceTextBox;

            // Load materials and categories
            LoadMaterialsAndCategories();
        }

        #region UI Event Handlers

        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateMaterialsList();
        }

        private void MaterialList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_hasChanges)
            {
                // Prompt to save changes
                MessageBoxResult result = MessageBox.Show(
                    "You have unsaved changes. Do you want to save them?",
                    "Save Changes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Cancel)
                {
                    // Revert selection change
                    _isLoading = true;
                    MaterialList.SelectedItem = _currentMaterial;
                    _isLoading = false;
                    return;
                }
                else if (result == MessageBoxResult.Yes)
                {
                    // Save changes
                    if (!SaveMaterial())
                    {
                        // If save failed, revert selection change
                        _isLoading = true;
                        MaterialList.SelectedItem = _currentMaterial;
                        _isLoading = false;
                        return;
                    }
                }
            }

            if (MaterialList.SelectedItem is BuildingMaterials selectedMaterial)
            {
                // Load the selected material
                LoadMaterial(selectedMaterial);
                DeleteButton.IsEnabled = true;
            }
            else
            {
                DeleteButton.IsEnabled = false;
                _currentMaterial = null;
                ClearForm();
            }

            _hasChanges = false;
            UpdateSaveButtonState();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // Create a new material
            _currentMaterial = new BuildingMaterials();
            _isNewMaterial = true;
            _hasChanges = false;

            // Clear the form
            ClearForm();

            // Clear material selection
            _isLoading = true;
            MaterialList.SelectedItem = null;
            _isLoading = false;

            // Update UI
            DeleteButton.IsEnabled = false;
            UpdateSaveButtonState();

            // Select the name field
            NameTextBox.Focus();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (MaterialList.SelectedItem is BuildingMaterials selectedMaterial)
            {
                MessageBoxResult result = MessageBox.Show(
                    $"Are you sure you want to delete the material '{selectedMaterial.Name}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // Remove the material
                    App.MaterialDb.RemoveMaterial(selectedMaterial.Id);
                    App.MaterialDb.SaveChanges();

                    // Update the UI
                    LoadMaterialsAndCategories();
                    ClearForm();

                    _currentMaterial = null;
                    _hasChanges = false;
                    UpdateSaveButtonState();
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_hasChanges)
            {
                MessageBoxResult result = MessageBox.Show(
                    "You have unsaved changes. Are you sure you want to discard them?",
                    "Discard Changes",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            if (_currentMaterial != null && _isNewMaterial)
            {
                // Discard the new material
                _currentMaterial = null;
                _isNewMaterial = false;

                // Clear selection
                MaterialList.SelectedItem = null;
            }
            else if (_currentMaterial != null)
            {
                // Reload the current material from database
                var material = App.MaterialDb.GetMaterial(_currentMaterial.Id);
                if (material != null)
                {
                    LoadMaterial(material);
                }
            }

            _hasChanges = false;
            UpdateSaveButtonState();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveMaterial();
        }

        private void PropertyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isLoading)
            {
                _hasChanges = true;
                UpdateSaveButtonState();
            }
        }

        private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoading)
            {
                _hasChanges = true;
                UpdateSaveButtonState();
            }
        }

        private void NumericTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isLoading)
            {
                TextBox textBox = sender as TextBox;

                // Validate numeric input
                if (textBox != null && !string.IsNullOrEmpty(textBox.Text))
                {
                    if (textBox == ConductivityTextBox && double.TryParse(textBox.Text, out double conductivity))
                    {
                        // Update R-value
                        double rValue = conductivity > 0 ? 0.001 / conductivity : 0;
                        RValueTextBox.Text = rValue.ToString("0.####");
                    }
                }

                _hasChanges = true;
                UpdateSaveButtonState();
            }
        }

        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Only allow digits, decimal point, and minus sign
            Regex regex = new Regex("[^0-9.-]+");
            e.Handled = regex.IsMatch(e.Text);

            // Only allow one decimal point
            if (e.Text == "." && sender is TextBox textBox && textBox.Text.Contains("."))
            {
                e.Handled = true;
            }

            // Only allow minus at the beginning
            if (e.Text == "-" && sender is TextBox tb && (tb.Text.Contains("-") || tb.CaretIndex > 0))
            {
                e.Handled = true;
            }
        }

        #endregion

        #region Helper Methods

        private void LoadMaterialsAndCategories()
        {
            _isLoading = true;

            // Remember the selected category
            string selectedCategory = CategoryFilter.SelectedItem is ComboBoxItem comboBoxItem
                ? comboBoxItem.Content.ToString()
                : "All Categories";

            // Populate category filter
            CategoryFilter.Items.Clear();
            CategoryFilter.Items.Add(new ComboBoxItem { Content = "All Categories" });

            foreach (var category in App.MaterialDb.Categories)
            {
                CategoryFilter.Items.Add(new ComboBoxItem { Content = category });
            }

            // Select the previously selected category or default to "All Categories"
            for (int i = 0; i < CategoryFilter.Items.Count; i++)
            {
                if (CategoryFilter.Items[i] is ComboBoxItem item && item.Content.ToString() == selectedCategory)
                {
                    CategoryFilter.SelectedIndex = i;
                    break;
                }
            }

            if (CategoryFilter.SelectedIndex < 0)
            {
                CategoryFilter.SelectedIndex = 0;
            }

            // Populate category combo box
            CategoryComboBox.Items.Clear();
            foreach (var category in App.MaterialDb.Categories)
            {
                CategoryComboBox.Items.Add(category);
            }

            // Update the materials list
            UpdateMaterialsList();

            _isLoading = false;
        }

        private void UpdateMaterialsList()
        {
            if (_isLoading)
            {
                return;
            }

            // Get the selected category
            string selectedCategory = "All Categories";
            if (CategoryFilter.SelectedItem is ComboBoxItem item)
            {
                selectedCategory = item.Content.ToString();
            }

            // Remember the selected material
            BuildingMaterials selectedMaterial = MaterialList.SelectedItem as BuildingMaterials;

            // Filter materials by category
            MaterialList.Items.Clear();
            IEnumerable<BuildingMaterials> materials = App.MaterialDb.Materials;

            if (selectedCategory != "All Categories")
            {
                materials = materials.Where(m => m.Category == selectedCategory);
            }

            // Add materials to the list
            foreach (var material in materials.OrderBy(m => m.Name))
            {
                MaterialList.Items.Add(material);
            }

            // Re-select the previously selected material if it's still in the list
            if (selectedMaterial != null)
            {
                for (int i = 0; i < MaterialList.Items.Count; i++)
                {
                    if (MaterialList.Items[i] is BuildingMaterials m && m.Id == selectedMaterial.Id)
                    {
                        MaterialList.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void LoadMaterial(BuildingMaterials material)
        {
            _currentMaterial = material;
            _isNewMaterial = false;
            _isLoading = true;

            // Populate the form
            NameTextBox.Text = material.Name;
            CategoryComboBox.Text = material.Category;
            ConductivityTextBox.Text = material.ThermalConductivity.ToString("0.####");
            RValueTextBox.Text = material.RValuePerMm.ToString("0.####");
            DensityTextBox.Text = material.Density.ToString("0.##");
            SpecificHeatTextBox.Text = material.SpecificHeat.ToString("0.##");
            VaporResistanceTextBox.Text = material.VaporResistance.ToString("0.##");
            DescriptionTextBox.Text = material.Description;

            // Clear validation errors
            ClearValidationErrors();

            _isLoading = false;
        }

        private void ClearForm()
        {
            _isLoading = true;

            NameTextBox.Text = string.Empty;
            CategoryComboBox.Text = string.Empty;
            ConductivityTextBox.Text = string.Empty;
            RValueTextBox.Text = string.Empty;
            DensityTextBox.Text = string.Empty;
            SpecificHeatTextBox.Text = string.Empty;
            VaporResistanceTextBox.Text = string.Empty;
            DescriptionTextBox.Text = string.Empty;

            // Clear validation errors
            ClearValidationErrors();

            _isLoading = false;
        }

        private void UpdateSaveButtonState()
        {
            SaveButton.IsEnabled = _hasChanges && ValidateInputs(false);
        }

        private bool ValidateInputs(bool showErrors = true)
        {
            // Clear previous validation errors
            ClearValidationErrors();

            List<string> errors = new List<string>();

            // Validate Name
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                errors.Add("Material name is required.");
                if (showErrors) MarkInvalid("Name");
            }

            // Validate Category
            if (string.IsNullOrWhiteSpace(CategoryComboBox.Text))
            {
                errors.Add("Category is required.");
                if (showErrors) MarkInvalid("Category");
            }

            // Validate Thermal Conductivity
            if (!double.TryParse(ConductivityTextBox.Text, out double conductivity) || conductivity <= 0)
            {
                errors.Add("Thermal conductivity must be a positive number.");
                if (showErrors) MarkInvalid("ThermalConductivity");
            }

            // Validate Density
            if (!double.TryParse(DensityTextBox.Text, out double density) || density <= 0)
            {
                errors.Add("Density must be a positive number.");
                if (showErrors) MarkInvalid("Density");
            }

            // Validate Specific Heat
            if (!double.TryParse(SpecificHeatTextBox.Text, out double specificHeat) || specificHeat <= 0)
            {
                errors.Add("Specific heat must be a positive number.");
                if (showErrors) MarkInvalid("SpecificHeat");
            }

            // Validate Vapor Resistance
            if (!double.TryParse(VaporResistanceTextBox.Text, out double vaporResistance) || vaporResistance < 0)
            {
                errors.Add("Vapor resistance must be a non-negative number.");
                if (showErrors) MarkInvalid("VaporResistance");
            }

            // Show validation errors if any
            if (showErrors && errors.Count > 0)
            {
                ValidationErrorText.Text = string.Join("\n", errors);
                ValidationErrorPanel.Visibility = Visibility.Visible;
            }

            return errors.Count == 0;
        }

        private void MarkInvalid(string propertyName)
        {
            if (_validationControls.TryGetValue(propertyName, out Control control))
            {
                control.BorderBrush = Brushes.Red;
                control.ToolTip = ValidationErrorText.Text;
            }
        }

        private void ClearValidationErrors()
        {
            ValidationErrorPanel.Visibility = Visibility.Collapsed;

            foreach (var control in _validationControls.Values)
            {
                control.BorderBrush = (SolidColorBrush)FindResource("TextBoxBorder");
                control.ToolTip = null;
            }
        }

        private bool SaveMaterial()
        {
            if (_currentMaterial == null)
            {
                return false;
            }

            // Validate input
            if (!ValidateInputs(true))
            {
                return false;
            }

            // Update the material
            _currentMaterial.Name = NameTextBox.Text;
            _currentMaterial.Category = CategoryComboBox.Text;
            _currentMaterial.ThermalConductivity = double.Parse(ConductivityTextBox.Text);
            _currentMaterial.Density = double.Parse(DensityTextBox.Text);
            _currentMaterial.SpecificHeat = double.Parse(SpecificHeatTextBox.Text);
            _currentMaterial.VaporResistance = double.Parse(VaporResistanceTextBox.Text);
            _currentMaterial.Description = DescriptionTextBox.Text;

            // Add to database if it's a new material
            if (_isNewMaterial)
            {
                App.MaterialDb.AddMaterial(_currentMaterial);
                _isNewMaterial = false;
            }

            // Save changes
            App.MaterialDb.SaveChanges();

            // Refresh the UI
            LoadMaterialsAndCategories();

            // Select the saved material
            for (int i = 0; i < MaterialList.Items.Count; i++)
            {
                if (MaterialList.Items[i] is BuildingMaterials m && m.Id == _currentMaterial.Id)
                {
                    MaterialList.SelectedIndex = i;
                    break;
                }
            }

            _hasChanges = false;
            UpdateSaveButtonState();

            MessageBox.Show("Material saved successfully.", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);

            return true;
        }

        #endregion

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_hasChanges)
            {
                MessageBoxResult result = MessageBox.Show(
                    "You have unsaved changes. Do you want to save them?",
                    "Save Changes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                }
                else if (result == MessageBoxResult.Yes)
                {
                    if (!SaveMaterial())
                    {
                        e.Cancel = true;
                    }
                }
            }

            base.OnClosing(e);
        }
    }
}