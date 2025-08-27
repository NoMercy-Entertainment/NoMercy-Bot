# NoMercyBot Overlay Widget System

## Project Overview
Building a comprehensive overlay widget system for NoMercyBot that allows users to create, manage, and use custom stream overlay widgets with various web frameworks (Vue, React, Svelte, Angular, or vanilla HTML/CSS/JS).

## Database Migration Commands
- **Create Migration**: `dotnet ef migrations add --project src\NoMercyBot.Database\NoMercyBot.Database.csproj --context NoMercyBot.Database.AppDbContext <migration name>`
- **Apply Migration**: `dotnet ef database update --project src\NoMercyBot.Database\NoMercyBot.Database.csproj --context NoMercyBot.Database.AppDbContext --configuration Debug`

## System Requirements

### 1. Widget Storage & Structure
#### 1.1 Storage Location
- Widgets stored in user's app data folder (use AppFiles class to derive folder)
- Database tracks metadata for widgets and enabled/disabled state
- Metadata should suffice the goals (TBD based on implementation needs)

#### 1.2 Widget Structure
- One folder per widget containing:
  - `source/` folder (development files)
  - `dist/` folder (built files)
  - Widget metadata file
- Structure details to be determined during implementation

#### 1.3 Browser Source Integration
- Unique URL for each widget linking to index file
- Support for built widget or dev mode with Vite if needed
- No `.html` extension required in URLs

### 2. Event System & Communication
#### 2.1 Event Subscription
- Allow widgets to subscribe to specific events they need
- Examples for chat widget: message received, deleted, updated
- Message deletion occurs when users are banned/moderated

#### 2.2 Widget-to-Widget Communication
- Currently undecided - will ask for approval if needed during implementation

### 3. Management & API
#### 3.1 Configuration Interface
- Full configurability through dashboard (desktop/browser/phone)
- API endpoints for all operations
- Support for widget installation via ZIP upload or URL
- StackBlitz-style editor providing VS Code-like environment per widget

#### 3.2 Hot Reload System
- Changes committed through interface or external sources trigger events
- Corresponding widgets receive browser source refresh signals for OBS
- Use SignalR WebSockets (no polling)

#### 3.3 Security & Warnings
- No imposed limits on widget capabilities
- Warn users when actions have potential to break system unrecoverably

### 4. Additional Features
#### 4.1 Text-to-Speech System
- Azure TTS integration for browser sources
- Example use case: Bot reads snarky replies to user commands
- Needs browser source compatibility

## Architecture Decisions Needed

### ✅ DECIDED - High Priority Questions
1. **Widget Metadata Schema**: ✅ APPROVED
   - Id (ULID), Name, Description, Version, Framework, IsEnabled, CreatedAt/UpdatedAt, EventSubscriptions (JSON), Settings (JSON)

2. **Build System**: ✅ Option C - Hybrid approach
   - Support both pre-built widgets and source code with server building
   - Provides flexibility for different user skill levels

3. **URL Routing**: ✅ Option A - `/overlay/widgets/{widgetId}`
   - Clean, semantic URLs for browser sources

4. **Database Schema**: ✅ Option C - Standalone widget system  
   - New `Widgets` table, no user association (single provider user context)

### 💡 NEW IDEAS TO IMPLEMENT
- **Historical Data Support**: When widgets connect and request data, provide historical context (e.g., chat messages from current stream) to maintain state during hot-reloads
- **Managed Dev Servers**: Take control of dev servers for each widget instead of relying on user-managed instances. This enables multiple simultaneous widget development, integrates with StackBlitz-style editor, and solves detection issues. Download standalone Node.js binary if user doesn't have correct version (use AppFiles executable suffix for Windows compatibility)

### Medium Priority Questions
1. **Event Broadcasting Architecture**: 
   - Centralized event hub or direct widget subscriptions?
   - Event filtering server-side or client-side?

2. **Editor Implementation**: 
   - Embedded Monaco editor or iframe to external service?
   - File system synchronization approach?

3. **TTS Integration**: 
   - Separate widget or built-in system service?
   - Real-time or queued processing?

## Implementation Phases

### Phase 1: Foundation ✅ COMPLETED
- [x] Database schema for widget metadata
- [x] Basic file system structure for widgets
- [x] Core API endpoints (CRUD operations)
- [x] Basic widget serving (static files)

### Phase 2: Core Features ✅ COMPLETED
- [x] WebSocket/SignalR integration for events
- [x] Widget hot reload system
- [x] Basic widget templates (seeded defaults)
- [x] Browser source URL routing
- [x] Real-time Twitch chat integration

### Phase 3: Advanced Features (IN PROGRESS)
- [x] Framework-specific scaffolding (Vue, React, Svelte, Angular) 
- [ ] StackBlitz-style editor integration
- [ ] ZIP upload and URL import functionality
- [ ] Framework-specific build pipelines
- [ ] Development mode support
- [ ] **Generated TypeScript documentation for widget development** (events, endpoints, types)

### Phase 4: Polish & TTS
- [ ] Azure TTS integration
- [ ] Security warnings system
- [ ] Dashboard UI enhancements
- [ ] Documentation and examples

## Current Task Status
**Status**: Phase 3 - Advanced Features Implementation

**Next Steps**: 
1. ✅ ~~Decide on widget metadata schema~~ - COMPLETED
2. ✅ ~~Choose build system approach~~ - COMPLETED
3. ✅ ~~Design URL routing structure~~ - COMPLETED
4. ✅ ~~Create database migrations~~ - COMPLETED
5. ✅ ~~Implement foundation layer~~ - COMPLETED
6. ✅ ~~Implement SignalR event system~~ - COMPLETED
7. ✅ ~~Integrate with Twitch chat~~ - COMPLETED
8. **🎯 CURRENT: Implement framework-specific scaffolding**
9. Build development environment features
10. Add advanced widget management

## Notes
- Context window limitations require this document for reference
- All major architectural decisions must be confirmed before implementation
- System should integrate seamlessly with existing NoMercyBot infrastructure
- **For coding style rules and conventions, refer to MASTER_CODING_PRACTICES.md**
