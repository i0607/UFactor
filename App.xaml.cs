using System.Linq;
using System.Windows;
using UFactor.Models;

namespace UFactor
{
    public partial class App : Application
    {
        public static MaterialDatabase MaterialDb { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);

                // Initialize the material database
                MaterialDb = new MaterialDatabase();

                // Check if materials exist, if not add defaults
                if (MaterialDb.Materials == null || !MaterialDb.Materials.Any())
                {
                    AddDefaultMaterials();
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Application startup error: {ex.Message}\n\nThe application will now close.",
                    "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(-1);
            }
        }

        private void AddDefaultMaterials()
        {
            try
            {
                // Check if we already have materials to avoid duplicates
                if (MaterialDb != null && MaterialDb.Materials != null && MaterialDb.Materials.Any())
                    return;

                if (MaterialDb != null)
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
                        Description = "Standard hollow concrete block"
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

                    // Save the changes
                    MaterialDb.SaveChanges();
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error adding default materials: {ex.Message}",
                    "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                // Save any pending changes
                if (MaterialDb != null)
                {
                    MaterialDb.SaveChanges();
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving on exit: {ex.Message}");
            }
            finally
            {
                base.OnExit(e);
            }
        }
    }
}