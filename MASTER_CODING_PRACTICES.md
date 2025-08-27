# NoMercyBot Code Practices (MCP)
This document outlines the coding standards, architectural patterns, and best practices for the NoMercyBot

## Table of Contents
1. [Project Structure & Organization](#project-structure--organization)
2. [C# Coding Standards](#c-coding-standards)
3. [Database & Entity Framework Conventions](#database--entity-framework-conventions)
4. [API & Controller Patterns](#api--controller-patterns)
5. [Service Layer Architecture](#service-layer-architecture)
6. [Configuration Management](#configuration-management)
7. [Dependency Injection Patterns](#dependency-injection-patterns)
8. [File & Folder Naming Conventions](#file--folder-naming-conventions)
9. [Frontend/TypeScript Standards](#fronendtypescript-standards)
10. [Testing & Documentation](#testing--documentation)

## Project Structure & Organization

### Solution Architecture
The solution follows a multi-project architecture with clear separation of concerns:

```
src/
├── NoMercyBot.Globals/           # Top-level shared components (no dependencies on other projects)
├── NoMercyBot.Database/          # Data layer (Entity Framework, Models, Migrations)
├── NoMercyBot.Services/          # Business logic and service implementations
├── NoMercyBot.Api/              # REST API controllers and middleware
├── NoMercyBot.Server/           # Application host and startup configuration
└── NoMercyBot.Client/           # Frontend Vue.js application
```

### Project Dependency Rules
- **NoMercyBot.Globals**: Cannot depend on any other projects - only used for shared utilities
- **NoMercyBot.Database**: Can reference Globals only
- **NoMercyBot.Services**: Can reference Database and Globals
- **NoMercyBot.Api**: Can reference Services, Database, and Globals
- **NoMercyBot.Server**: Can reference all other projects as the composition root
- **NoMercyBot.Client**: Standalone frontend project

### Folder Structure Within Projects

#### Services Project Structure
```
NoMercyBot.Services/
├── Interfaces/                   # Service contracts and abstractions
├── {ProviderName}/              # Provider-specific implementations (Discord/, Twitch/, etc.)
├── {FeatureName}/               # Feature-specific services (TTS/, Widgets/, etc.)
│   ├── Interfaces/              # Feature-specific interfaces
│   ├── Models/                  # Feature-specific models/DTOs
│   ├── Providers/               # Provider implementations
│   └── Services/                # Service implementations
├── Other/                       # General services that don't fit specific categories
└── ServiceCollectionExtensions.cs  # DI registration
```

#### Database Project Structure
```
NoMercyBot.Database/
├── Models/                      # Entity Framework models
│   └── {FeatureName}/          # Grouped by feature when complex (e.g., ChatMessage/)
├── Migrations/                  # EF Core migrations
└── AppDbContext.cs             # Main database context
```

#### API Project Structure
```
NoMercyBot.Api/
├── Controllers/                 # REST API controllers
├── Dto/                        # Data transfer objects
├── Helpers/                    # API-specific helper classes
├── Middleware/                 # Custom middleware
└── Resources/                  # Localization and static resources
```

## C# Coding Standards

### Modern C# Syntax Requirements
- **Use collection expressions**: `List<string> items = [];` instead of `new List<string>()`
- **Target-typed new expressions**: `ConfigUpdateRequest config = new();` instead of `ConfigUpdateRequest config = new ConfigUpdateRequest()`
- **Collection initializers**: Use `[]` for empty collections instead of calling constructors
- **Collection expressions with values**: Use `["item1", "item2"]` instead of `new List<string> { "item1", "item2" }`
- **Array collection expressions**: Use `string[] items = ["a", "b", "c"];` instead of `new string[] { "a", "b", "c" }`
- **Using declarations**: Prefer `using FileStream scope = new("file.txt");` over using blocks when possible
- **Explicit type declarations**: Always declare explicit types for variables, fields, and properties - never use `var`

### Code Style Rules

#### Type Declarations and Collection Expressions
- **File-scoped namespaces**: Use `namespace NoMercyBot.Services.TTS;` instead of block syntax
- **Always use explicit types**: Never use `var` - always declare explicit types for all variables, fields, and properties
- **Using aliases for type clarity**: When working with similar types from different namespaces, use using aliases:
  ```csharp
  using DatabaseTtsVoice = NoMercyBot.Database.Models.TtsVoice;
  using ServicesTtsVoice = NoMercyBot.Services.TTS.Models.TtsVoice;
  ```
- **Variable declarations**: Always use explicit types for all variables:
  - ✅ `List<ITtsProvider> providers = await GetProvidersAsync();`
  - ✅ `Dictionary<string, string> settings = [];`
  - ✅ `ITtsProvider? provider = await GetProviderAsync();`
  - ✅ `string[] names = ["azure", "legacy"];`
  - ✅ `List<string> items = ["item1", "item2", "item3"];`
  - ✅ `List<SpeakerInfoJson> legacyVoices = json.FromJson<List<SpeakerInfoJson>>() ?? [];`
  - ✅ `ConfigUpdateRequest config = new();`
  - ❌ `var providers = await GetProvidersAsync();` (never use var)
  - ❌ `var legacyVoices = json.FromJson<List<SpeakerInfoJson>>() ?? [];` (never use var)
  - ❌ `var config = new ConfigUpdateRequest();` (never use var)
  - ❌ `new List<string>()` (use `[]` instead)
  - ❌ `new string[] { "a", "b" }` (use `["a", "b"]` instead)
- **Field declarations**: Always use explicit types for private fields:
  - ✅ `private readonly AppDbContext _dbContext;`
  - ✅ `private readonly List<ITtsProvider> _providers = [];`
  - ✅ `private readonly string[] _supportedTypes = ["azure", "legacy"];`
- **Property declarations**: Always use explicit types:
  - ✅ `public string Name { get; set; } = string.Empty;`
  - ✅ `public List<TtsVoice> Voices { get; set; } = [];`
  - ✅ `public string[] SupportedFormats { get; set; } = ["wav", "mp3"];`

#### Formatting Rules
- **Ternary operator formatting**: Always format complex ternary operators on new lines:
  ```csharp
  // ✅ Correct formatting
  DisplayName = !string.IsNullOrWhiteSpace(voice.DisplayName) 
      ? voice.DisplayName 
      : voice.Name,
  
  Region = voice.Locale.Contains('-') 
      ? voice.Locale.Split('-')[1].ToUpperInvariant() 
      : string.Empty,
  
  // ❌ Incorrect - single line for complex conditions
  DisplayName = !string.IsNullOrWhiteSpace(voice.DisplayName) ? voice.DisplayName : voice.Name,
  ```
- **Method parameter defaults**: Do not use unnecessary default values or parameters:
  - ✅ `Logger.Setup("Message", LogEventLevel.Information);` (when log level is needed)
  - ✅ `Logger.Setup("Message");` (when using default level)
  - ❌ `Logger.Setup("Message", LogEventLevel.Information);` (when Information is the default)
- **Collection initialization patterns**:
  - ✅ `List<string> empty = [];` (empty collection)
  - ✅ `List<string> items = ["a", "b", "c"];` (collection with values)
  - ✅ `Dictionary<string, int> map = [];` (empty dictionary)
  - ✅ `string[] array = ["first", "second"];` (array with values)
  - ❌ `new List<string>()` or `new List<string> { "a", "b" }`
  - ❌ `new string[] { "a", "b" }` or `new[] { "a", "b" }`
- **Null-conditional operators**: Use `?.` and `??` operators appropriately
- **String interpolation**: Prefer `$"text {variable}"` over string concatenation
- **Pattern matching**: Use modern pattern matching syntax where applicable

### Naming Conventions
- **Classes**: PascalCase (`TtsProviderService`, `ConfigController`)
- **Methods**: PascalCase (`GetAvailableVoicesAsync`, `UpdateConfig`)
- **Properties**: PascalCase (`IsEnabled`, `CreatedAt`)
- **Fields (private)**: camelCase with underscore prefix (`_dbContext`, `_serviceProvider`)
- **Parameters**: camelCase (`providerId`, `characterCount`)
- **Local variables**: camelCase (`userVoice`, `audioBytes`)
- **Constants**: PascalCase (`DnsServer`, `UserAgent`)
- **Interfaces**: PascalCase with "I" prefix (`ITtsProvider`, `IWidgetEventService`)

### Method Signatures
- **Async methods**: Always suffix with `Async` (`SynthesizeAsync`, `GetVoicesAsync`)
- **CancellationToken**: Include as last parameter with default value when possible
- **Task return types**: Use `Task<T>` for async methods returning values, `Task` for void
- **Nullable reference types**: Use `?` appropriately for nullable parameters and return types

## Database & Entity Framework Conventions

### Entity Models
- **File location**: `src/NoMercyBot.Database/Models/`
- **Naming**: Singular form matching table name (`TtsVoice`, `User`, `Configuration`)
- **Primary keys**: Use `[PrimaryKey(nameof(Id))]` attribute
- **Properties**: 
  - ID properties as `string Id { get; set; } = string.Empty;`
  - Use `[DatabaseGenerated(DatabaseGeneratedOption.None)]` for manual ID assignment
  - Navigation properties as `ICollection<RelatedEntity>` with `= [];` initialization

### JSON Serialization
- **Newtonsoft.Json**: Use `[JsonProperty("snake_case")]` attributes for API compatibility
- **Collections**: Initialize with `= [];` for required collections

### Migration Naming
- **Convention**: Descriptive names (`AddTtsProviderSystem`, `UpdateUserPreferences`)
- **Commands**: 
  - Create: `dotnet ef migrations add --project src\NoMercyBot.Database\NoMercyBot.Database.csproj --context NoMercyBot.Database.AppDbContext <MigrationName>`
  - Apply: `dotnet ef database update --project src\NoMercyBot.Database\NoMercyBot.Database.csproj --context NoMercyBot.Database.AppDbContext --configuration Debug`

### Configuration Storage
- **Table**: `Configurations` with `Key`, `Value`, and `SecureValue` columns
- **Key naming**: snake_case for consistency (`tts_azure_api_key`, `billing_cycle_start_day`)
- **Secure values**: Use `SecureValue` column for sensitive data (API keys, tokens)
- **Regular values**: Use `Value` column for non-sensitive configuration

## API & Controller Patterns

### Controller Structure
- **Inheritance**: Inherit from `ControllerBase` or custom `BaseController`
- **Attributes**: `[ApiController]`, `[Authorize]` (when authentication required), `[Route("api/[controller]")]`
- **Dependency injection**: Constructor injection only, no service locator pattern

### HTTP Method Conventions
- **GET**: Retrieve data (`GetVoices()`, `GetUserVoice()`)
- **POST**: Create new resources or trigger actions (`SetUserVoice()`, `Speak()`)
- **PUT**: Update existing resources (`UpdateConfig()`, `UpdateServiceStatus()`)
- **DELETE**: Remove resources

### Response Patterns
- **Success**: `Ok(data)` for successful operations with data
- **No content**: `NoContent()` for successful operations without response data
- **Not found**: `NotFound()` or `NotFoundResponse()` for missing resources
- **Bad request**: `BadRequest(message)` for invalid input
- **Unauthorized**: `Unauthorized()` for authentication failures

### Request/Response Models
- **Location**: `src/NoMercyBot.Api/Dto/` for shared DTOs
- **Inline classes**: Define request/response classes as nested classes when controller-specific
- **Naming**: Suffix with `Request`, `Response`, or `Dto` (`ConfigUpdateRequest`, `SetTtsVoiceDto`)

## Service Layer Architecture

### Interface Design
- **Location**: `{ProjectName}/Interfaces/` or `{ProjectName}/{Feature}/Interfaces/`
- **Naming**: Start with "I" prefix (`ITtsProvider`, `ITtsUsageService`)
- **Async patterns**: All service methods should be async where I/O is involved
- **Return types**: Use appropriate return types (`Task<T?>` for nullable results)

### Service Implementation
- **Constructor injection**: All dependencies via constructor parameters
- **No service locator**: Never resolve services from `IServiceProvider` or `IServiceScope` manually
- **Dispose pattern**: Implement `IDisposable` only when managing unmanaged resources

### Provider Pattern
For pluggable implementations (like TTS providers):
- **Base class**: Abstract base class with common functionality (`TtsProviderBase`)
- **Interface**: Common contract (`ITtsProvider`)
- **Implementations**: Specific providers (`AzureTtsProvider`, `LegacyTtsProvider`)
- **Registration**: Register all implementations and use `IEnumerable<IInterface>` for multi-provider scenarios

### Seeding System
For provider-based seeding systems:
- **Provider iteration**: Loop through all registered providers to get data
- **DTO pattern**: Create DTOs for standardized data insertion
- **Conversion methods**: Separate methods for converting provider data to database models
- **Error handling**: Handle provider failures gracefully without breaking entire seeding
- **Using aliases**: Use type aliases when working with multiple similar types from different namespaces

## Configuration Management

### Global Configuration
- **Location**: `src/NoMercyBot.Globals/Information/Config.cs`
- **Pattern**: Static class with static properties
- **Initialization**: Properties with default values
- **Types**: Use appropriate types (`int`, `bool`, `string?`, `KeyValuePair<string, T>`)

### Database Configuration
- **Storage**: `Configurations` table with key-value pairs
- **Access pattern**: Upsert pattern for updates:
```csharp
await _dbContext.Configurations.Upsert(new Configuration
{
    Key = "config_key",
    Value = value
})
.On(c => c.Key)
.WhenMatched((existing, incoming) => new Configuration
{
    Key = existing.Key,
    Value = incoming.Value
})
.RunAsync();
```

### Environment-Specific Settings
- **appsettings.json**: Base configuration
- **appsettings.Development.json**: Development overrides
- **User secrets**: For sensitive development data
- **Environment variables**: For production secrets

## Dependency Injection Patterns

### Service Registration
- **Location**: `ServiceCollectionExtensions.cs` in each project
- **Pattern**: Extension methods for logical grouping
- **Lifetime management**:
  - `AddSingleton<T>()`: For stateless services
  - `AddScoped<T>()`: For per-request services (mainly in API)
  - `AddTransient<T>()`: For lightweight, stateless services

### Registration Patterns
```csharp
// Core service registration
services.AddSingleton<ITtsUsageService, TtsUsageService>();

// Multiple implementations
services.AddSingleton<ITtsProvider, LegacyTtsProvider>();
services.AddSingleton<ITtsProvider, AzureTtsProvider>();

// Hosted services
services.AddHostedService<TtsProviderInitializationService>();

// Combined singleton and hosted service
services.AddSingletonHostedService<SomeService>();
```

### Constructor Injection Rules
- **Direct injection**: Inject specific dependencies, not service containers
- **Interface dependency**: Depend on interfaces, not concrete implementations
- **Multiple implementations**: Use `IEnumerable<IInterface>` when multiple implementations needed
- **Avoid factory patterns**: Unless absolutely necessary for complex object creation

## File & Folder Naming Conventions

### File Naming
- **C# files**: PascalCase matching class name (`TtsProviderService.cs`)
- **Interface files**: PascalCase with "I" prefix (`ITtsProvider.cs`)
- **Configuration files**: PascalCase (`appsettings.json`, `ServiceCollectionExtensions.cs`)
- **Frontend files**: kebab-case (`chat-overlay-widget.vue`)

### Folder Naming
- **C# projects**: PascalCase (`NoMercyBot.Services`, `TTS`)
- **Feature folders**: PascalCase (`Interfaces`, `Providers`, `Services`)
- **Frontend folders**: kebab-case (`src`, `components`, `stores`)

### Namespace Conventions
- **Root namespace**: Match assembly name (`NoMercyBot.Services`)
- **Nested namespaces**: Follow folder structure (`NoMercyBot.Services.TTS.Providers`)
- **No extra nesting**: Don't add namespace levels that don't exist as folders

## Frontend/TypeScript Standards

### Vue.js Conventions
- **Script syntax**: Always use `<script setup lang="ts">`
- **Import organization**: Group imports with blank lines (Vue imports, libraries, stores, components)
- **Component naming**: PascalCase for component names, kebab-case for files
- **Reactive references**: Use `ref<Type>()` syntax with explicit typing

### TypeScript Standards
- **Explicit typing**: Define interfaces for complex objects
- **Naming conventions**: 
  - camelCase for variables and functions
  - PascalCase for interfaces and types
  - kebab-case for CSS classes
- **Template structure**: Clean indentation, logical grouping, inline conditions where appropriate

### CSS/Styling
- **Framework**: Prefer Tailwind utility classes
- **Custom classes**: Use kebab-case (`custom-widget-style`)
- **Spacing**: Use custom values like `w-available` when standard utilities don't fit
- **Indentation**: Use tabs for consistency

## Testing & Documentation

### Code Documentation
- **XML documentation**: Use for public APIs and complex methods
- **Inline comments**: Explain business logic, not obvious code
- **README files**: Provide setup and usage instructions for complex features
- **Markdown documentation**: Use for architectural decisions and system design

### Testing Patterns
- **Unit tests**: Test business logic in isolation
- **Integration tests**: Test service interactions and database operations
- **API tests**: Test controller endpoints and request/response handling
- **File naming**: `{ClassUnderTest}Tests.cs` or `{FeatureName}Tests.cs`

## Terminal & Development Commands

### PowerShell Commands
- **All terminal operations**: Use PowerShell commands exclusively
- **Directory navigation**: Never change directory when working with C# code
- **Package management**: Use `dotnet add package` for NuGet packages
- **Build commands**: Use `dotnet build`, `dotnet run`, etc.

### Entity Framework Commands
- **Migration creation**: Full project path specification required
- **Database updates**: Include configuration specification
- **Rollback**: Use specific migration names when rolling back

## Development Workflow

### Code Changes
- **Ask before drastic changes**: Always confirm before making significant structural changes
- **Incremental updates**: Make small, focused changes rather than large refactoring
- **Validation**: Check for compilation errors after making changes
- **Testing**: Verify changes work as expected before committing

### Project Updates
- **Dependency updates**: Keep packages up to date but verify compatibility
- **Configuration changes**: Update both code and documentation
- **Database changes**: Always create migrations for schema changes
- **API changes**: Update documentation and client code simultaneously

---

This MCP document should be referenced for all development work on the NoMercyBot project to ensure consistency and maintainability across the codebase.
