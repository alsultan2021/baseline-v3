# Baseline.AI.Admin

Admin UI module for managing Baseline.AI configuration from the Xperience by Kentico administration interface.

## Overview

This module provides a user-friendly admin interface for configuring AI features without requiring code changes. Settings are stored in the database and merged with `appsettings.json` defaults at runtime.

## Features

- **General Settings**: Enable/disable AI features (Vector Search, Chatbot, Auto-Tagging, Search Suggestions)
- **Chatbot Settings**: Customize branding (title, colors, messages, position)
- **Auto-Tagging Settings**: Configure taxonomy matching rules and thresholds
- **RAG Settings**: Fine-tune retrieval-augmented generation parameters
- **Auto-Installation**: Database tables are created automatically on first run - no SQL scripts required!

## Installation

### 1. Project Reference

Add a reference to `Baseline.AI.Admin` in your web project:

```xml
<ProjectReference Include="..\..\..\src\v3\AI\Baseline.AI.Admin\Baseline.AI.Admin.csproj" />
```

### 2. Service Registration

Register the admin services in your `Program.cs` or service extensions:

```csharp
using Baseline.AI.Admin;

// After AddBaselineAI(configuration)
services.AddBaselineAIAdmin();
```

### 3. Run the Application

On first application startup, the module will automatically:

1. Create the `XperienceCommunity.Baseline.AI` module resource in the CMS
2. Create the `BaselineAI_Settings` database table with all required columns
3. Insert default settings record with sensible defaults

**No manual SQL execution required!**

## Configuration Strategy

### Kept in appsettings.json (Sensitive/Infrastructure)

- `Provider` (OpenAI, Azure, Ollama, etc.)
- `ApiKey`
- `Endpoint`
- `EmbeddingModel`
- `ChatModel`
- `EmbeddingDimensions`

### Managed via Admin UI (Business/Editorial)

- Feature toggles (EnableVectorSearch, EnableChatbot, etc.)
- Chatbot branding (Title, ThemeColor, WelcomeMessage, etc.)
- Auto-tagging rules (MinConfidence, MaxTagsPerTaxonomy, etc.)
- RAG parameters (TopK, SimilarityThreshold, etc.)

## Admin UI Location

After installation, the Baseline AI settings will appear in the Xperience admin under:

**Baseline AI** (category) → **Baseline AI** (application)

- General Settings
- Chatbot Settings
- Auto-Tagging Settings
- RAG Settings

## Architecture

### Files

| File                                        | Purpose                                              |
| ------------------------------------------- | ---------------------------------------------------- |
| `BaselineAISettingsInfo.cs`                 | Database entity for storing settings                 |
| `BaselineAIAdminModule.cs`                  | Registers the admin module and triggers installation |
| `Installers/BaselineAIModuleInstaller.cs`   | Orchestrates module and database installation        |
| `Installers/BaselineAISettingsInstaller.cs` | Creates the DataClassInfo and database table         |
| `UIPages/BaselineAIApplication.cs`          | Main application page                                |
| `UIPages/BaselineAI*Settings.cs`            | Configuration edit pages                             |
| `Services/IBaselineAISettingsProvider.cs`   | Interface for settings retrieval                     |
| `Services/BaselineAISettingsProvider.cs`    | Merges DB settings with appsettings.json             |

### Auto-Installation Flow

```
Application Startup
       │
       ▼
BaselineAIAdminModule.OnInit()
       │
       ▼
ApplicationEvents.Initialized.Execute
       │
       ▼
IBaselineAIModuleInstaller.Install()
       │
       ├──► InstallModule() - Creates ResourceInfo
       │
       ├──► BaselineAISettingsInstaller.Install() - Creates DataClassInfo & table
       │
       └──► EnsureDefaultSettings() - Creates default settings record
```

### Settings Provider

The `IBaselineAISettingsProvider` service merges database settings with `appsettings.json`:

```csharp
public interface IBaselineAISettingsProvider
{
    BaselineAIOptions GetSettings();
    Task<BaselineAIOptions> GetSettingsAsync();
    void RefreshCache();
}
```

Settings are cached for 5 minutes. Call `RefreshCache()` to force a reload.

## Usage in Code

Inject `IBaselineAISettingsProvider` instead of `IOptions<BaselineAIOptions>` to get merged settings:

```csharp
public class MyService
{
    private readonly IBaselineAISettingsProvider _settingsProvider;

    public MyService(IBaselineAISettingsProvider settingsProvider)
    {
        _settingsProvider = settingsProvider;
    }

    public void DoSomething()
    {
        var settings = _settingsProvider.GetSettings();

        if (settings.EnableAutoTagging)
        {
            // Use settings.AutoTagging.MinConfidence, etc.
        }
    }
}
```

## CI/CD Considerations

The settings table supports Continuous Integration (CI) via Xperience's built-in CI features. Settings will be synchronized across environments when using `kxp-ci-store` and `kxp-ci-restore`.
