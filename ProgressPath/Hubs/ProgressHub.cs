using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace ProgressPath.Hubs;

/// <summary>
/// SignalR hub for real-time communication between the server and clients.
/// Handles channel subscriptions for teachers (group channels) and students (session channels).
/// REQ-RT-001: All real-time updates use SignalR.
/// </summary>
public class ProgressHub : Hub
{
    /// <summary>
    /// Client method name constants - must match the JavaScript/Blazor client handlers.
    /// </summary>
    public static class ClientMethods
    {
        /// <summary>
        /// Notifies clients of a progress update for a student session.
        /// Payload: { sessionId, progress, totalSteps, percentage, isCompleted }
        /// </summary>
        public const string ReceiveProgress = "ReceiveProgress";

        /// <summary>
        /// Notifies clients of a new chat message.
        /// Payload: { id, content, isFromStudent, timestamp, isOffTopic, contributesToProgress }
        /// </summary>
        public const string ReceiveMessage = "ReceiveMessage";

        /// <summary>
        /// Notifies clients of a new help alert (off-topic or inactivity).
        /// Payload: { id, sessionId, alertType, createdAt, nickname }
        /// </summary>
        public const string ReceiveAlert = "ReceiveAlert";

        /// <summary>
        /// Notifies clients that an alert has been resolved by the teacher.
        /// Payload: alertId (int)
        /// </summary>
        public const string AlertResolved = "AlertResolved";

        /// <summary>
        /// Toast notification when a previously inactive student resumes activity.
        /// Payload: nickname (string)
        /// REQ-AI-017: 4-second auto-dismiss toast.
        /// </summary>
        public const string StudentResumedActivity = "StudentResumedActivity";

        /// <summary>
        /// Notifies clients that a new student has joined the group.
        /// Payload: { id, nickname, currentProgress, isCompleted, joinedAt }
        /// </summary>
        public const string StudentJoined = "StudentJoined";
    }

    /// <summary>
    /// Thread-safe mapping of connection IDs to their subscribed group names.
    /// Used to clean up group memberships on disconnect.
    /// </summary>
    private static readonly ConcurrentDictionary<string, List<string>> ConnectionGroups = new();

    private readonly ILogger<ProgressHub> _logger;

    public ProgressHub(ILogger<ProgressHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Subscribes the caller to a group channel to receive updates for all students in a group.
    /// Used by teachers on the dashboard.
    /// Channel format: 'group_{groupId}'
    /// </summary>
    /// <param name="groupId">The group ID to subscribe to.</param>
    public async Task JoinGroupChannel(string groupId)
    {
        var channelName = $"group_{groupId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, channelName);
        TrackGroup(Context.ConnectionId, channelName);

        _logger.LogInformation(
            "Connection {ConnectionId} joined group channel {ChannelName}",
            Context.ConnectionId,
            channelName);
    }

    /// <summary>
    /// Subscribes the caller to a student channel to receive updates for their session.
    /// Used by students in the chat interface.
    /// Channel format: 'student_{sessionId}'
    /// </summary>
    /// <param name="sessionId">The student session ID to subscribe to.</param>
    public async Task JoinStudentChannel(string sessionId)
    {
        var channelName = $"student_{sessionId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, channelName);
        TrackGroup(Context.ConnectionId, channelName);

        _logger.LogInformation(
            "Connection {ConnectionId} joined student channel {ChannelName}",
            Context.ConnectionId,
            channelName);
    }

    /// <summary>
    /// Unsubscribes the caller from all channels they have joined.
    /// </summary>
    public async Task LeaveChannels()
    {
        var connectionId = Context.ConnectionId;

        if (ConnectionGroups.TryRemove(connectionId, out var groups))
        {
            foreach (var group in groups)
            {
                await Groups.RemoveFromGroupAsync(connectionId, group);
                _logger.LogDebug(
                    "Connection {ConnectionId} left channel {ChannelName}",
                    connectionId,
                    group);
            }
        }

        _logger.LogInformation(
            "Connection {ConnectionId} left all channels",
            connectionId);
    }

    /// <summary>
    /// Cleans up group memberships when a client disconnects.
    /// REQ-RT-002: Handle connection drops gracefully.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;

        if (ConnectionGroups.TryRemove(connectionId, out var groups))
        {
            foreach (var group in groups)
            {
                await Groups.RemoveFromGroupAsync(connectionId, group);
            }

            _logger.LogInformation(
                "Connection {ConnectionId} disconnected, removed from {GroupCount} channels",
                connectionId,
                groups.Count);
        }

        if (exception != null)
        {
            _logger.LogWarning(
                exception,
                "Connection {ConnectionId} disconnected with error",
                connectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Tracks a group membership for a connection in a thread-safe manner.
    /// </summary>
    private static void TrackGroup(string connectionId, string groupName)
    {
        ConnectionGroups.AddOrUpdate(
            connectionId,
            _ => new List<string> { groupName },
            (_, existingGroups) =>
            {
                lock (existingGroups)
                {
                    if (!existingGroups.Contains(groupName))
                    {
                        existingGroups.Add(groupName);
                    }
                }
                return existingGroups;
            });
    }
}
