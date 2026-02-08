# Progress Path

A real-time student progress tracking application that enables teachers to monitor student group progress through AI-guided learning conversations.

## Assignment

This application was created as a prototype for **real-time tracking of student group progress** by a teacher. The core requirements were:

- Teacher authentication via Google OAuth with role-based access control
- Group creation with natural language goal descriptions
- QR code-based student joining (one device per group)
- AI-guided text chat that helps students achieve learning goals
- Two types of progress tracking:
  - **Binary goals** (completed/not completed) — displayed as a checkmark
  - **Percentage goals** (e.g., "solve 3 equations") — displayed as a progress bar
- Real-time teacher dashboard with student progress monitoring
- Alert system for off-topic behavior and inactivity
- Support for mathematical expressions and code snippets
- Voice dictation support for students

## Implementation Highlights

### AI-Powered Goal Interpretation

When a teacher creates a group with a goal description like *"independently solve 3 different quadratic equations using the discriminant"*, the LLM automatically:
- Determines the goal type (binary or percentage-based)
- Creates a structured welcome message for students
- Generates initial guidance for the AI tutor

### Multi-Provider LLM Support

Built with [LLMTornado](https://github.com/lofcz/LLMTornado), the application supports multiple LLM providers:
- OpenAI (GPT-4, GPT-4o)
- Anthropic (Claude)
- Google (Gemini)
- Groq
- And more...

### Real-Time Communication

SignalR powers all real-time features:
- Instant progress updates on teacher dashboard
- Live chat message synchronization
- Multi-tab support for students
- Alert notifications for teachers

### Two-Step Alert Escalation

1. **First off-topic message**: Warning shown to student
2. **Subsequent off-topic messages**: Alert sent to teacher
3. **Inactivity (5 min)**: Warning message to student
4. **Inactivity (10 min)**: Alert sent to teacher

### Progress Tracking

- Progress only increases (never decreases)
- Progress-contributing messages are highlighted with green borders
- Chat input is disabled upon goal completion
- Teachers can view key messages that contributed to progress

## Technology Stack

| Category | Technology |
|----------|------------|
| **Backend** | .NET 8.0, Blazor Server, Entity Framework Core |
| **Database** | SQL Server (Azure SQL Edge for ARM64) |
| **Real-time** | SignalR |
| **AI Integration** | LLMTornado 3.8.46 |
| **Frontend** | TypeScript 5.7, Tailwind CSS 4.0 |
| **Authentication** | Google OAuth 2.0 |
| **QR Codes** | QRCoder 1.6.0 |
| **Deployment** | Docker, Azure App Service |

## Architecture Overview

```
ProgressPath/
├── Components/
│   ├── Pages/
│   │   ├── Teacher/          # Dashboard, group management
│   │   └── Student/          # Chat interface, join flow
│   ├── Shared/               # Reusable components
│   └── Layout/               # App layout, navigation
├── Controllers/              # OAuth endpoints
├── Data/                     # DbContext, migrations
├── Hubs/                     # SignalR hub
├── Models/                   # Entities and DTOs
├── Services/                 # Business logic
│   ├── LLMService.cs         # AI integration
│   ├── ChatService.cs        # Chat handling
│   ├── GroupService.cs       # Group management
│   ├── AlertService.cs       # Alert system
│   └── InactivityMonitorService.cs  # Background monitoring
└── wwwroot/
    └── ts/                   # TypeScript modules
```

## Getting Started

### Prerequisites

- [Docker](https://www.docker.com/get-started) and Docker Compose
- An LLM API key (OpenAI, Anthropic, etc.)
- (Optional) Google OAuth credentials for production use

### Local Development Setup

1. **Clone the repository**

   ```bash
   git clone https://github.com/your-username/scio--progress-path.git
   cd scio--progress-path
   ```

2. **Create environment file**

   ```bash
   cp .env.example .env
   ```

3. **Configure environment variables**

   Edit `.env` and set the following:

   ```env
   # SQL Server password (must meet complexity requirements)
   SA_PASSWORD=YourStrong!Passw0rd
   
   # LLM Configuration
   LLM__Provider=openai
   LLM__Model=gpt-4o
   LLM__ApiKey=your-api-key-here
   
   # For testing without Google OAuth
   DevAuth__Enabled=true
   ```

4. **Start the application**

   ```bash
   docker compose up --build
   ```

5. **Access the application**

   Open [http://localhost:5001](http://localhost:5001) in your browser.

### Development with VS Code

The project includes pre-configured VS Code tasks for common operations:

| Task | Description |
|------|-------------|
| `Docker Start` | Start the development environment |
| `Docker Start (Background)` | Start in background mode |
| `Docker Stop` | Stop containers |
| `Docker Stop & Clean` | Stop and remove volumes |
| `Docker Logs` | View container logs |
| `Docker Rebuild` | Rebuild and restart |

Access via **Terminal → Run Task** or `Cmd+Shift+P` → "Tasks: Run Task".

### Database Access

Query the SQL Server database directly:

```bash
docker run --rm -it --network scio--progress-path_progresspath-network \
  mcr.microsoft.com/mssql-tools /opt/mssql-tools/bin/sqlcmd \
  -S progresspath-db -U sa -P 'YourStrong!Passw0rd' -d ProgressPath \
  -Q "SELECT * FROM Groups"
```

## Usage

### For Teachers

1. **Login** using Google OAuth (or Dev Login in development mode)
2. **Create a group** with a name and goal description
3. **Review the AI interpretation** of your goal
4. **Share the QR code** with students
5. **Monitor progress** in real-time on the dashboard
6. **Respond to alerts** when students need help

### For Students

1. **Scan the QR code** or enter the join code
2. **Enter your nickname** to join the group
3. **Work through the goal** via AI-guided chat
4. **Track your progress** via the progress indicator
5. **Complete the goal** to finish the session

## Production Deployment

### Azure App Service

The project includes deployment scripts in `.azure/`:

```bash
# Full deployment (with health checks)
.azure/deploy.sh

# Quick deployment (skip stop/start)
.azure/soft-deploy.sh

# Check Azure Free tier quota
.azure/check-quota.sh
```

### Required Azure Configuration

Set these application settings in Azure App Service:

- `ConnectionStrings__DefaultConnection`
- `LLM__Provider`, `LLM__Model`, `LLM__ApiKey`
- `GoogleAuth__ClientId`, `GoogleAuth__ClientSecret`
- `Application__BaseUrl`

## Environment Variables

| Variable | Description | Required |
|----------|-------------|----------|
| `SA_PASSWORD` | SQL Server SA password | Yes (local) |
| `LLM__Provider` | LLM provider (openai, anthropic, etc.) | Yes |
| `LLM__Model` | Model name (gpt-4o, claude-3-sonnet) | Yes |
| `LLM__ApiKey` | API key for LLM provider | Yes |
| `GoogleAuth__ClientId` | Google OAuth client ID | Production |
| `GoogleAuth__ClientSecret` | Google OAuth client secret | Production |
| `DevAuth__Enabled` | Enable dev login button | No |
| `Application__BaseUrl` | Base URL for QR codes | Yes |

## Key Features

- **Real-time progress tracking** with SignalR
- **AI-guided learning** with multi-provider LLM support
- **Automatic goal interpretation** from natural language
- **QR code joining** for easy student access
- **Multi-tab synchronization** for students
- **Alert system** for off-topic and inactive students
- **Mathematical expression rendering** (LaTeX)
- **Code snippet rendering** with syntax highlighting
- **Voice dictation** for accessibility
- **Responsive design** with animated particle background

## License

This project was created as an assignment submission.
