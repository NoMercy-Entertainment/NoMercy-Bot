# TTS Provider System Implementation Plan

## Overview
Refactor existing TTS system to support multiple providers (starting with Azure) while maintaining backward compatibility and implementing character usage tracking with configurable billing limits.

## Database Schema Changes

### New Tables

#### TtsProviders
- Id (ULID, Primary Key)
- Name (string) - "Azure", "Legacy", etc.
- Type (string) - Provider type identifier
- IsEnabled (bool)
- Priority (int) - Fallback order
- CreatedAt/UpdatedAt (DateTime)

#### TtsUsageRecords
- Id (ULID, Primary Key)
- ProviderId (ULID, Foreign Key)
- CharactersUsed (int)
- BillingPeriodStart (DateTime)
- BillingPeriodEnd (DateTime)
- CreatedAt (DateTime)

### Configuration Entries (snake_case)
- `tts_azure_api_key` (SecureValue) - Azure API key
- `tts_azure_region` (Value) - Azure region
- `tts_azure_voice_name` (Value) - Default Azure voice
- `tts_billing_cycle_start_day` (Value) - Day of month billing starts (1-28)
- `tts_billing_cycle_length_days` (Value) - Length of billing cycle in days
- `tts_azure_character_limit` (Value) - Max characters per billing cycle
- `tts_allow_user_voice_selection` (Value) - Enable user voice preferences
- `tts_fallback_on_limit` (Value) - Fall back to legacy when limit reached
- `tts_temporary_override_active` (Value) - Temporary override enabled

## Code Structure

### 1. Provider Abstraction Layer
Create `ITtsProvider` interface with standardized methods:
- `Task<byte[]> SynthesizeAsync(string text, string voiceId, CancellationToken cancellationToken)`
- `Task<bool> IsAvailableAsync()`
- `Task<int> GetCharacterCountAsync(string text)`
- `Task<List<TtsVoice>> GetAvailableVoicesAsync()`

### 2. Provider Implementations
- `LegacyTtsProvider` - Wraps existing localhost:6040 system
- `AzureTtsProvider` - New Azure Cognitive Services implementation
- Future providers follow same pattern

### 3. Usage Tracking Service
`ITtsUsageService` to handle:
- Character counting and billing period management
- Limit checking and enforcement
- Temporary override handling
- Usage reporting

### 4. Provider Selection Service
`ITtsProviderService` to handle:
- Provider selection based on availability and limits
- Fallback logic when providers fail or hit limits
- Configuration management

### 5. Refactored TTS Service
Updated `TtsService` to:
- Use provider abstraction instead of direct API calls
- Implement usage tracking
- Handle provider selection and fallbacks
- Maintain existing public interface for backward compatibility

## Implementation Steps

### Step 1: Create Provider Infrastructure ✅ COMPLETED
**Status**: ✅ IMPLEMENTED
**Files Created**:
- `src/NoMercyBot.Services/TTS/Interfaces/ITtsProvider.cs`
- `src/NoMercyBot.Services/TTS/Interfaces/ITtsUsageService.cs`
- `src/NoMercyBot.Services/TTS/Interfaces/ITtsProviderService.cs`
- `src/NoMercyBot.Services/TTS/Models/TtsVoice.cs`
- `src/NoMercyBot.Services/TTS/Providers/TtsProviderBase.cs`
- `src/NoMercyBot.Services/TTS/Providers/LegacyTtsProvider.cs`
- `src/NoMercyBot.Services/TTS/Services/TtsUsageService.cs`
- `src/NoMercyBot.Services/TTS/Services/TtsProviderService.cs`
- `src/NoMercyBot.Services/TTS/Services/TtsProviderInitializationService.cs`
- `src/NoMercyBot.Database/Models/TtsProvider.cs`
- `src/NoMercyBot.Database/Models/TtsUsageRecord.cs`

**Database Changes**:
- ✅ Migration created and applied: `AddTtsProviderSystem`
- ✅ Added `TtsProviders` and `TtsUsageRecords` tables
- ✅ Updated `AppDbContext` with new DbSets

**Key Features Implemented**:
1. ✅ ITtsProvider interface with standardized methods
2. ✅ TtsProviderBase abstract class with common functionality
3. ✅ LegacyTtsProvider wrapping existing localhost:6040 system
4. ✅ Provider registration system in ServiceCollectionExtensions
5. ✅ Usage tracking service with billing period management
6. ✅ Provider service with selection and fallback logic
7. ✅ Refactored TtsService to use new provider abstraction
8. ✅ Provider initialization hosted service

### Step 2: Azure Provider Implementation ✅ COMPLETED
**Status**: ✅ IMPLEMENTED
**Files Created**:
- `src/NoMercyBot.Services/TTS/Providers/AzureTtsProvider.cs`

**Dependencies Added**:
- ✅ Microsoft.CognitiveServices.Speech NuGet package installed

**Key Features Implemented**:
1. ✅ Azure Cognitive Services Speech SDK integration
2. ✅ SSML generation for voice selection
3. ✅ Configuration-based API key and region management
4. ✅ Voice availability detection and fallback
5. ✅ Default voice selection (en-US-JennyNeural)
6. ✅ Proper resource disposal and error handling
7. ✅ Character counting for usage tracking
8. ✅ Priority-based provider selection (Azure priority 1, Legacy priority 999)

### Step 3: Usage Tracking System ✅ COMPLETED
**Status**: ✅ IMPLEMENTED

**Key Features Implemented**:
1. ✅ TtsUsageService with character counting and billing cycles
2. ✅ Configurable billing periods (start day, cycle length)
3. ✅ Character limit enforcement per provider
4. ✅ Temporary override functionality
5. ✅ Database usage recording with billing period tracking
6. ✅ Remaining character calculation

### Step 4: Provider Selection Logic ✅ COMPLETED
**Status**: ✅ IMPLEMENTED

**Key Features Implemented**:
1. ✅ TtsProviderService for provider management
2. ✅ Priority-based provider selection (lower number = higher priority)
3. ✅ Character limit checking before synthesis
4. ✅ Automatic fallback when limits exceeded
5. ✅ Provider availability checks
6. ✅ Configuration-driven fallback behavior

### Step 5: Service Integration ✅ COMPLETED
**Status**: ✅ IMPLEMENTED

**Key Features Implemented**:
1. ✅ Refactored TtsService to use provider abstraction
2. ✅ Maintained backward compatibility with existing API
3. ✅ Character counting before synthesis
4. ✅ Usage tracking integration
5. ✅ Provider information included in widget events
6. ✅ Proper dependency injection patterns

### Step 6: User Voice Preferences ✅ COMPLETED
**Status**: ✅ IMPLEMENTED
**Current State**: Provider-aware voice selection with cross-provider fallback support

**Key Features Implemented**:
1. ✅ Extended TTSVoiceController to support provider-specific voices
2. ✅ Updated GetVoices API to aggregate voices from all providers with optional provider filtering
3. ✅ Enhanced SetUserVoice API to handle both legacy and provider-specific voice formats
4. ✅ Implemented cross-provider voice fallback in TtsService
5. ✅ Added comprehensive TTS configuration endpoints in ServiceController
6. ✅ Provider-aware voice storage using "provider:voiceId" format
7. ✅ Backward compatibility with existing legacy voice preferences

**API Endpoints Enhanced**:
- `GET /api/tts/voices` - Returns voices from all providers or filtered by provider
- `GET /api/tts/providers` - Returns all TTS providers with availability status
- `POST /api/tts/voice` - Sets user voice preference with provider awareness
- `POST /api/settings/providers/tts-character-limit` - Configure character limits per provider
- `POST /api/settings/providers/tts-billing-cycle` - Configure billing cycle settings
- `POST /api/settings/providers/tts-fallback-settings` - Configure fallback behavior
- `POST /api/settings/providers/tts-temporary-override` - Enable/disable temporary overrides

**Voice Selection Logic**:
- User selects voice from any available provider
- Voice preference stored as "provider:voiceId" for provider voices or plain ID for legacy
- TtsService checks provider availability before using preferred voice
- Automatic fallback to random legacy voice if preferred provider unavailable
- Maintains backward compatibility with existing voice preferences

## System Status: ✅ FULLY IMPLEMENTED

### What's Working Now
1. **Multi-Provider Architecture**: Azure and Legacy providers working side-by-side
2. **Character Usage Tracking**: Full billing cycle management with configurable limits
3. **Automatic Fallback**: Seamless fallback from Azure to Legacy when limits exceeded
4. **Provider Selection**: Priority-based provider selection with availability checks
5. **User Voice Preferences**: Cross-provider voice selection with fallback support
6. **Configuration Management**: Complete API endpoints for all TTS settings
7. **Backward Compatibility**: Existing TTS functionality unchanged for end users

### Configuration Required for Azure TTS
To enable Azure TTS, configure these settings via the API endpoints:
- **API Key**: `POST /api/settings/providers/azure-tts-api-key`
- **Region**: `POST /api/settings/providers/azure-tts-region` 
- **Character Limit**: `POST /api/settings/providers/tts-character-limit`
- **Billing Cycle**: `POST /api/settings/providers/tts-billing-cycle`
- **Fallback Settings**: `POST /api/settings/providers/tts-fallback-settings`

### Default Behavior
- **Without Azure configuration**: System uses Legacy provider only
- **With Azure configured**: Azure gets priority (1), Legacy as fallback (999)
- **When limits exceeded**: Automatic fallback to Legacy if enabled
- **Voice selection**: Users can choose voices from any available provider
- **Billing tracking**: Character usage tracked per provider per billing cycle

### Next Steps (Optional Enhancements)
1. **Usage Dashboard**: Add API endpoints to retrieve usage statistics and billing information
2. **Provider Management UI**: Create frontend interfaces for managing TTS provider settings
3. **Voice Testing**: Add API endpoint to test voice synthesis before setting preferences
4. **Additional Providers**: Extend system to support Google Cloud TTS, Amazon Polly, etc.
5. **Advanced Analytics**: Track popular voices, usage patterns, and provider performance

The TTS Provider System is now **production-ready** with full multi-provider support, usage tracking, and configuration management!
