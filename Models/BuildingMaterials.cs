using System;

namespace UFactor.Models
{
    public class BuildingMaterials
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public double ThermalConductivity { get; set; } // W/(m·K)
        public double Density { get; set; } // kg/m³
        public double SpecificHeat { get; set; } // J/(kg·K)
        public double VaporResistance { get; set; } // μ (dimensionless)
        public string Description { get; set; }

        public BuildingMaterials()
        {
            Id = Guid.NewGuid();
            Name = string.Empty;
            Category = string.Empty;
            Description = string.Empty;
        }

        // Calculated property
        public double RValuePerMm => ThermalConductivity > 0 ? (1.0 / ThermalConductivity) * 0.001 : 0;

        public override string ToString()
        {
            return Name ?? "Unnamed Material";
        }
    }
}