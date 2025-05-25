using System;
using System.IO;
using System.Windows;
using UFactor.Models;

namespace UFactor
{
    public partial class App : Application
    {
        public static MaterialDatabase MaterialDb { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize material database
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "UFactor");

            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            string dbPath = Path.Combine(appDataPath, "materials.json");
            MaterialDb = new MaterialDatabase(dbPath);

            // If no materials exist, add some default ones
            if (MaterialDb.Materials.Count == 0)
            {
                AddDefaultMaterials();
            }
        }

        private void AddDefaultMaterials()
        {
            MaterialDb.AddMaterial(new BuildingMaterials
            {
                Name = "Brick",
                Category = "Masonry",
                ThermalConductivity = 0.72,
                Density = 1800,
                SpecificHeat = 840,
                VaporResistance = 10,
                Description = "Standard clay brick"
            });

            MaterialDb.AddMaterial(new BuildingMaterials
            {
                Name = "Concrete Block",
                Category = "Masonry",
                ThermalConductivity = 1.0,
                Density = 2000,
                SpecificHeat = 840,
                VaporResistance = 6,
                Description = "Standard concrete block"
            });

            MaterialDb.AddMaterial(new BuildingMaterials
            {
                Name = "Mineral Wool",
                Category = "Insulation",
                ThermalConductivity = 0.04,
                Density = 30,
                SpecificHeat = 840,
                VaporResistance = 1,
                Description = "Standard mineral wool insulation"
            });

            MaterialDb.AddMaterial(new BuildingMaterials
            {
                Name = "Gypsum Board",
                Category = "Board Materials",
                ThermalConductivity = 0.25,
                Density = 900,
                SpecificHeat = 840,
                VaporResistance = 8,
                Description = "Standard drywall"
            });

            MaterialDb.SaveChanges();
        }
    }
}