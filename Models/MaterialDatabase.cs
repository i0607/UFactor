using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace UFactor.Models
{
    public class MaterialDatabase
    {
        private List<BuildingMaterials> _materials;
        private readonly string _dataFilePath;

        public IEnumerable<BuildingMaterials> Materials => _materials;

        public MaterialDatabase()
        {
            _materials = new List<BuildingMaterials>();

            // Create data directory if it doesn't exist
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, "UFactor");

            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }

            _dataFilePath = Path.Combine(appFolder, "materials.json");

            LoadMaterials();
        }

        public void AddMaterial(BuildingMaterials material)
        {
            if (material == null) return;

            if (material.Id == Guid.Empty)
            {
                material.Id = Guid.NewGuid();
            }

            _materials.Add(material);
        }

        public void UpdateMaterial(BuildingMaterials updatedMaterial)
        {
            if (updatedMaterial == null) return;

            var existingMaterial = _materials.FirstOrDefault(m => m.Id == updatedMaterial.Id);
            if (existingMaterial != null)
            {
                var index = _materials.IndexOf(existingMaterial);
                _materials[index] = updatedMaterial;
            }
        }

        public void RemoveMaterial(Guid materialId)
        {
            var material = _materials.FirstOrDefault(m => m.Id == materialId);
            if (material != null)
            {
                _materials.Remove(material);
            }
        }

        public BuildingMaterials GetMaterialById(Guid id)
        {
            return _materials.FirstOrDefault(m => m.Id == id);
        }

        public BuildingMaterials GetMaterialByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return _materials.FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        // Add the missing GetMaterial method
        public BuildingMaterials GetMaterial(string name)
        {
            return GetMaterialByName(name);
        }

        public void SaveChanges()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string jsonString = JsonSerializer.Serialize(_materials, options);
                File.WriteAllText(_dataFilePath, jsonString);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error saving materials to file: {ex.Message}");
            }
        }

        private void LoadMaterials()
        {
            try
            {
                if (File.Exists(_dataFilePath))
                {
                    string jsonString = File.ReadAllText(_dataFilePath);
                    if (!string.IsNullOrEmpty(jsonString))
                    {
                        var materials = JsonSerializer.Deserialize<List<BuildingMaterials>>(jsonString);
                        _materials = materials ?? new List<BuildingMaterials>();
                    }
                }
            }
            catch (Exception ex)
            {
                // If loading fails, start with empty list
                _materials = new List<BuildingMaterials>();
                System.Diagnostics.Debug.WriteLine($"Error loading materials: {ex.Message}");
            }
        }
    }
}