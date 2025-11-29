# NCAA Translator WPF Application

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![Windows](https://img.shields.io/badge/platform-Windows-lightgrey.svg)](https://www.microsoft.com/windows)

A powerful Windows desktop application for processing and translating NCAA sports data, featuring real-time game monitoring, name translation, and graphics template updates.

## 📋 Table of Contents

- [Features](#-features)
- [Prerequisites](#-prerequisites)
- [Installation](#-installation)
- [Configuration](#-configuration)
- [Usage](#-usage)
- [User Interface](#-user-interface)
- [Settings](#-settings)
- [Name Converters](#-name-converters)
- [Troubleshooting](#-troubleshooting)
- [Contributing](#-contributing)

## ✨ Features

### Real-Time Data Processing
- **Live Game Monitoring**: Automatically fetches and displays live NCAA game data
- **Periodic Updates**: Configurable timer-based data refresh (5-300 seconds)
- **Multiple Sports Support**: Basketball, Football, Hockey, Volleyball, and more

### Advanced Filtering & Display
- **Game Display Modes**:
  - **Live**: Shows only currently active games
  - **All**: Displays all games (conference, non-conference, home)
  - **Display**: Shows games for configured display teams
- **Conference & Non-Conference Games**: Separate categorization
- **Home Team Highlighting**: Special handling for your favorite team

### Name Translation System
- **Team Name Mapping**: Custom display names for NCAA 6-character codes
- **Conference Translation**: Localized conference names
- **Dynamic Updates**: Add new teams and conferences on-the-fly

### Graphics Integration
- **XML Template Updates**: Automatic Out-of-Score (OOS) template updates
- **JSON Conversion**: XML to JSON conversion for modern systems
- **Configurable Output**: Custom file paths and naming patterns

### User-Friendly Interface
- **Tabbed Interface**: Organized settings and data views
- **Search & Filter**: Quick team and conference lookup
- **Auto-Save**: Changes saved automatically
- **Modern UI**: Clean, responsive WPF interface

## 🔧 Prerequisites

- **Operating System**: Windows 10 or later
- **.NET Runtime**: .NET 8.0 Desktop Runtime
- **Internet Connection**: Required for fetching NCAA data

## 📦 Installation

### Option 1: Download Release
1. Download the latest release from the [Releases](https://github.com/yourusername/NcaaTranslator/releases) page
2. Extract the ZIP file to your desired location
3. Run `NcaaTranslator.Wpf.exe`

### Option 2: Build from Source
```bash
# Clone the repository
git clone https://github.com/yourusername/NcaaTranslator.git
cd NcaaTranslator

# Build the WPF application
dotnet build src/NcaaTranslator.Wpf/NcaaTranslator.Wpf.csproj

# Run the application
dotnet run --project src/NcaaTranslator.Wpf/NcaaTranslator.Wpf.csproj
```

## ⚙️ Configuration

The application uses two main configuration files located in the `config/` directory:

### Settings.json
Contains application settings including:
- Timer intervals
- Home team configuration
- Sports settings
- Display teams list
- XML-to-JSON conversion settings

### NcaaNameConverter.json
Contains name mappings for:
- Team translations (6-char codes to display names)
- Conference name translations

## 🚀 Usage

### Getting Started

1. **Launch the Application**: Run `NcaaTranslator.Wpf.exe`
2. **Initial Setup**: The app starts automatically and begins fetching data
3. **Configure Sports**: Go to Settings → Sports tab to enable desired sports
4. **Set Home Team**: Configure your favorite team in Settings → General

### Main Interface

The main tab displays live game data organized by sport:

- **Sport Expanders**: Click to expand/collapse sport details
- **Game Information**: Home/Away teams, scores, and game clock
- **Status Indicators**: Running/Stopped status and last update time

### Basic Workflow

1. **Enable Sports**: In Settings → Sports, check "Enabled" for sports you want to monitor
2. **Configure Display**: Choose game display mode (Live/All/Display)
3. **Start Monitoring**: Click "Start" to begin automatic data fetching
4. **Monitor Games**: View live scores and game status in the main tab

## 🖥️ User Interface

### Main Tab
- **Control Panel**: Start/Stop buttons and status information
- **Sports Display**: Expandable sections showing games by sport
- **Game Details**: Team names, scores, and game clocks

### Settings Tab

#### General Settings
- **Timer**: Set data refresh interval (5-300 seconds)
- **Home Team**: Select your primary team for special handling

#### Sports Configuration
- **Add/Remove Sports**: Manage which sports to monitor
- **Sport Details**: Name, code, division, week, conference
- **Game Lists**: Enable conference, non-conference, and top-25 games
- **Display Mode**: Choose how games are filtered
- **OOS Updates**: Configure XML template updates

#### Display Teams
- **Team Management**: Add teams for "Display" mode filtering
- **Search**: Find teams quickly by name

#### XML to JSON
- **Conversion Toggle**: Enable/disable XML-to-JSON conversion
- **File Paths**: Configure input XML file locations

### Name Converters Tab

#### Teams
- **Search**: Filter teams by name or code
- **Edit Display Names**: Customize how team names appear
- **6-Character Codes**: NCAA standard team identifiers

#### Conferences
- **Search**: Filter conferences by name
- **Custom Names**: Override default conference names

## 🔧 Settings

### Timer Configuration
```json
"Timer": 20
```
- Range: 5-300 seconds
- Default: 20 seconds
- Controls how often data is fetched from NCAA APIs

### Home Team Setup
```json
"HomeTeam": "NO DAK"
```
- Uses 6-character NCAA team code
- Affects game categorization and display

### Sports Configuration
Each sport can be configured with:
- **Enabled**: Whether to monitor this sport
- **Conference**: Associated conference for filtering
- **Division/Week**: NCAA API parameters
- **Lists Needed**: Which game categories to include
- **OOS Settings**: XML template update configuration

### Display Teams
```json
"DisplayTeams": [
  {
    "NcaaTeamName": "UVA"
  }
]
```
Teams listed here are prioritized in "Display" mode.

## 🏷️ Name Converters

### Team Name Translation
The application maintains a mapping between NCAA's 6-character team codes and display names:

```json
{
  "name6Char": "NODAK",
  "customName": "North Dakota",
  "seoname": "north-dakota",
  "nameShort": "Fighting Hawks"
}
```

### Conference Translation
Similar mapping for conference names:

```json
{
  "conferenceSeo": "summit-league",
  "customConferenceName": "Summit League"
}
```

## 🔍 Troubleshooting

### Common Issues

#### Application Won't Start
- Ensure .NET 8.0 Desktop Runtime is installed
- Check Windows Event Viewer for error details
- Verify config files are present and valid JSON

#### No Data Appearing
- Check internet connection
- Verify sports are enabled in settings
- Confirm NCAA APIs are accessible
- Check timer is running (Status: Running)

#### Settings Not Saving
- Ensure write permissions to config directory
- Check JSON syntax in configuration files
- Restart application after manual config changes

#### OOS Updates Not Working
- Verify file paths exist and are writable
- Check OOS settings are properly configured
- Ensure XML templates are in correct format

### Debug Mode
- Enable additional logging by checking the console output
- Use the console application version for command-line debugging

### Logs and Diagnostics
- Application logs errors silently by default
- Check Windows Event Viewer for system-level errors
- Manual config file validation using JSON validators

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Setup
```bash
# Clone and setup
git clone https://github.com/yourusername/NcaaTranslator.git
cd NcaaTranslator

# Build all components
dotnet build NcaaTranslator.sln

# Run tests
dotnet test

# Debug WPF app
dotnet run --project src/NcaaTranslator.Wpf/
```

## 🙏 Acknowledgments

- NCAA for providing sports data APIs
- .NET community for excellent development tools
- WPF for robust desktop application framework

---

**Note**: This application is not officially affiliated with the NCAA. Please ensure compliance with NCAA data usage policies.