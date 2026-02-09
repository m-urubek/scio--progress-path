# Assignment

Create a prototype application for **real-time tracking of student group progress** by a teacher.

## Functionality

- **Registration / Login** via Google account, RBAC.
- After logging in, the user can create a new group and see a table overview of existing groups. When creating a group, the user only fills in its name (e.g. "A2 - quadratic equations 1") and a goal description (e.g. "independently solve 3 different quadratic equations of the type ax^2 + bx + c using the discriminant").
- A **QR code** is generated for the group, which anyone can use to join the group. One device can join a group only once (localStorage). After joining, the user enters their nickname (e.g. "Honza Novák").
- The user in the group works via a **text chat**. Upon joining, they are greeted with a message describing the goal they need to achieve.
- The user always sees their progress toward goal completion on the screen. Goals are of the type:
  - **"completed / not completed"** (e.g. "explains the difference between a linear and quadratic equation") — displayed as a checkmark.
  - **"completed %"** (e.g. "solve 3 equations") — completed 0%, 33%, 66%, 100% — displayed as a progress bar.
- The system guides the student toward solving tasks. If the student is not working, it first shows a **warning** (e.g. an indicator under a message that is not relevant to the goals), then **alerts the teacher** (described below).
- Messages that address a goal or increase progress are **highlighted** in the conversation, for example with a green border.
- The teacher can **monitor student progress in real time**, see their progress toward goal completion, and if a student needs help, the teacher sees an indicator which they can mark as resolved.
- The teacher can expand a **student detail** (inline on the page) showing key messages that led to progress on the assigned goals. This can also be displayed as task-solution pairs or as an aggregation of multiple messages that collectively solve a task.
- The chat interface for students supports rendering **mathematical expressions** and **code snippets**.
- The student can **dictate input by voice**.
- visually, it should have similar theme to /Users/mu/Coding/TherapistTemplate including the animated background

## Technology

- **Blazor Server, .NET Core 8/9/10**
- **SQL Server**
- **TypeScript, Sass/Tailwind**
- UI components — any library
- AI libraries — any but ideally https://github.com/lofcz/LLMTornado if feasible for this project

## Deployment & Local Development

- **Production deployment:** Azure App Service (Linux) via Azure CLI zip deployment (`az webapp deploy --type zip`).
- **Local development:** Docker Compose with two services — the Blazor Server app and SQL Server (Linux container with persistent volume).
- **VS Code tasks (`.vscode/tasks.json`):** Include tasks for:
  - Azure deployment: Build for Deployment, Prepare Package, Create Zip, Deploy to App Service, Full Deploy (`.azure/deploy.sh`), Soft Deploy (`.azure/soft-deploy.sh`), Check Quota, Restart App, Configure App Settings.
  - Docker local dev: Docker Start, Docker Start (Background), Docker Stop, Docker Stop & Clean (removes volumes), Docker Logs, Docker Rebuild.
- **Azure deployment scripts (`.azure/`):** `deploy.sh` (full deploy with login check, quota check, build, stop/start, health check), `soft-deploy.sh` (quick deploy without stop/start), `check-quota.sh` (Azure Free tier quota monitoring), `deploy-config.sh` (centralized config). `.azure/` directory in `.gitignore`.
- **Dockerfile:** Multi-stage build (SDK for build, ASP.NET runtime for final image).
- **`.env.example`:** Documents all required environment variables (SA password, LLM API key, Google OAuth credentials). Actual `.env` in `.gitignore`.
- Follow the deployment pattern from `/Users/mu/Coding/SuperSmashHoesV2` (VS Code tasks + `.azure/` scripts).

## Submission

- **Demo deployment on Azure App Service + GitHub repository.**