using Microsoft.AspNetCore.SignalR;
using Renci.SshNet;
using System.Collections.Generic;

namespace SshNetWebTerminal.Services;

public static class SshSessionManager
{
    private static readonly Dictionary<string, SshClient> SshClients = [];
    private static readonly Dictionary<string, ShellStream> SshStreams = [];
    private static readonly Dictionary<string, SshStreamEventWrapper> Wrappers = [];

    public static void Register(string connectionId, SshClient client, ShellStream stream, SshStreamEventWrapper wrapper)
    {
        SshClients.Add(connectionId, client);
        SshStreams.Add(connectionId, stream);
        Wrappers.Add(connectionId, wrapper);
    }

    public static ShellStream? GetShellStream(string connectionId) => SshStreams.TryGetValue(connectionId, out ShellStream? stream) ? stream : null;

    public static async Task Remove(string connectionId)
    {
        if (Wrappers.TryGetValue(connectionId, out SshStreamEventWrapper? w))
        {
            w?.Dispose();
        }
        Wrappers.Remove(connectionId);

        ShellStream? s = GetShellStream(connectionId);
        if (s != null)
        {
            await s.DisposeAsync();
        }
        SshStreams.Remove(connectionId);

        if (SshClients.TryGetValue(connectionId, out SshClient? c))
        {
            c?.Disconnect();
            c?.Dispose();
        }
        SshClients.Remove(connectionId);
    }

    public static async Task<List<string>> Cleanup()
    {
        List<string> Ids = [];

        foreach ((var id, var c) in SshClients)
        {
            if (c == null || !c.IsConnected)
            {
                await Remove(id);
                Ids.Add(id);
            }
        }

        foreach ((var id, var s) in SshStreams)
        {
            if (s == null || (!s.CanRead && s.CanWrite))
            {
                await Remove(id);
                Ids.Add(id);
            }
        }

        return Ids;
    }
}
