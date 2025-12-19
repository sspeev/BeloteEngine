# Belote Card Game - Real-Time Multiplayer Server

A real-time multiplayer Belote card game built with ASP.NET Core SignalR, featuring a single lobby system and live game interactions.

## 🏗️ Project Architecture

### Overview
This project follows a clean architecture pattern with clear separation of concerns:
- **Hub Layer**: SignalR communication management
- **Service Layer**: Business logic and game rules
- **Models Layer**: Data transfer objects and domain entities
- **Singleton Pattern**: Single lobby instance for simplified game management

### Architecture Diagram

┌─────────────────┐ ┌──────────────────┐ ┌─────────────────┐ │ Client Web │◄──►│ BeloteHub │◄──►│ LobbyService │ │ (JavaScript) │ │ (SignalR) │ │ │ └─────────────────┘ └──────────────────┘ └─────────────────┘ │ │ │ ▼ │ ┌─────────────────┐ │ │ ILobby │ │ │ (Singleton) │ │ └─────────────────┘ │ ▼ ┌──────────────────┐ │ GameService │ │ │ └──────────────────┘

## 📁 Project Structure
BeloteGame/ 
├── 📁 Models/ # Data models and DTOs 
│ ├── Player.cs # Player entity 
│ ├── LobbyInfo.cs # Lobby state information 
│ ├── JoinResult.cs # Join operation result 
│ ├── Card.cs # Game card model 
│ ├── GameState.cs # Current game state 
│ └── Bid.cs # Bidding information 
│ ├── 📁 Services/ 
│ ├── 📁 Contracts/ # Service interfaces 
│ │ ├── ILobbyService.cs # Lobby management contract 
│ │ ├── IGameService.cs # Game logic contract 
│ │ └── ILobby.cs # Singleton lobby contract 
│ │ │ └── 📁 Implementations/ # Service implementations 
│ ├── LobbyService.cs # Lobby business logic 
│ ├── GameService.cs # Game rules and state 
│ └── Lobby.cs # Singleton lobby instance 
│ ├── 📁 Hubs/ # SignalR communication 
│ └── BeloteHub.cs # Real-time communication hub 
│ └── Program.cs # Application startup and DI


## 🎯 Core Components

### 1. Hub Layer (`BeloteHub`)
**Responsibility**: Handle real-time client-server communication
- Manages SignalR connections
- Routes client requests to appropriate services
- Handles connection/disconnection events
- Manages SignalR groups for lobby organization

**Key Methods**:
- `JoinLobby()` - Add player to lobby
- `LeaveLobby()` - Remove player from lobby
- `StartGame()` - Initiate game session
- `PlayCard(Card card)` - Process card plays
- `MakeBid(Bid bid)` - Handle bidding

### 2. Service Layer

#### LobbyService (`ILobbyService`)
**Responsibility**: Manage lobby state and player sessions
- Player join/leave operations
- Lobby state management
- Game initiation logic
- Connection handling
- Real-time notifications via HubContext

**Key Features**:
- Thread-safe operations with locking
- Single lobby instance management
- Player limit enforcement (4 players max)
- Disconnection handling

#### GameService (`IGameService`)
**Responsibility**: Implement Belote game rules and logic
- Game state management
- Card play validation
- Bidding system
- Score calculation
- Turn management

#### Lobby Singleton (`ILobby`)
**Responsibility**: Maintain single lobby instance
- Player collection management
- Game state tracking
- Thread-safe operations
- Lobby reset functionality

### 3. Models Layer
**Responsibility**: Define data structures and DTOs

#### Core Models:
- **Player**: User information and connection details
- **LobbyInfo**: Current lobby state for client updates
- **JoinResult**: Operation result with success/error information
- **Card**: Game card representation
- **GameState**: Complete game state information
- **Bid**: Bidding information and validation

## 🔄 Data Flow

### Player Join Flow
1. Client calls JoinLobby() → BeloteHub
2. BeloteHub → LobbyService.JoinLobby()
3. LobbyService updates Lobby singleton
4. LobbyService → HubContext.NotifyLobbyUpdate()
5. All lobby clients receive LobbyUpdated event

### Game Start Flow
1. Client calls StartGame() → BeloteHub
2. BeloteHub → LobbyService.StartGame()
3. LobbyService validates player count (4 players)
4. LobbyService → GameService.StartNewGame()
5. GameService creates new game state
6. All players receive GameStarted event with initial state

### Card Play Flow
1. Client calls PlayCard(card) → BeloteHub
2. BeloteHub → GameService.PlayCard()
3. GameService validates move and updates state
4. All players receive CardPlayed event with new state

## 🛡️ Key Design Patterns

### 1. Dependency Injection
- All services registered as singletons
- Clear separation of contracts and implementations
- Testable architecture with interface-based design

### 2. Singleton Pattern
- Single lobby instance shared across all connections
- Thread-safe operations with explicit locking
- Simplified state management without ID tracking

### 3. Observer Pattern (via SignalR)
- Real-time state synchronization
- Event-driven client updates
- Automatic notification system

### 4. Service Layer Pattern
- Business logic separated from communication logic
- Reusable services for different endpoints
- Clear responsibility boundaries

## 🔧 Configuration

### Service Registration (Program.cs)
```csharp
builder.Services.AddSignalR();

// Register singleton services
builder.Services.AddSingleton<ILobby, Lobby>();
builder.Services.AddSingleton<ILobbyService, LobbyService>();
builder.Services.AddSingleton<IGameService, GameService>();

// Map SignalR hub
app.MapHub<BeloteHub>("/belotehub");
