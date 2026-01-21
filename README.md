<div align="center">

# 🔴 BeloteEngine 🔴

**A modern, real-time multiplayer Belote card game implementation**

[![License:  MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.txt)
[![C#](https://img.shields.io/badge/C%23-12.0-239120?logo=csharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![SignalR](https://img.shields.io/badge/SignalR-Real--time-00ADD8?logo=microsoft)](https://dotnet.microsoft.com/apps/aspnet/signalr)

[Features](#-features) • [Quick Start](#-quick-start) • [Architecture](#%EF%B8%8F-architecture) • [API Documentation](#-api-documentation)

</div>

---

## 📖 About

BeloteEngine is a web-based implementation of the classic card game **Belote**, built in .NET. It features real-time multiplayer gameplay powered by SignalR and a robust game engine that faithfully implements Belote rules.

## ✨ Features

- 🎮 **Real-time Multiplayer** - Live gameplay using ASP.NET Core SignalR
- 🏗️ **Clean Architecture** - Clear separation of concerns with layered design
- 🌐 **Web-Based** - Play in your browser from any device, no installation required
- 🐳 **Docker Ready** - Easy deployment with Docker Compose

## 🚀 Quick Start

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/get-started) (optional, for containerized deployment)

### Running with Docker Compose

```bash
# Clone the repository
git clone https://github.com/sspeev/BeloteEngine.git
cd BeloteEngine

# Start the development environment
docker compose up dev

# Or start the production environment
docker compose up prod
```

The application will be available at `http://localhost:8081`

### Running Locally

```bash
# Clone the repository
git clone https://github.com/sspeev/BeloteEngine.git
cd BeloteEngine

# Restore dependencies
dotnet restore

# Run the API project
dotnet run --project src/BeloteEngine. Api
```

## 🏗️ Architecture

BeloteEngine follows **Clean Architecture** principles with clear separation of concerns:

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│   Client Web    │◄───►│    BeloteHub     │◄───►│  LobbyService   │
│  (TypeScript)   │     │    (SignalR)     │     │                 │
└─────────────────┘     └──────────────────┘     └────────┬────────┘
                                                           │
                                                           ▼
                                                  ┌──────────────────┐
                                                  │   GameService    │
                                                  │                  │
                                                  └──────────────────┘
```

### Project Structure

```
BeloteEngine/
├── 📁 src/
│   ├── 📁 BeloteEngine.Api/          # Web API & SignalR Hub
│   │   ├── Hubs/                     # Communication layer
│   │   │   └── BeloteHub. cs         # Real-time game hub
│   │   ├── Controllers/              # HTTP endpoints
│   │   └── Program.cs               # Application entry point
│   │
│   ├── 📁 BeloteEngine. Services/     # Business logic layer
│   │   ├── Contracts/                # Service interfaces
│   │   │   ├── ILobbyService.cs
│   │   │   └── IGameService.cs
│   │   └── Services/                 # Service implementations
│   │       ├── LobbyService. cs      # Lobby management
│   │       └── GameService. cs       # Game rules & logic
│   │
│   └── 📁 BeloteEngine.Data/         # Data layer
│       └── Entities/
│           ├── Models/               # Domain models
│           │   ├── Player.cs        # Player entity
│           │   ├── Card.cs          # Card representation
│           │   ├── Deck.cs          # Deck management
│           │   ├── Game.cs          # Game state
│           │   ├── Lobby.cs         # Lobby entity
│           │   └── Team.cs          # Team entity
│           └── Enums/                # Game enumerations
│               ├── Suit. cs          # Card suits
│               ├── Status.cs        # Player status
│               └── Announces.cs     # Trump announcements
│
├── 📁 wwwroot/                       # Static web assets
├── 🐳 compose.yaml                   # Docker Compose configuration
├── 📄 BeloteEngine. sln               # Solution file
└── 📜 LICENSE.txt                    # MIT License
```

## 🎯 Core Components

### 1. Hub Layer (`BeloteHub`)

**Responsibility:** Handle real-time client-server communication

- 🔌 Manages SignalR connections
- 🔀 Routes client requests to services
- 📡 Handles connection/disconnection events
- 👥 Manages SignalR groups for lobbies

**Key Methods:**
- `JoinLobby()` - Add player to lobby
- `LeaveLobby()` - Remove player from lobby
- `StartGame()` - Initiate game session
- `PlayCard()` - Process card plays
- `MakeBid()` - Handle bidding

### 2. Service Layer

#### 🏠 LobbyService (`ILobbyService`)

**Responsibility:** Manage lobby state and player sessions

- Player join/leave operations
- Lobby state management
- Game initiation logic
- Real-time updates via HubContext

**Key Features:**
- 🔒 Thread-safe operations
- 👤 Player limit enforcement (4 players max)
- 🎮 Multi-lobby support

#### 🎲 GameService (`IGameService`)

**Responsibility:** Implement Belote game rules and logic

- Game state management
- Card play validation
- Bidding system (Clubs ♣, Diamonds ♦, Hearts ♥, Spades ♠)
- Score calculation
- Turn management

### 3. Data Layer

**Domain Models:**
- **Lobby** - Game lobby with players and game state
- **Player** - User information, connection details, hand of cards
- **Card** - Suit, rank, value, and power
- **Deck** - Standard 32-card Belote deck
- **Game** - Complete game state, teams, current player
- **Team** - Two players per team, score tracking

## 🔄 Game Flow

### 1️⃣ Player Join Flow

```
Client → JoinLobby() → BeloteHub
                           ↓
                    LobbyService.JoinLobby()
                           ↓
                    Update Lobby State
                           ↓
            Notify All Players (LobbyUpdated)
```

### 2️⃣ Game Start Flow

```
Client → StartGame() → BeloteHub
                          ↓
                   Validate 4 Players
                          ↓
                   GameService.StartNewGame()
                          ↓
           Initialize Deck & Deal Cards
                          ↓
        Broadcast GameStarted to All Players
```

### 3️⃣ Bidding Phase Flow

```
Current Player → MakeBid() → BeloteHub
                                ↓
                        Validate Bid
                                ↓
                    Update Game State
                                ↓
                Next Player or Start Play
```

### 4️⃣ Card Play Flow

```
Client → PlayCard(card) → BeloteHub
                             ↓
                    GameService. PlayCard()
                             ↓
                Validate Move & Update State
                             ↓
        Broadcast CardPlayed to All Players
```

## 🎴 Belote Rules Implementation

### Card Values

| Rank | Trump Value | Non-Trump Value |
|------|------------|-----------------|
| J    | 20         | 2              |
| 9    | 14         | 0              |
| A    | 11         | 11             |
| 10   | 10         | 10             |
| K    | 4          | 4              |
| Q    | 3          | 3              |
| 8    | 0          | 0              |
| 7    | 0          | 0              |

### Game Phases

1. **Splitting** - First player splits the deck
2. **Dealing** - Cards are dealt (3-2-3 pattern)
3. **Bidding** - Players bid for trump suit or pass
4. **Playing** - Card play with trick-taking rules
5. **Scoring** - Team score calculation

### Winning Condition

First team to reach **151 points** wins! 

## 🛡️ Design Patterns

- **Dependency Injection** - All services registered with proper lifetimes
- **Observer Pattern** - Real-time updates via SignalR
- **Service Layer Pattern** - Clear separation of business logic

## 🔧 Configuration

### Service Registration (Program.cs)

```csharp
builder.Services.AddSignalR();

// Register services
builder.Services.AddSingleton<ILobbyService, LobbyService>();
builder.Services.AddSingleton<IGameService, GameService>();

// Map SignalR hub
app. MapHub<BeloteHub>("/belotehub");
```

## 🐳 Docker Configuration

### Development Environment

```bash
docker compose up dev
```

- Hot reload enabled
- Volume mounting for live code changes
- Debug logging

### Production Environment

```bash
docker compose up prod
```

- Optimized build
- Production logging
- Minimal container size

## 📚 API Documentation

### SignalR Hub Methods

#### Client → Server

- `JoinLobby(RequestInfoModel)` - Join a game lobby
- `LeaveLobby(LeaveRequestModel)` - Leave the current lobby
- `StartGame(int lobbyId)` - Host starts the game
- `MakeBid(BidModel)` - Make a trump bid
- `PlayCard(CardModel)` - Play a card from hand

#### Server → Client

- `LobbyUpdated(LobbyInfo)` - Lobby state changed
- `GameStarted(GameState)` - Game has begun
- `CardsDealt(List<Card>)` - Player receives cards
- `CardPlayed(GameState)` - Card was played
- `GameOver(GameResult)` - Game finished

## 🤝 Contributing

Contributions are welcome!   Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.

---

<div align="center">

**Made with ❤️ by [sspeev](https://github.com/sspeev)**

⭐ Star this repository if you find it helpful!

</div>
