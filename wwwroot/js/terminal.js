"use strict";

let isConnected = false;
const socket = new signalR.HubConnectionBuilder().withUrl("/ssh").build();
const terminalContainer = document.getElementById('terminal-container');
const xterm = new Terminal({
    rows: 24,
    cols: 80,
    'cursorBlink': false,
    rendererType: "canvas"
});

// Fit the content to the canvas.
const fitAddon = new FitAddon.FitAddon();
xterm.loadAddon(fitAddon);
xterm.open(terminalContainer);
fitAddon.fit();

// Resize when window changes.
window.onresize = function () {
    fitAddon.fit();
}

// Connect to the Web Socket.
socket.start().catch(function (err) {
    return console.error(err.toString());
});

// Backend -> Browser - Receiving a error
socket.on("Error", function (data) {
    xterm.write('\r\n*** Error: ' + data + ' ***\r\n');
});

// Backend -> Browser - Ssh disconnected.
socket.on("Disconnect", function (data) {
    isConnected = false;
    toggleConnectBtns(isConnected);
    xterm.write('\r\n*** Lost connection to SSH Server ***\r\n');
});

// Backend -> Browser - Receivng generic data.
socket.on("ReceiveMessage", function (data) {
    xterm.write(data);
});

// Browser -> Backend
xterm.onData((data) => {
    if (!isConnected) {
        xterm.blur();
        return;
    }

    socket.invoke("SendMessage", data).catch(function (err) {
        return console.error(err.toString());
    });
});

// Browser navigates away, force a disconnect.
window.addEventListener("unload", function (event) {
    if (isConnected) {
        socket.invoke("Disconnect").catch(function (err) {
            return console.error(err.toString());
        });
    }
    return true;
});

// Connect button.
document.getElementById("connectBtn").addEventListener("click", function (event) {
    event.preventDefault();

    const host = document.getElementById("host").value;
    const user = document.getElementById("user").value;
    const pass = document.getElementById("pass").value;

    if (isConnected) {
        xterm.write('\r\n*** Disconnected from SSH Server ***\r\n');

        // Send the message we want to connect to a host.
        socket.invoke("Disconnect", host).catch(function (err) {
            return console.error(err.toString());
        });
    }

    // Connect.
    xterm.write('\r\n*** Conneccting to SSH Server***\r\n');

    // Send the message we want to connect to a host.
    socket.invoke("Connect", host, user, pass).then(function () {
        xterm.write('\r\n*** Connected to SSH Server ***\r\n');
        isConnected = true;
        toggleConnectBtns(isConnected);
        xterm.focus();
    }).catch(function (err) {
        return console.error(err);
    });
});

// Disconnect button.
document.getElementById("disconnectBtn").addEventListener("click", function (event) {
    event.preventDefault();

    if (isConnected) {
        socket.invoke("Disconnect").catch(function (err) {
            return console.error(err.toString());
        });
    }
});

function toggleConnectBtns(isConnected = false) {
    if (isConnected) {
        if (!document.getElementById('connectBtn').hasAttribute('disabled')) {
            document.getElementById('connectBtn').setAttribute('disabled', 'disabled');
        }
        if (document.getElementById('disconnectBtn').hasAttribute('disabled')) {
            document.getElementById('disconnectBtn').removeAttribute('disabled');
        }
    }
    else {
        if (document.getElementById('connectBtn').hasAttribute('disabled')) {
            document.getElementById('connectBtn').removeAttribute('disabled');
        }
        if (!document.getElementById('disconnectBtn').hasAttribute('disabled')) {
            document.getElementById('disconnectBtn').setAttribute('disabled', 'disabled');
        }
    }
}