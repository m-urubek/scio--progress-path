# Product Requirements Document: Progress Path

## Overview

Progress Path is a real-time student progress tracking prototype that enables teachers to monitor student group learning activities. Teachers create learning groups with specific goals, students join via QR codes and work through AI-guided chat sessions, and teachers monitor progress in real-time through a dashboard.

The application uses an AI tutor to guide students toward completing learning goals without giving direct answers, tracks progress automatically, and alerts teachers when students need help (off-topic behavior or inactivity). The visual theme matches the TherapistTemplate with animated particle backgrounds, glow effects, and a pink/purple/teal color palette.

**Primary Users:**
- **Teachers:** Create groups, define learning goals, monitor student progress in real-time
- **Students:** Join groups via QR code, work through AI-guided chat to achieve learning goals

**Core Value Proposition:** Teachers gain immediate visibility into every student's progress during group exercises, replacing manual check-ins with automated AI-driven progress tracking and alerts.

---

## Requirements

### 1. Authentication & Authorization

**REQ-AUTH-001:** Users shall authenticate via Google OAuth 2.0.

**REQ-AUTH-002:** Upon first login, users shall be assigned the "Teacher" role by default.

**REQ-AUTH-003:** The system shall store user profile data (email, display name, profile picture) from Google.

**REQ-AUTH-004:** The system shall support two roles: "Teacher" and "Student".

**REQ-AUTH-005:** Teachers shall have permissions to: create groups, view all groups they created, monitor student progress, mark help requests as resolved.

**REQ-AUTH-006:** Students (anonymous, device-bound) shall have permissions to: join groups via QR code, participate in chat, view their own progress.

**REQ-AUTH-007:** Unauthenticated users accessing teacher-only routes shall be redirected to the login page.

---

### 2. Group Management

#### 2.1 Group Creation

**REQ-GROUP-001:** Teachers shall create groups by providing:
- Group name (e.g., "A2 - quadratic equations 1")
- Goal description in English (e.g., "independently solve 3 different quadratic equations of the type ax^2 + bx + c using the discriminant")

**REQ-GROUP-002:** Upon group creation, the system shall generate a unique alphanumeric join code (6-8 characters) and corresponding QR code.

**REQ-GROUP-003:** Teachers shall view a table listing all their groups with columns: name, creation date, active students count, overall progress.

**REQ-GROUP-004:** Goal descriptions shall be capped at 500 characters in the input field.

**REQ-GROUP-005:** Group names do not need to be unique. The join code is the unique identifier.

#### 2.2 Goal Interpretation & Teacher Confirmation

**REQ-GROUP-006:** After a teacher submits a goal description, the AI shall analyze it and determine:
- Goal type: binary ("completed / not completed") or percentage-based ("completed %")
- For percentage goals: the number of discrete steps required for 100% completion (target: 2-10 steps)

**REQ-GROUP-007:** The teacher shall see and explicitly confirm the AI's interpretation before students can join the group.

**REQ-GROUP-008:** The confirmation screen shall display:
- The interpreted goal type (binary or percentage)
- For percentage goals: the number of steps and what constitutes each step
- A sample welcome message the AI would show students
- Accept/Reject buttons to confirm or reject the interpretation

**REQ-GROUP-009:** If the teacher rejects the interpretation, they shall be able to re-specify or edit the goal description and resubmit. There is no limit on re-submission attempts.

**REQ-GROUP-010:** The group shall not be joinable by students until the teacher confirms the AI interpretation.

**REQ-GROUP-011:** If the AI API fails to return a valid interpretation, the system shall display a generic error message allowing the teacher to retry.

#### 2.3 QR Code & Join URL

**REQ-GROUP-012:** The QR code shall encode a URL in the format: `{BaseUrl}/join/{JoinCode}` where BaseUrl is configurable via application settings (e.g., `https://myapp.azurewebsites.net`).

**REQ-GROUP-013:** The join code shall be a short alphanumeric string (6-8 characters), unique per group.

#### 2.4 Group Joining

**REQ-GROUP-014:** Anyone shall join a group by scanning the QR code (no authentication required).

**REQ-GROUP-015:** Upon joining, the user shall enter a nickname (e.g., "Honza Novak").

**REQ-GROUP-016:** Nicknames must be unique within a group. If a student attempts to use a nickname already taken by another student in the same group, the system shall reject it and prompt them to choose a different nickname.

**REQ-GROUP-017:** Device-binding shall be enforced via localStorage: one device can join a specific group only once.

**REQ-GROUP-018:** If a device attempts to rejoin the same group, the system shall restore the previous session.

**REQ-GROUP-019:** A single device may join multiple different groups (each with its own session).

**REQ-GROUP-020:** Multiple browser tabs on the same device accessing the same group shall restore the same session and receive synchronized updates via SignalR.

**REQ-GROUP-021:** If a student accesses the group from a different device, they are treated as a new student and must enter a new nickname.

#### 2.5 LocalStorage & Session Recovery Limitations

**REQ-GROUP-022:** If a student clears browser data or uses a different browser, they are treated as a new student and must enter a new (different) nickname. The orphaned session remains visible to the teacher on the dashboard.

**REQ-GROUP-023:** LocalStorage device-binding can be bypassed by clearing browser data. This limitation is acceptable for the prototype.

**REQ-GROUP-024:** Orphaned sessions remain visible on the teacher dashboard indefinitely. No cleanup mechanism is required for the prototype.

#### 2.6 Group Permanence

**REQ-GROUP-025:** Groups are permanent once created. Teachers cannot edit, disband, or delete groups.

**REQ-GROUP-026:** Groups remain accessible and visible in the teacher's group list indefinitely.

---

### 3. Goal System

#### 3.1 Goal Types

**REQ-GOAL-001:** The system shall support two goal types:
- **Binary goal:** completed / not completed (displayed as checkmark)
- **Percentage goal:** progress tracked as percentage (displayed as progress bar, e.g., 0%, 33%, 66%, 100%)

**REQ-GOAL-002:** The AI shall parse the teacher's goal description and determine the appropriate goal type and sub-goals.

**REQ-GOAL-003:** For percentage goals, the AI shall determine the discrete steps (e.g., "solve 3 equations" = 3 steps, each worth ~33%). Target 2-10 steps; if more sub-tasks exist, group them into logical stages.

**REQ-GOAL-004:** If the AI determines a goal has only 1 step, it shall be automatically converted to a binary goal.

**REQ-GOAL-005:** For percentage goals, a hard minimum of 2 steps shall be enforced at the data layer.

#### 3.2 Progress Tracking

**REQ-GOAL-006:** Student progress shall be calculated and updated in real-time based on chat message analysis.

**REQ-GOAL-007:** The student shall always see their current progress indicator on the chat screen as a progress bar with percentage. Students do NOT see an itemized list of completed sub-tasks—only the aggregate percentage.

**REQ-GOAL-008:** Progress updates shall be persisted to the database immediately upon detection.

**REQ-GOAL-009:** Progress shall be tracked by total steps completed, regardless of the order in which the student completes them (order-agnostic).

**REQ-GOAL-010:** Progress determination is fully automated. Teachers cannot manually override or adjust student progress.

**REQ-GOAL-011:** Progress shall only increase, never decrease. Once a step is completed, it cannot be un-completed.

**REQ-GOAL-012:** For display, percentages shall be rounded to whole integers (e.g., 14%, 29%, 43% for a 7-step goal). Always show 0% for no progress and 100% for full completion.

#### 3.3 Goal Completion

**REQ-GOAL-013:** When a student reaches 100% progress or completes a binary goal, the system shall display a completion message.

**REQ-GOAL-014:** Upon goal completion, the chat input shall be disabled immediately—the student cannot send further messages.

**REQ-GOAL-015:** The student's completion state shall be visible to the teacher on the monitoring dashboard (100% progress bar or checkmark is sufficient visual distinction).

**REQ-GOAL-016:** Teachers shall retain full access to view chat history and progress details for completed students.

---

### 4. Student Chat Interface

#### 4.1 Chat Functionality

**REQ-CHAT-001:** Upon joining, the student shall receive a welcome message explaining the goal they need to achieve.

**REQ-CHAT-002:** Students shall communicate via text messages with the AI tutor.

**REQ-CHAT-003:** The AI tutor shall always respond in English. Goal descriptions are always provided in English.

**REQ-CHAT-004:** The AI shall guide students toward completing their assigned goals without giving direct answers.

**REQ-CHAT-005:** Messages shall be displayed in chronological order with timestamps.

#### 4.2 Message Classification & Highlighting

**REQ-CHAT-006:** The AI shall classify each student message for progress contribution and on-topic/off-topic status.

**REQ-CHAT-007:** Messages that contribute to goal progress shall be visually highlighted (e.g., green border).

**REQ-CHAT-008:** Messages deemed irrelevant to goals shall display a warning indicator visible only to the student.

**REQ-CHAT-009:** If a message contains any substantive goal-relevant content (even mixed with off-topic content), it shall be classified as on-topic.

#### 4.3 Rich Content Rendering

**REQ-CHAT-010:** The chat shall render mathematical expressions (LaTeX/MathML support via KaTeX or MathJax).

**REQ-CHAT-011:** The chat shall render code snippets with syntax highlighting.

#### 4.4 Voice Input

**REQ-CHAT-012:** Students shall have the option to dictate messages via voice using the browser-native Web Speech API.

**REQ-CHAT-013:** A microphone button shall toggle voice input mode, with visual indication of recording state (e.g., color change or animation).

**REQ-CHAT-014:** Transcribed text shall appear in the input field for review before sending.

**REQ-CHAT-015:** Voice dictation requires browser support (Chrome, Edge recommended). Unsupported browsers shall hide the dictation button entirely (no fallback message). Detection via `'webkitSpeechRecognition' in window || 'SpeechRecognition' in window`.

---

### 5. AI-Powered Guidance & Monitoring

#### 5.1 Student Guidance

**REQ-AI-001:** The AI shall analyze each student message to determine relevance to the assigned goal.

**REQ-AI-002:** The AI shall provide hints and guidance to help students progress without giving direct answers.

**REQ-AI-003:** The AI shall recognize when a student has demonstrated understanding or completed a task.

**REQ-AI-004:** The AI shall err on the side of giving credit—if a student demonstrates partial understanding, award the progress.

**REQ-AI-005:** For binary goals, mark as complete when the student demonstrates understanding of the core concept (even with informal or imperfect phrasing). Example: For "explains the difference between linear and quadratic equations", responses like "linear is straight line, quadratic is curved/parabola" or "quadratic has x² term" count as complete.

#### 5.2 Off-Topic Detection & Warning System

**REQ-AI-006:** A message shall only be classified as off-topic when it is clearly and entirely unrelated to the learning goal. When in doubt, classify as on-topic.

**REQ-AI-007:** Examples of on-topic: tangential questions, meta-questions about the subject, clarification requests, loosely related concepts, foundational concept questions. Examples of off-topic: "What's for lunch?", "Tell me a joke", completely unrelated topics like "What's the capital of France?"

**REQ-AI-008:** When a student sends an off-topic message:
- **First offense:** Display a warning indicator visible only to the student (no teacher alert).
- **Subsequent off-topic messages after warning (without returning on-topic):** Generate a help request alert for the teacher.

**REQ-AI-009:** The warning shall be displayed inline with or under the off-topic message.

**REQ-AI-010:** If a student returns to on-topic messages after receiving a warning, the off-topic warning count shall reset.

**REQ-AI-011:** Mixed messages containing both on-topic and off-topic content shall be classified as on-topic.

#### 5.3 Inactivity Detection

**REQ-AI-012:** The system shall detect student inactivity based on a fixed 10-minute timeout.

**REQ-AI-013:** The inactivity timer starts when the student joins the group (enters nickname and sees welcome message). After the first message, subsequent timers measure from the last message sent.

**REQ-AI-014:** The timer runs server-side based on timestamps, regardless of connection state. A disconnected student is still considered inactive if they haven't sent messages.

**REQ-AI-015:** When a student is inactive for 10 minutes, the system shall generate a one-time inactivity alert for the teacher.

**REQ-AI-016:** After the initial inactivity alert, no further inactivity alerts shall be sent until teacher resolves the alert.

**REQ-AI-017:** When a previously inactive student (who triggered an alert) sends a new message, the system shall notify the teacher via a transient toast notification (auto-dismissing after 4 seconds) with the student's nickname (e.g., "Honza has resumed activity").

#### 5.4 Alert Resolution & Repeated Alerts

**REQ-AI-018:** After a teacher resolves a help alert (off-topic or inactivity), the same student can trigger new help alerts if they exhibit the triggering behavior again.

**REQ-AI-019:** For off-topic alerts: after resolution, if the student goes off-topic again, the warning cycle restarts (warning first, then escalation to a new alert).

**REQ-AI-020:** For inactivity alerts: after resolution, if the student becomes inactive again for 10 minutes, a new inactivity alert is generated.

#### 5.5 LLM API Error Handling

**REQ-AI-021:** If the LLM API fails when processing a student message, the student's message shall still appear in the chat (persisted).

**REQ-AI-022:** The chat shall display an error message ("Sorry, I'm having trouble responding right now. Please try again in a moment.").

**REQ-AI-023:** The student may send another message to retry. The failed message receives no classification or progress update.

**REQ-AI-024:** The inactivity timer still resets when a message is sent, regardless of API failure (student did attempt to engage).

#### 5.6 LLM Context Management

**REQ-AI-025:** The AI shall maintain full conversation context when processing student messages to accurately track cumulative progress.

**REQ-AI-026:** Every LLM request shall include: the goal definition, current progress state (completed steps), and off-topic warning counter, ensuring the AI never loses track.

**REQ-AI-027:** If conversations grow very long (exceeding ~50 messages or ~8000 tokens of history), older messages may be summarized while preserving the critical state information.

**REQ-AI-028:** The AI shall determine which messages contribute to progress during message processing (pre-computed, not on-demand). Store a `ContributesToProgress` flag on each message record.

---

### 6. Teacher Dashboard

#### 6.1 Navigation

**REQ-DASH-001:** The groups list page shows all teacher's groups with name, creation date, active student count, and overall progress.

**REQ-DASH-002:** Clicking a group navigates to the single-group dashboard view.

**REQ-DASH-003:** No cross-group aggregated view is provided. Teachers view one group at a time.

#### 6.2 Real-Time Monitoring

**REQ-DASH-004:** Teachers shall view all students in a group with their current progress (visual progress bars/checkmarks).

**REQ-DASH-005:** Progress updates shall appear in real-time without page refresh (SignalR).

**REQ-DASH-006:** Students requiring help shall display a visible alert indicator.

**REQ-DASH-007:** Teachers shall mark help alerts as "resolved" to dismiss them.

**REQ-DASH-008:** The dashboard shall display a simple scrollable list of students, optimized for ~30 concurrent students.

**REQ-DASH-009:** Students with unresolved alerts shall be sorted to the top of the list.

#### 6.3 Alert Indicators

**REQ-DASH-010:** Inactivity alerts shall display as a distinct indicator (e.g., yellow/orange icon) with tooltip showing "Inactive for X minutes."

**REQ-DASH-011:** Off-topic alerts shall display as a distinct indicator (e.g., red icon) with tooltip showing "Off-topic messages."

**REQ-DASH-012:** Activity resumption notifications (toasts) shall display the student nickname and auto-dismiss after 4 seconds. These are informational only and require no teacher action.

**REQ-DASH-013:** Toast notifications shall appear when new alerts fire (to draw attention, in addition to the list sorting).

#### 6.4 Student Detail View

**REQ-DASH-014:** Teachers shall expand any student row inline to view detailed progress information.

**REQ-DASH-015:** The detail view shall show key messages that contributed to goal progress (task-solution pairs or aggregated message summaries). These are the messages flagged as `ContributesToProgress` during AI processing.

**REQ-DASH-016:** The detail view shall provide access to view full chat history for both in-progress and completed students.

**REQ-DASH-017:** The detail view shall work identically for in-progress and completed students.

---

### 7. Real-Time Communication

**REQ-RT-001:** All real-time updates (progress changes, new messages, alerts) shall use SignalR.

**REQ-RT-002:** The system shall handle connection drops gracefully using Blazor Server's built-in reconnection mechanism with a visible reconnection overlay.

**REQ-RT-003:** Upon reconnection, chat history shall be restored from the server.

**REQ-RT-004:** Optimistic UI updates shall be used where appropriate, with server confirmation.

**REQ-RT-005:** Students cannot send messages while disconnected (Blazor Server architecture).

---

### 8. Visual Design & Theme

#### 8.1 TherapistTemplate-Inspired Theme

**REQ-THEME-001:** The application shall use a visual theme similar to TherapistTemplate with:
- **Primary color:** #ff6b9d (pink)
- **Secondary color:** #c084fc (purple)
- **Tertiary color:** #22d3d8 (teal/cyan)
- **Accent color:** #a78bfa (violet)
- **Highlight color:** #f0abfc (light pink)

**REQ-THEME-002:** The dark theme shall be the default, with background color #0f0a1a and surface color #1a0f2e.

**REQ-THEME-003:** The application shall support only dark theme mode (no light mode toggle).

#### 8.2 Animated Background

**REQ-THEME-004:** The application shall feature an animated particle background using tsparticles (via JS interop from Blazor).

**REQ-THEME-005:** Particles shall be interactive, responding to mouse hover (grab mode, 140px distance) and click (push mode, 2 particles).

**REQ-THEME-006:** Particle colors shall use the theme palette (pink, purple, teal).

**REQ-THEME-007:** Particles shall be connected with subtle link lines that form triangles.

#### 8.3 UI Component Styling

**REQ-THEME-008:** Containers shall feature glow border effects (e.g., `0 0 15px rgba(34, 211, 216, 0.3)` for teal glow).

**REQ-THEME-009:** Hover states shall include glow animations (glow-pulse effect).

**REQ-THEME-010:** Buttons shall use gradient backgrounds (pink to purple) with glow shadows.

**REQ-THEME-011:** Typography shall use 'Outfit' for headings and 'Inter' for body text.

**REQ-THEME-012:** Text shall include gradient color effects for emphasis.

---

### 9. Deployment & Development

#### 9.1 VS Code Tasks (`.vscode/tasks.json`)

**REQ-DEPLOY-001:** A "Build for Deployment" task shall run `dotnet publish` in Release configuration.

**REQ-DEPLOY-002:** A "Prepare Deployment Package" task shall create a `deploy-package/` directory.

**REQ-DEPLOY-003:** A "Create Zip Package" task shall zip the deployment package into `deploy.zip`.

**REQ-DEPLOY-004:** A "Deploy to App Service" task shall deploy via `az webapp deploy --type zip`.

**REQ-DEPLOY-005:** A "Full Deploy" task shall execute `.azure/deploy.sh`.

**REQ-DEPLOY-006:** A "Soft Deploy" task shall execute `.azure/soft-deploy.sh` (skips app stop/start).

**REQ-DEPLOY-007:** A "Check Quota" task shall execute `.azure/check-quota.sh`.

**REQ-DEPLOY-008:** A "Restart App" task shall restart the App Service via `az webapp restart`.

**REQ-DEPLOY-009:** Azure App Service name and resource group shall be configurable via `.vscode/settings.json`.

#### 9.2 Azure Deployment Scripts (`.azure/`)

**REQ-DEPLOY-010:** `deploy-config.sh` — centralized Azure configuration.

**REQ-DEPLOY-011:** `deploy.sh` — full deploy with login check, quota check, build, stop/start, health check.

**REQ-DEPLOY-012:** `soft-deploy.sh` — quick deploy without stopping/starting.

**REQ-DEPLOY-013:** `check-quota.sh` — Azure Free tier quota monitoring.

**REQ-DEPLOY-014:** `.azure/` directory in `.gitignore`.

**REQ-DEPLOY-015:** `deploy-package/` and `deploy.zip` in `.gitignore`.

#### 9.3 Docker Compose for Local Development

**REQ-DEPLOY-016:** A `docker-compose.yml` shall define:
- **`app`** — The Blazor Server application with hot-reload support
- **`db`** — SQL Server Linux container with persistent volume

**REQ-DEPLOY-017:** A multi-stage `Dockerfile` using .NET SDK for build and ASP.NET runtime for production.

**REQ-DEPLOY-018:** A `.dockerignore` file excluding build artifacts.

**REQ-DEPLOY-019:** Connection string from environment variables (`ConnectionStrings__DefaultConnection`).

**REQ-DEPLOY-020:** A `.env.example` documenting required variables:
- `SA_PASSWORD`: SQL Server SA password
- `LLM_PROVIDER`: LLM provider name (e.g., "openai", "anthropic")
- `LLM_MODEL`: Model name (e.g., "gpt-4", "claude-3-sonnet")
- `LLM_API_KEY`: API key for the LLM provider
- `GOOGLE_CLIENT_ID`: Google OAuth client ID
- `GOOGLE_CLIENT_SECRET`: Google OAuth client secret
- `APPLICATION_BASE_URL`: Base URL for QR codes (e.g., "https://myapp.azurewebsites.net")

**REQ-DEPLOY-021:** Actual `.env` in `.gitignore`.

#### 9.4 VS Code Tasks for Docker Development

**REQ-DEPLOY-022:** "Docker: Start" — `docker compose up --build`

**REQ-DEPLOY-023:** "Docker: Start (Background)" — `docker compose up --build -d`

**REQ-DEPLOY-024:** "Docker: Stop" — `docker compose down`

**REQ-DEPLOY-025:** "Docker: Stop & Clean" — `docker compose down -v`

**REQ-DEPLOY-026:** "Docker: Logs" — `docker compose logs -f`

**REQ-DEPLOY-027:** "Docker: Rebuild" — `docker compose up --build --force-recreate`

---

### 10. LLM Configuration

**REQ-LLM-001:** The LLM provider and model shall be configurable via application settings (environment variables or appsettings.json).

**REQ-LLM-002:** The implementation shall use LLMTornado library for multi-provider abstraction, allowing any LLMTornado-supported provider to be used.

**REQ-LLM-003:** The specific LLM provider is an operational decision; the code shall be provider-agnostic.

**REQ-LLM-004:** Configuration variables: `LLM_PROVIDER`, `LLM_MODEL`, `LLM_API_KEY`.

---

## Acceptance Criteria

### Authentication

**AC-AUTH-01:** Given a user on the login page, when they click "Sign in with Google", then they are redirected to Google OAuth and upon success are logged in and redirected to the groups list.

**AC-AUTH-02:** Given a logged-in teacher, when they navigate to the app, then they see only groups they created.

### Group Management

**AC-GROUP-01:** Given a teacher on the dashboard, when they click "Create Group" and enter a name and goal, then the system displays the AI's interpretation of the goal for confirmation within 10 seconds.

**AC-GROUP-02:** Given a teacher viewing the goal interpretation, when they confirm it, then a QR code is generated and the group becomes joinable.

**AC-GROUP-03:** Given a teacher viewing the goal interpretation, when they reject it, then they can modify the goal description and resubmit indefinitely.

**AC-GROUP-04:** Given a student with the QR code for a confirmed group, when they scan it, then they are taken to a join page where they enter their nickname.

**AC-GROUP-05:** Given a student with the QR code for an unconfirmed group, when they scan it, then they see a message that the group is not yet available.

**AC-GROUP-06:** Given a device that already joined a group, when scanning the same QR code again, then the previous session is restored.

**AC-GROUP-07:** Given a student enters a nickname already taken in the group, when they submit, then they receive an error and must choose a different nickname.

### Goal & Progress

**AC-GOAL-01:** Given a student in a group with a percentage goal "solve 3 equations", when they correctly solve 1 equation, then their progress shows 33% and the contributing message has a green border.

**AC-GOAL-02:** Given a student in a group with a binary goal, when they demonstrate the required understanding (even informally), then the goal displays as completed (checkmark).

**AC-GOAL-03:** Given a teacher viewing the dashboard, when a student's progress changes, then the teacher sees the update within 2 seconds without refreshing.

**AC-GOAL-04:** Given a percentage goal with ordered steps, when a student completes step 3 before step 1, then progress increments correctly (order-agnostic).

### Goal Completion

**AC-GOAL-05:** Given a student with a percentage goal, when they reach 100% progress, then they see a completion message and chat input is disabled immediately.

**AC-GOAL-06:** Given a student with a binary goal, when they complete it, then they see a completion message and chat input is disabled immediately.

**AC-GOAL-07:** Given a student has completed their goal, when the teacher views their detail, then full chat history is visible.

### Chat Interface

**AC-CHAT-01:** Given a student joining a group, when the chat loads, then they see a welcome message in English with the goal description and a progress indicator at 0% (or unchecked for binary).

**AC-CHAT-02:** Given a student typing LaTeX (e.g., `$x^2 + 5x + 6 = 0$`), when sent, then the equation renders as formatted math.

**AC-CHAT-03:** Given a student sending code in triple backticks, when displayed, then it shows with syntax highlighting.

**AC-CHAT-04:** Given a student using a browser that supports Web Speech API, when they click the microphone button and speak, then transcribed text appears in the input field.

**AC-CHAT-05:** Given a browser without Web Speech API support, when viewing the chat, then no microphone button is visible (button hidden, no fallback message).

**AC-CHAT-06:** Given a student sends a message that advances their goal, then that message displays with a green border.

**AC-CHAT-07:** Given the LLM API is unavailable, when a student sends a message, then their message appears, they see an error response, and can send another message to retry.

### AI Behavior

**AC-AI-01:** Given a goal description in English, when the AI responds to students, then all responses are in English.

**AC-AI-02:** Given a student sends an off-topic message for the first time, then a warning indicator appears on that message (no teacher alert).

**AC-AI-03:** Given a student with a warning sends another off-topic message without returning on-topic, then the teacher's dashboard shows a help alert.

**AC-AI-04:** Given a student sends a message with both on-topic and off-topic content, then it is classified as on-topic.

### Off-Topic Warning Reset

**AC-AI-05:** Given a student has received an off-topic warning then sends on-topic messages, when they later send an off-topic message, then they receive a fresh warning (not immediate escalation).

**AC-AI-06:** Given a student has been escalated and then returns to on-topic work, when they go off-topic again, then the warning cycle restarts.

### Inactivity Detection

**AC-AI-07:** Given a student joins but sends no messages for 10 minutes, then the teacher's dashboard shows an inactivity alert.

**AC-AI-08:** Given a student triggered an inactivity alert and remains inactive longer, then no additional alerts are generated until resolved.

**AC-AI-09:** Given a student triggered an inactivity alert, when they send a new message, then the teacher sees a toast notification "[Nickname] has resumed activity" that auto-dismisses after 4 seconds.

### Alert Resolution & Repeated Alerts

**AC-AI-10:** Given a teacher resolves an off-topic alert, when that student goes off-topic again, then the warning cycle restarts (warning first, then new alert).

**AC-AI-11:** Given a teacher resolves an inactivity alert, when that student becomes inactive again for 10 minutes, then a new inactivity alert is generated.

### Real-Time & Reconnection

**AC-RT-01:** Given the connection drops, then a reconnection overlay is displayed.

**AC-RT-02:** Given reconnection succeeds, then chat history is restored from the server.

**AC-RT-03:** Given the same student opens the group in two browser tabs, when they send a message from tab A, then tab B receives the update.

### Teacher Dashboard

**AC-DASH-01:** Given a teacher viewing a group, then they see all students with progress indicators (students with alerts sorted to top).

**AC-DASH-02:** Given a student has an alert, when the teacher clicks "Mark Resolved", then the alert disappears.

**AC-DASH-03:** Given a teacher clicks on a student row, then an inline detail panel expands showing key progress messages.

**AC-DASH-04:** Given a student (in-progress or completed), when the teacher views their detail, then full chat history is accessible.

### Visual Theme

**AC-THEME-01:** Given the application loads, then an animated particle background is visible with interactive particles.

**AC-THEME-02:** Given UI containers, then they have glow border effects matching the pink/purple/teal theme.

**AC-THEME-03:** Given interactive elements, when hovered, then glow animations are triggered.

**AC-THEME-04:** Given the application loads, then the color scheme matches the specified palette.

---

## Constraints

### Technical Stack (Mandatory)

- **Backend:** Blazor Server on .NET 8 (or 9/10 if available)
- **Database:** SQL Server (Azure SQL for deployment; SQL Server container via Docker Compose for local dev)
- **Frontend:** TypeScript for client-side interactivity, Tailwind CSS for styling
- **Real-time:** SignalR (built into Blazor Server)
- **AI Integration:** LLMTornado library (https://github.com/lofcz/LLMTornado) for LLM API calls
- **Deployment:** Azure App Service (Linux) via Azure CLI zip deployment

### Technical Constraints

**TC-001:** Blazor Server requires WebSocket support from the hosting environment.

**TC-002:** Azure App Service (Linux) shall have WebSocket support enabled.

**TC-003:** Google OAuth requires HTTPS in production.

**TC-004:** Voice dictation (Web Speech API) requires browser support (Chrome, Edge). Unsupported browsers hide the dictation button.

**TC-005:** LocalStorage device-binding can be bypassed by clearing browser data (acceptable for prototype).

**TC-006:** LLM provider and model shall be configurable via application settings.

**TC-007:** Production deployment uses Azure CLI with zip deployment.

**TC-008:** Docker Compose is for local development only.

### Fixed Configuration Values

**FC-001:** Inactivity timeout: 10 minutes (not configurable).

**FC-002:** Off-topic warnings: 1 warning before escalation (not configurable).

**FC-003:** AI tutor language: English only.

**FC-004:** Groups are permanent and cannot be deleted.

**FC-005:** Progress is fully automated with no teacher override.

**FC-006:** Goal descriptions in English only.

### Performance Targets

**PT-001:** Real-time updates within 2 seconds.

**PT-002:** AI response time < 5 seconds.

**PT-003:** Goal interpretation < 10 seconds.

**PT-004:** Support for at least 30 concurrent students per group.

### Assumptions

- Goal descriptions are always provided in English.
- Single LLM provider with configurable API key (provider-agnostic code).
- No offline support required.
- Web-only interface (responsive for mobile browsers).
- Math rendering via KaTeX or MathJax.
- Code highlighting via Prism.js or highlight.js.
- QR code generation via QRCoder library.
- Particle animation via tsparticles (via JS interop).
- TherapistTemplate CSS is accessible at `/Users/mu/Coding/TherapistTemplate/frontend/src/index.css` for exact style extraction.

---

## Out of Scope

The following features are explicitly **NOT** included in this prototype:

1. **Email/password authentication** — Only Google OAuth supported.
2. **Multiple teachers per group** — Each group has one teacher (creator).
3. **Student accounts** — Students are anonymous, device-bound only.
4. **Group editing after confirmation** — Goals are immutable once confirmed.
5. **Group deletion/disbanding** — Groups are permanent.
6. **Historical analytics/reporting** — No charts, exports, or trend analysis.
7. **Custom goal type configuration** — Goal types inferred by AI.
8. **Direct teacher-to-student messaging** — No private chat.
9. **Multi-language support** — English only (AI and goals).
10. **Configurable inactivity timeout** — Fixed at 10 minutes.
11. **Configurable off-topic warning count** — Fixed at 1 warning.
12. **Teacher progress override** — Fully automated.
13. **File/image uploads in chat** — Text-only (with math/code rendering).
14. **Offline mode / PWA** — Requires active internet.
15. **Native mobile apps** — Web only.
16. **Rate limiting / abuse prevention** — Not required for prototype.
17. **Automated testing** — Manual testing acceptable.
18. **Student-to-student chat** — Students only interact with AI.
19. **Continued chat after goal completion** — Chat disabled immediately upon completion.
20. **Chat export or reporting** — No export functionality.
21. **Email notifications** — In-app alerts only.
22. **Accessibility (WCAG compliance)** — Best-effort only.
23. **Internationalization (i18n)** — UI is English-only.
24. **Session recovery after localStorage clear** — Student must rejoin with a new nickname.
25. **Light/dark theme toggle** — Dark theme only.
26. **Cross-group dashboard aggregation** — Teachers view one group at a time.
27. **Orphaned session cleanup** — Sessions remain indefinitely.

---

## Appendix

### Visual Reference: TherapistTemplate Theme Details

#### Color Palette

| Token | Color | Hex Value |
|-------|-------|-----------|
| Primary | Pink | #ff6b9d |
| Secondary | Purple | #c084fc |
| Tertiary | Teal/Cyan | #22d3d8 |
| Accent | Violet | #a78bfa |
| Highlight | Light Pink | #f0abfc |
| Dark Background | Deep Purple | #0f0a1a |
| Dark Surface | Purple | #1a0f2e |

#### Glow Effects

- **Glow Teal:** `0 0 15px rgba(34, 211, 216, 0.3), 0 0 30px rgba(34, 211, 216, 0.15)`
- **Glow Pink:** `0 0 20px rgba(255, 107, 157, 0.4), 0 0 40px rgba(255, 107, 157, 0.2)`
- **Glow Purple:** `0 0 20px rgba(192, 132, 252, 0.4), 0 0 40px rgba(192, 132, 252, 0.2)`

#### Animation Effects

- **Glow Pulse:** 3-second ease-in-out infinite animation
- **Float:** 6-second ease-in-out infinite with 10px vertical movement
- **Particle Links:** Subtle connecting lines with triangle formations

#### Component Styles

- **Glow Container:** Transparent background with glowing borders
- **Inner Card:** Semi-transparent with blur effect
- **Gradient Text:** Multi-color gradient using theme colors
- **Glow Button:** Gradient background with glow shadow, scale on hover

#### Typography

- **Headings:** 'Outfit' sans-serif
- **Body Text:** 'Inter' sans-serif

### AI Integration Notes

The AI system should:

1. **Parse Goal Descriptions:** Analyze English goal text to identify binary vs percentage goals and discrete steps (2-10 recommended).
2. **Present Interpretation for Confirmation:** Show teacher the interpreted goal structure for approval.
3. **Evaluate Progress:** For each student message, determine progress contribution with bias toward giving credit.
4. **Classify Messages:** Determine on-topic vs off-topic status (bias toward on-topic when ambiguous).
5. **Guide Students:** Provide pedagogically appropriate hints in English without giving direct answers.
6. **Generate Summaries:** Create task-solution pairs for teacher review.
7. **Maintain Context:** Include full conversation history (or summarized older messages if exceeding ~50 messages/~8000 tokens) plus goal definition, progress state, and warning counter in each request.

### Business Rules Summary

| Rule | Value |
|------|-------|
| Inactivity timeout | 10 minutes (starts at join, resets on each message) |
| Off-topic warnings before escalation | 1 warning |
| AI response language | English only |
| Goal descriptions | English only |
| Teacher confirms goal interpretation | Required |
| Teacher can disband group | No |
| Goal completion behavior | Chat disabled immediately |
| Nicknames per group | Must be unique |
| Alerts after resolution | Can trigger again |
| Mixed on/off-topic messages | Treated as on-topic |
| Progress step order | Order-agnostic |
| Teacher progress override | Not allowed |
| LocalStorage bypass | Acceptable for prototype |
| Orphaned sessions | Remain indefinitely |

### Implementation Notes

> **LLMTornado API Warning:** The `ChatRequest.Model` property is typed as `ChatModel`, not `string`. When setting the model from configuration (a string value), you must use the correct conversion. The `ChatModel` class likely supports implicit conversion from string (since `api.Chat.CreateConversation("gpt-4o")` works per the README), but verify this by checking the library source or documentation via WebFetch. If implicit conversion is not supported, use `new ChatModel(modelString)` or the appropriate factory method. The implementer MUST consult the LLMTornado GitHub repo (https://github.com/lofcz/LlmTornado) or its documentation site (https://llmtornado.ai) to confirm the correct API usage — do NOT rely solely on training data for this library.

### Reference Projects

- **TherapistTemplate** (located at `/Users/mu/Coding/TherapistTemplate`): Visual theme reference with particle background, glow effects, and color palette. Main CSS at `/Users/mu/Coding/TherapistTemplate/frontend/src/index.css`
- **SuperSmashSisV2**: Deployment pattern reference for VS Code tasks and `.azure/` scripts. Path: /Users/mu/Coding/SuperSmashSisV2
