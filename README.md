**U-Factor Calculator**

**Building Thermal Performance Analysis Tool
Final International University (FIU)**

**📋 Overview**

The **U-Factor Calculator** is a professional desktop application designed for building thermal performance analysis. It calculates the thermal transmittance (U-Factor) of building wall assemblies, helping architects, engineers, and building professionals optimize energy efficiency and comply with building codes.


**🎯 Key Features**

**Multi-Assembly Analysis:** Calculate U-factors for multiple wall assemblies simultaneously
**Layer-by-Layer Design:** Build assemblies by adding individual material layers
**Material Database:** Comprehensive library of building materials with thermal properties
**Real-Time Calculations:** Instant U-factor updates as you modify assemblies
**Project Management:** Save, load, and manage multiple calculation projects
**Professional UI:** Modern, intuitive interface with Material Design elements
**Export Capabilities:** Generate reports and export results
**Code Compliance:** Supports various building code standards

**🚀 Getting Started**

**Prerequisites**

- Windows 10/11 (64-bit recommended)
- .NET Framework 4.7.2 or higher
- Visual Studio 2019/2022 (for development)
- 4 GB RAM minimum (8 GB recommended)

**Installation**
**Option 1: Download Release**

**-Go to Releases**
-Download the latest UFactor-Calculator-Setup.exe
-Run the installer and follow the setup wizard
-Launch from Start Menu or Desktop shortcut

**Option 2: Build from Source**

bash# Clone the repository
git clone https://github.com/yourusername/ufactor-calculator.git
cd ufactor-calculator

**# Open in Visual Studio**
start UFactor.sln

# Build and run
# Press F5 or Build > Start Debugging
📖 Usage Guide
Creating Your First Assembly

**Launch the Application**

Open U-Factor Calculator from your Start Menu
The application opens with a default "Wall Assembly 1" tab

**
Add Material Layers**

Click "Add Layer" to add materials to your assembly
Select materials from the dropdown (e.g., "Brick", "Insulation", "Drywall")
Enter the thickness for each layer in inches or millimeters


**View Results**

U-Factor is calculated automatically in real-time
Results show:

U-Factor (Btu/hr·ft²·°F or W/m²·K)
R-Value total thermal resistance
Assembly Thickness




**Multiple Assemblies**

Click the "+" tab to add new assemblies
Compare different wall configurations side-by-side
Close assemblies using the "×" button on each tab



**Project Management**

Save Project: File > Save Project or Ctrl+S
Open Project: File > Open Project or Ctrl+O
New Project: File > New Project or Ctrl+N

**Material Database**
The application includes a comprehensive material database with thermal properties:
CategoryExamplesMasonryBrick, Concrete Block, StoneInsulationFiberglass, Foam, RockwoolFinishesDrywall, Plaster, Wood PanelingStructuralSteel, Wood Framing, ConcreteMembranesVapor Barriers, Weather Barriers
🛠️ Technical Details
Architecture
UFactor/
├── Models/           # Data models and business logic
│   ├── Assembly.cs
│   ├── Layer.cs
│   └── Material.cs
├── ViewModels/       # MVVM view models
├── Views/            # WPF user controls and windows
│   └── MainWindow.xaml
├── Services/         # Business services
│   ├── CalculationService.cs
│   └── MaterialService.cs
├── Data/             # Material database
└── Assets/           # Images and resources
Key Technologies

Framework: .NET Framework 4.7.2+ / .NET 6.0+
UI Framework: WPF (Windows Presentation Foundation)
Pattern: MVVM (Model-View-ViewModel)
Data Storage: JSON project files
Styling: Material Design inspired themes

Calculation Methods
The U-Factor calculation follows ASHRAE standards:
U-Factor = 1 / R-total
R-total = R-inside + Σ(R-layers) + R-outside
R-layer = Thickness / Conductivity
Where:

R-inside: Interior surface resistance (0.68 hr·ft²·°F/Btu)
R-outside: Exterior surface resistance (0.17 hr·ft²·°F/Btu)
R-layers: Thermal resistance of each material layer
