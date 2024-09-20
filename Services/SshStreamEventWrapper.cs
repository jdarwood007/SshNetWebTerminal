using Microsoft.AspNetCore.SignalR;
using Renci.SshNet;
using Renci.SshNet.Common;
using System.Text;

namespace SshNetWebTerminal.Services;

public class SshStreamEventWrapper : IDisposable
{
    private readonly ISingleClientProxy Socket;
    private readonly ShellStream ShellStream;

    public SshStreamEventWrapper(ISingleClientProxy socket, ShellStream shellStream)
    {
        Socket = socket;
        ShellStream = shellStream;

        ShellStream.DataReceived += DataReceived;
        ShellStream.ErrorOccurred += ErrorOccurred;
        ShellStream.Closed += Closed;
    }

    private async void DataReceived(object? _, ShellDataEventArgs e) => await Socket.SendAsync("ReceiveMessage", Encoding.UTF8.GetString(e.Data));

    private async void ErrorOccurred(object? _, ExceptionEventArgs e) => await Socket.SendAsync("Error", e.Exception.Message);

    private async void Closed(object? sender, EventArgs e) => await Socket.SendAsync("Disconnect");

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            ShellStream.DataReceived -= DataReceived;
            ShellStream.ErrorOccurred -= ErrorOccurred;
            ShellStream.Closed -= Closed;
        }
    }
}