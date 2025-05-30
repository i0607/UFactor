using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UFactor.Models;

namespace UFactor.Views
{
    public partial class MaterialManagementWindow : Window
    {
        private List<BuildingMaterials> _allMaterials;
        private BuildingMaterials _selectedMaterial;
        private bool _isLoading;

        public MaterialManagementWindow()
        {
            InitializeComponent();
            _allMaterials = new List<BuildingMaterials>();
            _selectedMaterial = null;
            LoadWindow();
        }

        private void LoadWindow()
        {
            try
            {
                _isLoading = true;

                // Load materials from database
                if (App.MaterialDb != null && App.MaterialDb.Materials != null)
                {
                    _allMaterials = App.MaterialDb.Materials.ToList();
                }
                else
                {
                    _allMaterials = new List<BuildingMaterials>();
                }

                // Populate category filter
                PopulateCategoryFilter();

                // Update material list
                UpdateMaterialsList();

                // Initialize form
                ClearForm();

                _isLoading = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading materials: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateCategoryFilter()
        {
            try
            {
                if (CategoryFilter != null)
                {
                    CategoryFilter.Items.Clear();
                    CategoryFilter.Items.Add("All Categories");

                    var categories = _allMaterials
                        .Where(m => !string.IsNullOrEmpty(m.Category))
                        .Select(m => m.Category)
                        .Distinct()
                        .OrderBy(c => c);

                    foreach (var category in categories)
                    {
                        CategoryFilter.Items.Add(category);
                    }

                    CategoryFilter.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error populating categories: {ex.Message}");
            }
        }

        private void UpdateMaterialsList()
        {
            try
            {
                if (MaterialList == null)
                {
                    System.Diagnostics.Debug.WriteLine("MaterialList is null - window may not be fully loaded");
                    return;
                }

                // Get selected category
                string selectedCategory = "All Categories";
                if (CategoryFilter != null && CategoryFilter.SelectedItem != null)
                {
                    selectedCategory = CategoryFilter.SelectedItem.ToString();
                }

                // Filter materials by category
                IEnumerable<BuildingMaterials> materials = _allMaterials;
                if (selectedCategory != "All Categories")
                {
                    materials = materials.Where(m => m.Category == selectedCategory);
                }

                // Add materials to the list
                MaterialList.Items.Clear();
                foreach (var material in materials.OrderBy(m => m.Name))
                {
                    MaterialList.Items.Add(material);
                }

                // Re-select the previously selected material if it's still in the list
                if (_selectedMaterial != null)
                {
                    for (int i = 0; i < MaterialList.Items.Count; i++)
                    {
                        if (MaterialList.Items[i] is BuildingMaterials material && material.Id == _selectedMaterial.Id)
                        {
                            MaterialList.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating materials list: {ex.Message}");
                MessageBox.Show($"Error updating materials list: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;
            UpdateMaterialsList();
        }

        private void MaterialList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;

            try
            {
                if (MaterialList != null && MaterialList.SelectedItem is BuildingMaterials material)
                {
                    _selectedMaterial = material;
                    LoadMaterialToForm(material);
                    if (DeleteButton != null)
                        DeleteButton.IsEnabled = true;
                }
                else
                {
                    _selectedMaterial = null;
                    if (DeleteButton != null)
                        DeleteButton.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error selecting material: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadMaterialToForm(BuildingMaterials material)
        {
            try
            {
                _isLoading = true;

                if (NameTextBox != null)
                    NameTextBox.Text = material.Name ?? string.Empty;
                if (CategoryComboBox != null)
                    CategoryComboBox.Text = material.Category ?? string.Empty;
                if (ConductivityTextBox != null)
                    ConductivityTextBox.Text = material.ThermalConductivity.ToString("0.####");
                if (DensityTextBox != null)
                    DensityTextBox.Text = material.Density.ToString("0.#");
                if (SpecificHeatTextBox != null)
                    SpecificHeatTextBox.Text = material.SpecificHeat.ToString("0.#");
                if (VaporResistanceTextBox != null)
                    VaporResistanceTextBox.Text = material.VaporResistance.ToString("0.#");
                if (DescriptionTextBox != null)
                    DescriptionTextBox.Text = material.Description ?? string.Empty;

                UpdateRValue();
                if (SaveButton != null) SaveButton.IsEnabled = false;
                if (ValidationErrorPanel != null) ValidationErrorPanel.Visibility = Visibility.Collapsed;

                _isLoading = false;
            }
            catch (Exception ex)
            {
                _isLoading = false;
                MessageBox.Show($"Error loading material to form: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearForm()
        {
            try
            {
                _isLoading = true;

                if (NameTextBox != null) NameTextBox.Text = string.Empty;
                if (CategoryComboBox != null) CategoryComboBox.Text = string.Empty;
                if (ConductivityTextBox != null) ConductivityTextBox.Text = string.Empty;
                if (DensityTextBox != null) DensityTextBox.Text = string.Empty;
                if (SpecificHeatTextBox != null) SpecificHeatTextBox.Text = string.Empty;
                if (VaporResistanceTextBox != null) VaporResistanceTextBox.Text = string.Empty;
                if (DescriptionTextBox != null) DescriptionTextBox.Text = string.Empty;
                if (RValueTextBox != null) RValueTextBox.Text = string.Empty;

                if (SaveButton != null) SaveButton.IsEnabled = false;
                if (DeleteButton != null) DeleteButton.IsEnabled = false;
                if (ValidationErrorPanel != null) ValidationErrorPanel.Visibility = Visibility.Collapsed;

                _isLoading = false;
            }
            catch (Exception ex)
            {
                _isLoading = false;
                System.Diagnostics.Debug.WriteLine($"Error clearing form: {ex.Message}");
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _selectedMaterial = null;
                if (MaterialList != null) MaterialList.SelectedItem = null;
                ClearForm();
                if (NameTextBox != null) NameTextBox.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding new material: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedMaterial == null)
                {
                    MessageBox.Show("Please select a material to delete.", "No Selection",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show(
                    $"Are you sure you want to delete '{_selectedMaterial.Name}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    if (App.MaterialDb != null)
                    {
                        App.MaterialDb.RemoveMaterial(_selectedMaterial.Id);
                        if (App.MaterialDb.Materials != null)
                        {
                            _allMaterials = App.MaterialDb.Materials.ToList();
                        }
                        else
                        {
                            _allMaterials = new List<BuildingMaterials>();
                        }
                    }

                    UpdateMaterialsList();
                    PopulateCategoryFilter();
                    ClearForm();
                    _selectedMaterial = null;

                    MessageBox.Show("Material deleted successfully.", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting material: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateForm())
                    return;

                BuildingMaterials material;
                try
                {
                    material = CreateMaterialFromForm();
                }
                catch
                {
                    return; // Error message already shown in CreateMaterialFromForm
                }

                if (App.MaterialDb != null)
                {
                    if (_selectedMaterial == null)
                    {
                        // Adding new material
                        App.MaterialDb.AddMaterial(material);
                        MessageBox.Show("Material added successfully.", "Success",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // Updating existing material
                        material.Id = _selectedMaterial.Id;
                        App.MaterialDb.UpdateMaterial(material);
                        MessageBox.Show("Material updated successfully.", "Success",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    if (App.MaterialDb.Materials != null)
                    {
                        _allMaterials = App.MaterialDb.Materials.ToList();
                    }
                    else
                    {
                        _allMaterials = new List<BuildingMaterials>();
                    }
                }

                UpdateMaterialsList();
                PopulateCategoryFilter();
                if (SaveButton != null) SaveButton.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving material: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void PropertyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoading) return;
            if (SaveButton != null) SaveButton.IsEnabled = true;
        }

        private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;
            if (SaveButton != null) SaveButton.IsEnabled = true;
        }

        private void NumericTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoading) return;

            UpdateRValue();
            if (SaveButton != null) SaveButton.IsEnabled = true;
        }

        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Allow only numbers and decimal point
            if (sender is TextBox textBox)
            {
                Regex regex = new Regex(@"^[0-9]*\.?[0-9]*$");
                e.Handled = !regex.IsMatch(textBox.Text + e.Text);
            }
        }

        private void UpdateRValue()
        {
            try
            {
                if (ConductivityTextBox != null && RValueTextBox != null)
                {
                    if (double.TryParse(ConductivityTextBox.Text, out double conductivity) && conductivity > 0)
                    {
                        double rValue = 1.0 / conductivity * 0.001; // Per mm
                        RValueTextBox.Text = rValue.ToString("0.######");
                    }
                    else
                    {
                        RValueTextBox.Text = string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating R-value: {ex.Message}");
            }
        }

        private bool ValidateForm()
        {
            var errors = new List<string>();

            string nameText = string.Empty;
            string categoryText = string.Empty;
            string conductivityText = string.Empty;
            string densityText = string.Empty;
            string specificHeatText = string.Empty;
            string vaporResistanceText = string.Empty;

            if (NameTextBox != null) nameText = NameTextBox.Text ?? string.Empty;
            if (CategoryComboBox != null) categoryText = CategoryComboBox.Text ?? string.Empty;
            if (ConductivityTextBox != null) conductivityText = ConductivityTextBox.Text ?? string.Empty;
            if (DensityTextBox != null) densityText = DensityTextBox.Text ?? string.Empty;
            if (SpecificHeatTextBox != null) specificHeatText = SpecificHeatTextBox.Text ?? string.Empty;
            if (VaporResistanceTextBox != null) vaporResistanceText = VaporResistanceTextBox.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(nameText))
                errors.Add("Name is required");

            if (string.IsNullOrWhiteSpace(categoryText))
                errors.Add("Category is required");

            if (!double.TryParse(conductivityText, out double conductivity) || conductivity <= 0)
                errors.Add("Thermal Conductivity must be a positive number");

            if (!double.TryParse(densityText, out double density) || density <= 0)
                errors.Add("Density must be a positive number");

            if (!double.TryParse(specificHeatText, out double specificHeat) || specificHeat <= 0)
                errors.Add("Specific Heat must be a positive number");

            if (!double.TryParse(vaporResistanceText, out double vaporResistance) || vaporResistance < 0)
                errors.Add("Vapor Resistance must be a non-negative number");

            if (errors.Any())
            {
                if (ValidationErrorText != null) ValidationErrorText.Text = string.Join("\n", errors);
                if (ValidationErrorPanel != null) ValidationErrorPanel.Visibility = Visibility.Visible;
                return false;
            }

            if (ValidationErrorPanel != null) ValidationErrorPanel.Visibility = Visibility.Collapsed;
            return true;
        }

        private BuildingMaterials CreateMaterialFromForm()
        {
            try
            {
                string nameText = string.Empty;
                string categoryText = string.Empty;
                string conductivityText = "0";
                string densityText = "0";
                string specificHeatText = "0";
                string vaporResistanceText = "0";
                string descriptionText = string.Empty;

                if (NameTextBox != null && NameTextBox.Text != null)
                    nameText = NameTextBox.Text.Trim();
                if (CategoryComboBox != null && CategoryComboBox.Text != null)
                    categoryText = CategoryComboBox.Text.Trim();
                if (ConductivityTextBox != null && ConductivityTextBox.Text != null)
                    conductivityText = ConductivityTextBox.Text;
                if (DensityTextBox != null && DensityTextBox.Text != null)
                    densityText = DensityTextBox.Text;
                if (SpecificHeatTextBox != null && SpecificHeatTextBox.Text != null)
                    specificHeatText = SpecificHeatTextBox.Text;
                if (VaporResistanceTextBox != null && VaporResistanceTextBox.Text != null)
                    vaporResistanceText = VaporResistanceTextBox.Text;
                if (DescriptionTextBox != null && DescriptionTextBox.Text != null)
                    descriptionText = DescriptionTextBox.Text.Trim();

                return new BuildingMaterials
                {
                    Name = nameText,
                    Category = categoryText,
                    ThermalConductivity = double.Parse(conductivityText),
                    Density = double.Parse(densityText),
                    SpecificHeat = double.Parse(specificHeatText),
                    VaporResistance = double.Parse(vaporResistanceText),
                    Description = descriptionText
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating material from form: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                throw; // Re-throw to let caller handle
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Double-check that all controls are available
                if (MaterialList == null || CategoryFilter == null)
                {
                    MessageBox.Show("Error: Window controls not properly initialized.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                    return;
                }

                // Refresh the display
                LoadWindow();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading window: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }
    }
}