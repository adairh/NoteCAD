using System.Collections;
using UnityEngine;
using WebSocketSharp.Server;
using WebSocketSharp;

public class CADServer : MonoBehaviour
{
    public string server = "localhost";
    public string sendPort = "8000";
    public string receivePort = "8001";

    private WebSocketServer wssvSend;
    private WebSocketServer wssvReceive;
    private static SendBehavior sendBehaviorInstance; // Static instance to access SendBehavior

    private void Start()
    {
        StartServer();
    }

    public void StartServer()
    {
        wssvSend = new WebSocketServer("ws://" + server + ":" + sendPort);
        wssvReceive = new WebSocketServer("ws://" + server + ":" + receivePort);

        // Set up the /Send and /Receive endpoints
        wssvSend.AddWebSocketService<SendBehavior>("/Send", () =>
        {
            sendBehaviorInstance = new SendBehavior();
            return sendBehaviorInstance;
        });

        wssvReceive.AddWebSocketService<ReceiveBehavior>("/Receive");

        wssvSend.Start();
        wssvReceive.Start();
        Debug.Log("WebSocket server started on ports " + sendPort + " (send) and " + receivePort + " (receive)");
    }

    public void StopServer()
    {
        wssvSend?.Stop();
        wssvReceive?.Stop();
    }

    private void OnApplicationQuit()
    {
        StopServer();
    }

    public void SendMessageToClient(string message)
    {
        if (sendBehaviorInstance != null)
        {
            sendBehaviorInstance.SendToClients(message);
            Debug.Log("Message sent to clients: " + message);
        }
        else
        {
            Debug.LogWarning("No clients connected to send message.");
        }
    }
}

public class SendBehavior : WebSocketBehavior
{
    public void SendToClients(string message)
    {
        Sessions.Broadcast(message);
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        Debug.Log("Received from client (SendBehavior): " + e.Data);
        Sessions.Broadcast("Echo: " + e.Data); // Echoes back the message to all connected clients
    }
}

public class ReceiveBehavior : WebSocketBehavior
{
    protected override void OnMessage(MessageEventArgs e)
    {
        Debug.Log("Received from client (ReceiveBehavior): " + e.Data);
    }
}
