using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class TcpCommandManager
{
    private readonly TcpClient tcpClient;
    private readonly ConcurrentQueue<ICommand> callbackQueue;
    private Thread listener;
    private string agentName;
    private bool listen;


    public TcpCommandManager(TcpClient tcpClient, ConcurrentQueue<ICommand> callbackQueue) {
        this.tcpClient = tcpClient;
        this.callbackQueue = callbackQueue;
    }
    public string AgentName {
        get { return agentName; }
        set { agentName = value; }
    }

    public void Start() {
        listener = new Thread(new ThreadStart(ListenForIncommingRequests)) {
            IsBackground = true
        };
        listener.Start();
    }

    public void Stop() {
        listener.Abort();
        listen = false;
        agentName = null;
    }

    public void SendMessageToClient(string message) {
        try {
            NetworkStream stream = tcpClient.GetStream();
            if (stream.CanWrite) {
                byte[] serverMessageAsByteArray = Encoding.ASCII.GetBytes(message);
                stream.Write(serverMessageAsByteArray, 0, serverMessageAsByteArray.Length);
            }
        } catch (SocketException socketException) {
            Debug.Log("Socket exception in " + agentName + " thread: " + socketException.Message);
        } catch (ObjectDisposedException) {
            Debug.Log("TcpClient finished for " + agentName);
        }

    }
    public bool IsConnected() {
        return tcpClient != null && tcpClient.Connected;
    }

    private void ListenForIncommingRequests() {
        listen = true;
        try {
            while (listen)
                ProcessClientMessage();
        } catch (Exception e) {
            Debug.LogException(e);
        } finally {
            tcpClient.Close();
            listen = false;
        }
    }

    private void ProcessClientMessage() {
        Byte[] bytes = new Byte[1024];
        using (NetworkStream stream = tcpClient.GetStream()) {
            int length;
            while ((length = stream.Read(bytes, 0, bytes.Length)) != 0) {
                string clientMessage = GetMessageFromByteBuffer(bytes, length);
                EnqueueCommandsFromMessage(clientMessage);
                Debug.Log(agentName + " command message: " + clientMessage);
            }
        }
    }

    private string GetMessageFromByteBuffer(Byte[] bytes, int length) {
        var incommingData = new byte[length];
        Array.Copy(bytes, 0, incommingData, 0, length);
        return Encoding.ASCII.GetString(incommingData);
    }

    private void EnqueueCommandsFromMessage(string message) {
        var commands = message.Split(new string[] { "}{" }, StringSplitOptions.RemoveEmptyEntries);
        if (commands.Length == 1)
            EnqueueCommandFromMessage(message);
        else if (commands.Length > 1)
            for (int i = 0; i < commands.Length; i++) {
                var command = commands[i];
                if (i == 0 || i < commands.Length - 1)
                    command += "}";
                if (i > 0)
                    command = "{" + command;
                EnqueueCommandFromMessage(command);
            }
    }

    private void EnqueueCommandFromMessage(string message) {
        ICommand command = CommandParser.ParseCommand(message);
        if (command != null) {
            if (AgentName == null && command is CreateCommand)
                AgentName = command.AgentName;
            else
                command.AgentName = AgentName;
            callbackQueue.Enqueue(command);
        }
    }


}
