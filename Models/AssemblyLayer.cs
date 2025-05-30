using System;
using System.ComponentModel;

namespace UFactor.Models
{
    public class AssemblyLayer : INotifyPropertyChanged
    {
        private int _layerNumber;
        private Guid _materialId;
        private double _thickness;
        private string _description;
        private string _materialName;

        public int LayerNumber
        {
            get => _layerNumber;
            set
            {
                _layerNumber = value;
                OnPropertyChanged(nameof(LayerNumber));
            }
        }

        public Guid MaterialId
        {
            get => _materialId;
            set
            {
                _materialId = value;
                UpdateMaterialName();
                OnPropertyChanged(nameof(MaterialId));
                OnPropertyChanged(nameof(RValue));
            }
        }

        public double Thickness
        {
            get => _thickness;
            set
            {
                _thickness = value;
                OnPropertyChanged(nameof(Thickness));
                OnPropertyChanged(nameof(RValue));
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged(nameof(Description));
            }
        }

        public string MaterialName
        {
            get => _materialName;
            set
            {
                _materialName = value;
                OnPropertyChanged(nameof(MaterialName));
            }
        }

        // Calculated property
        public double RValue
        {
            get
            {
                if (App.MaterialDb != null && MaterialId != Guid.Empty && Thickness > 0)
                {
                    var material = App.MaterialDb.GetMaterialById(MaterialId);
                    if (material != null && material.ThermalConductivity > 0)
                    {
                        // R = thickness(m) / thermal_conductivity
                        return (Thickness / 1000.0) / material.ThermalConductivity;
                    }
                }
                return 0;
            }
        }

        public AssemblyLayer()
        {
            LayerNumber = 1;
            MaterialId = Guid.Empty;
            Thickness = 0;
            Description = string.Empty;
            MaterialName = string.Empty;
        }

        private void UpdateMaterialName()
        {
            if (App.MaterialDb != null && MaterialId != Guid.Empty)
            {
                var material = App.MaterialDb.GetMaterialById(MaterialId);
                MaterialName = material?.Name ?? "Unknown Material";
            }
            else
            {
                MaterialName = string.Empty;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}