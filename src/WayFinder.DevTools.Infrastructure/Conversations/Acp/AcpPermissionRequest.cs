using System.Text.Json;

namespace WayFinder.DevTools.Infrastructure.Conversations.Acp;

public sealed record AcpPermissionRequest(
    string ToolCallId,
    string ToolName,
    JsonElement RawInput);
