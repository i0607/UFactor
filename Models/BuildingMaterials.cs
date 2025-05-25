using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UFactor.Models
{
    public class BuildingMaterials : INotifyPropertyChanged
    {
        private string _name;
        private string _category;
        private double _thermalConductivity;
        private double _density;
        private double _specificHeat;
        private double _vaporResistance;
        private string _description;
        private Guid _id;

        public BuildingMaterials()
        {
            _id = Guid.NewGuid();
        }

        public Guid Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(); }
        }

        public double ThermalConductivity
        {
            get => _thermalConductivity;
            set
            {
                _thermalConductivity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RValuePerMm));
            }
        }

        public double Density
        {
            get => _density;
            set { _density = value; OnPropertyChanged(); }
        }

        public double SpecificHeat
        {
            get => _specificHeat;
            set { _specificHeat = value; OnPropertyChanged(); }
        }

        public double VaporResistance
        {
            get => _vaporResistance;
            set { _vaporResistance = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        // Calculated property
        public double RValuePerMm => ThermalConductivity > 0 ? 0.001 / ThermalConductivity : 0;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}