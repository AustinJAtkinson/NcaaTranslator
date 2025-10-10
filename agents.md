# Agents

This document outlines the agents and components within the NcaaTranslator project.

## Overview

The NcaaTranslator is a .NET application designed to process and translate NCAA-related data, including names, scores, and scoreboard information. It fetches live game data from NCAA APIs, translates team and conference names, manages scoreboard displays, and updates output score templates for graphics systems.

**Note**: This documentation should be kept up to date with any changes to the codebase, including additions, modifications, or removals of agents and components.

## Key Components (Agents)

### NameConverter
- **Purpose**: Manages name conversion and translation for teams and conferences.
- **Location**: `src/NcaaTranslator.Library/NameConverter.cs`
- **Responsibilities**:
  - Loads team and conference name mappings from `NcaaNameConverter.json`.
  - Provides lookup methods for translating NCAA team names (using 6-character codes) to custom names.
  - Handles conference name translations.
  - Allows adding new teams or conferences dynamically and saves changes back to JSON.
  - Maintains dictionaries for efficient lookups.

### NcaaProcessor
- **Purpose**: Core processor for fetching, processing, and transforming NCAA data.
- **Location**: `src/NcaaTranslator.Library/NcaaProcessor.cs`
- **Responsibilities**:
  - Constructs URLs for NCAA API queries based on sport, season, week, or date.
  - Fetches JSON responses from NCAA endpoints asynchronously.
  - Fixes and translates team and conference names in contest data using NameConverter.
  - Categorizes games into conference, non-conference, home, display, and top-25 lists.
  - Updates Out-of-Score (OOS) XML templates with live game data for graphics display.
  - Converts XML files to JSON format.
  - Outputs processed data to JSON files and console logs.

### NcaaScoreboard
- **Purpose**: Data models representing NCAA scoreboard and contest information.
- **Location**: `src/NcaaTranslator.Library/NcaaScoreboard.cs`
- **Responsibilities**:
  - Defines structures for contests (games), teams, and data collections.
  - Includes properties for game state, scores, periods, clocks, and start times.
  - Provides computed properties for display clocks (e.g., handling pre-game, final, and in-progress states).
  - Supports serialization with JSON ignore conditions for null collections.
  - Represents various game lists: non-conference, conference, home, display, and top-25 games.

### OutScore
- **Purpose**: XML serialization models for graphics templates used in score displays.
- **Location**: `src/NcaaTranslator.Library/OutScore.cs`
- **Responsibilities**:
  - Defines classes for GFX template elements, including locations, sizes, fonts, and media.
  - Supports serialization of complex XML structures for visual elements like text, images, and animations.
  - Used by NcaaProcessor to update template files with dynamic game data (e.g., team names, scores, clocks).

### Settings
- **Purpose**: Configuration management for the application.
- **Location**: `src/NcaaTranslator.Library/Settings.cs`
- **Responsibilities**:
  - Loads and deserializes settings from `Settings.json`.
  - Manages lists of sports, display teams, and other configurations.
  - Provides static access to settings like timer intervals, home team, and XML-to-JSON conversion options.
  - Supports saving updated settings back to JSON.
  - Includes enums for game display modes (Live, All, Display) and classes for OOS updaters, lists needed, etc.

## Applications

### WPF Application
- **Location**: `src/NcaaTranslator.Wpf/`
- **Purpose**: Provides a graphical user interface for configuring and running the translator.
- **Key Features**:
  - MainWindow for user interaction.
  - Integrates with library components for real-time data processing.

### Console Application
- **Location**: `src/NcaaTranslator.Console/`
- **Purpose**: Command-line interface for batch processing and automated operations.
- **Key Features**:
  - Program.cs entry point for console-based execution.
  - Suitable for scheduled tasks or headless environments.

## Configuration

- **Settings.json**: Main configuration file containing sports, display teams, timer settings, and home team.
- **NcaaNameConverter.json**: Stores custom name mappings for teams and conferences.

## Dependencies

- Utilizes NuGet packages such as System.Text.Json (for JSON handling), System.Numerics.Vectors, System.Text.Encodings.Web, System.Threading.Tasks.Extensions, System.ValueTuple, System.Runtime.CompilerServices.Unsafe, and Newtonsoft.Json (for XML-to-JSON conversion).

## Architecture Notes

- The library uses asynchronous HTTP clients for API calls.
- Data flows from API fetch → name translation → categorization → output updates.
- Supports multiple sports with configurable divisions, weeks, and conference filters.
- OOS updates involve XML deserialization, data injection, and re-serialization.

## Future Enhancements

- Add more agents for advanced processing.
- Integrate AI-based name recognition.
- Expand support for additional NCAA data types.
- Improve error handling and logging.