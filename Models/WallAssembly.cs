using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace UFactor.Models
{
    public class WallAssembly : INotifyPropertyChanged
    {
        private string _name;
        private string _type;
        private ObservableCollection<AssemblyLayer> _layers;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public string Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged(nameof(Type));
            }
        }

        public ObservableCollection<AssemblyLayer> Layers
        {
            get => _layers;
            set
            {
                if (_layers != null)
                {
                    _layers.CollectionChanged -= Layers_CollectionChanged;
                }

                _layers = value;

                if (_layers != null)
                {
                    _layers.CollectionChanged += Layers_CollectionChanged;
                }

                OnPropertyChanged(nameof(Layers));
                UpdateCalculatedProperties();
            }
        }

        // Calculated properties
        public double TotalRValue => Layers?.Sum(l => l.RValue) ?? 0;
        public double UFactor => TotalRValue > 0 ? 1.0 / TotalRValue : 0;
        public double TotalThickness => Layers?.Sum(l => l.Thickness) ?? 0;

        public WallAssembly()
        {
            Name = "Wall Assembly 1";
            Type = "Wall";
            Layers = new ObservableCollection<AssemblyLayer>();
            Layers.CollectionChanged += Layers_CollectionChanged;
        }

        public WallAssembly(string name, string type)
        {
            Name = name;
            Type = type;
            Layers = new ObservableCollection<AssemblyLayer>();
            Layers.CollectionChanged += Layers_CollectionChanged;
        }

        private void Layers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Subscribe to property changes of new layers
            if (e.NewItems != null)
            {
                foreach (AssemblyLayer layer in e.NewItems)
                {
                    layer.PropertyChanged += Layer_PropertyChanged;
                }
            }

            // Unsubscribe from property changes of removed layers
            if (e.OldItems != null)
            {
                foreach (AssemblyLayer layer in e.OldItems)
                {
                    layer.PropertyChanged -= Layer_PropertyChanged;
                }
            }

            // Renumber layers
            RenumberLayers();
            UpdateCalculatedProperties();
        }

        private void Layer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Update calculated properties when any layer property changes
            if (e.PropertyName == nameof(AssemblyLayer.RValue) ||
                e.PropertyName == nameof(AssemblyLayer.Thickness))
            {
                UpdateCalculatedProperties();
            }
        }

        private void RenumberLayers()
        {
            if (Layers != null)
            {
                for (int i = 0; i < Layers.Count; i++)
                {
                    Layers[i].LayerNumber = i + 1;
                }
            }
        }

        private void UpdateCalculatedProperties()
        {
            OnPropertyChanged(nameof(TotalRValue));
            OnPropertyChanged(nameof(UFactor));
            OnPropertyChanged(nameof(TotalThickness));
        }

        public void AddLayer(AssemblyLayer layer)
        {
            if (layer != null)
            {
                Layers.Add(layer);
            }
        }

        public void RemoveLayer(AssemblyLayer layer)
        {
            if (layer != null && Layers.Contains(layer))
            {
                Layers.Remove(layer);
            }
        }

        public void RemoveLayerAt(int index)
        {
            if (index >= 0 && index < Layers.Count)
            {
                Layers.RemoveAt(index);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}