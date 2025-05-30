using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using UFactor.Models;

namespace UFactor.Models
{
    // Main project file structure
    public class ProjectData
    {
        public string ProjectName { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModified { get; set; }
        public string Version { get; set; }
        public List<WallAssemblyData> Assemblies { get; set; }

        public ProjectData()
        {
            ProjectName = "Untitled Project";
            Description = "";
            CreatedDate = DateTime.Now;
            LastModified = DateTime.Now;
            Version = "1.0";
            Assemblies = new List<WallAssemblyData>();
        }
    }

    // Serializable version of WallAssembly
    public class WallAssemblyData
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public List<AssemblyLayerData> Layers { get; set; }

        public WallAssemblyData()
        {
            Name = "";
            Type = "Wall";
            Layers = new List<AssemblyLayerData>();
        }

        // Convert from WallAssembly to serializable format
        public static WallAssemblyData FromWallAssembly(WallAssembly assembly)
        {
            var data = new WallAssemblyData
            {
                Name = assembly.Name,
                Type = assembly.Type,
                Layers = new List<AssemblyLayerData>()
            };

            foreach (var layer in assembly.Layers)
            {
                data.Layers.Add(AssemblyLayerData.FromAssemblyLayer(layer));
            }

            return data;
        }

        // Convert to WallAssembly from serializable format
        public WallAssembly ToWallAssembly()
        {
            var assembly = new WallAssembly(Name, Type);

            foreach (var layerData in Layers)
            {
                assembly.AddLayer(layerData.ToAssemblyLayer());
            }

            return assembly;
        }
    }

    // Serializable version of AssemblyLayer
    public class AssemblyLayerData
    {
        public int LayerNumber { get; set; }
        public Guid MaterialId { get; set; }
        public double Thickness { get; set; }
        public string Description { get; set; }
        public string MaterialName { get; set; } // For reference

        public AssemblyLayerData()
        {
            LayerNumber = 1;
            MaterialId = Guid.Empty;
            Thickness = 0;
            Description = "";
            MaterialName = "";
        }

        // Convert from AssemblyLayer to serializable format
        public static AssemblyLayerData FromAssemblyLayer(AssemblyLayer layer)
        {
            return new AssemblyLayerData
            {
                LayerNumber = layer.LayerNumber,
                MaterialId = layer.MaterialId,
                Thickness = layer.Thickness,
                Description = layer.Description,
                MaterialName = layer.MaterialName
            };
        }

        // Convert to AssemblyLayer from serializable format
        public AssemblyLayer ToAssemblyLayer()
        {
            var layer = new AssemblyLayer
            {
                LayerNumber = LayerNumber,
                MaterialId = MaterialId,
                Thickness = Thickness,
                Description = Description,
                MaterialName = MaterialName
            };

            return layer;
        }
    }
}

// Project file management service
namespace UFactor.Services
{
    public class ProjectService
    {
        private const string DefaultProjectExtension = ".ufactor";
        private const string ProjectFilter = "UFactor Project Files (*.ufactor)|*.ufactor|All Files (*.*)|*.*";

        public static string GetDefaultProjectsFolder()
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string projectsPath = Path.Combine(documentsPath, "UFactor Projects");

            // Create directory if it doesn't exist
            if (!Directory.Exists(projectsPath))
            {
                Directory.CreateDirectory(projectsPath);
            }

            return projectsPath;
        }

        public static void SaveProject(ProjectData project, string filePath)
        {
            try
            {
                project.LastModified = DateTime.Now;

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string jsonString = JsonSerializer.Serialize(project, options);
                File.WriteAllText(filePath, jsonString);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error saving project: {ex.Message}");
            }
        }

        public static ProjectData LoadProject(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Project file not found: {filePath}");
                }

                string jsonString = File.ReadAllText(filePath);

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var project = JsonSerializer.Deserialize<ProjectData>(jsonString, options);

                if (project == null)
                {
                    throw new Exception("Failed to deserialize project data");
                }

                return project;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading project: {ex.Message}");
            }
        }

        public static string GetProjectFilter()
        {
            return ProjectFilter;
        }

        public static string GetDefaultExtension()
        {
            return DefaultProjectExtension;
        }
    }
}