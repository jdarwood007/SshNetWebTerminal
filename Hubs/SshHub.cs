using Microsoft.AspNetCore.SignalR;
using Renci.SshNet;
using Renci.SshNet.Common;
using SshNetWebTerminal.Services;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SshNetWebTerminal.Hubs;
public class SshHub : Hub
{
    public async Task Connect(string Host, string User, string Pass)
    {
        try
        {
            if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(User) || string.IsNullOrEmpty(Pass))
            {
                throw new Exception("Invalid Login");
            }

            // Attempt to login.
            SshClient client = new(Host, User, Pass);
            CancellationToken cancellation = new();
            await client.ConnectAsync(cancellation);

            // Open a Stream to act as our terminal.
            ShellStream shellStream = client.CreateShellStream("xterm-256color", 80, 24, 800, 600, 1024);

            // Wrap our event handlers to prevent memory leaks.
            SshStreamEventWrapper w = new(Clients.Caller, shellStream);

            SshSessionManager.Register(Context.ConnectionId, client, shellStream, w);
        }
        catch (Exception e)
        {
            await Clients.Caller.SendAsync("Error", e.Message);
            throw new HubException(e.Message);
        }
    }

    public async Task Disconnect()
    {
        try
        {
            await SshSessionManager.Remove(Context.ConnectionId);
            await Clients.Caller.SendAsync("Disconnect");
        }
        catch {}
    }

    // Receive data from the browser.
    public async Task SendMessage(string data)
    {
        try
        {
            ShellStream shellStream = SshSessionManager.GetShellStream(Context.ConnectionId) ?? throw new Exception("INvalid Shell");

            byte[] encoded = Encoding.UTF8.GetBytes(data);
            await shellStream.WriteAsync(encoded);
            await shellStream.FlushAsync();

        }
        catch (Exception e)
        {
            await Clients.Caller.SendAsync("Error", e.Message);
            throw new HubException(e.Message);
        }
    }

    // When the hub is removed, do some cleanup checks.
    protected async override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        try
        {
            List<string> Ids = await SshSessionManager.Cleanup();

            foreach (string id in Ids)
            {
                await Clients.Client(id).SendAsync("Disconnect");
            }
        }
        catch (Exception)
        {
            throw;
        }
    }
}
