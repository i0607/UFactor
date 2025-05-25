using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace UFactor.Models
{
    public class MaterialDatabase
    {
        private readonly string _dbFilePath;
        private ObservableCollection<BuildingMaterials> _materials;

        public MaterialDatabase(string dbFilePath)
        {
            _dbFilePath = dbFilePath;
            _materials = new ObservableCollection<BuildingMaterials>();
            LoadMaterials();
        }

        public ObservableCollection<BuildingMaterials> Materials => _materials;

        public IEnumerable<string> Categories => _materials
            .Select(m => m.Category)
            .Distinct()
            .OrderBy(c => c);

        public void AddMaterial(BuildingMaterials material)
        {
            if (material.Id == Guid.Empty)
            {
                material.Id = Guid.NewGuid();
            }

            _materials.Add(material);
        }

        public bool RemoveMaterial(Guid id)
        {
            var material = _materials.FirstOrDefault(m => m.Id == id);
            if (material != null)
            {
                return _materials.Remove(material);
            }
            return false;
        }

        public BuildingMaterials GetMaterial(Guid id)
        {
            return _materials.FirstOrDefault(m => m.Id == id);
        }

        public void SaveChanges()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(_materials, options);
            File.WriteAllText(_dbFilePath, json);
        }

        private void LoadMaterials()
        {
            if (File.Exists(_dbFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_dbFilePath);
                    var loadedMaterials = JsonSerializer.Deserialize<List<BuildingMaterials>>(json);

                    _materials.Clear();
                    foreach (var material in loadedMaterials)
                    {
                        _materials.Add(material);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading materials: {ex.Message}");
                }
            }
        }
    }
}