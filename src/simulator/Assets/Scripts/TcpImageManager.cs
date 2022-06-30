using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class TcpImageManager
{
    private readonly TcpClient tcpClient;
    private readonly ConcurrentQueue<TcpImageManager> callbackQueue;
    private ConcurrentQueue<ImageData> imageQueue;
    private Thread listener;
    private string entityName;
    private bool active;

    public TcpImageManager(TcpClient tcpClient, ConcurrentQueue<TcpImageManager> callbackQueue) {
        this.tcpClient = tcpClient;
        this.callbackQueue = callbackQueue;
        active = false;
    }

    public string EntityName {
        get { return entityName; }
        set { entityName = value; }
    }

    public ConcurrentQueue<ImageData> ImageQueue {
        get { return imageQueue; }
    }

    public void Stop() {
        active = false;
        if (tcpClient != null)
            tcpClient.Close();
        if (listener != null)
            listener.Abort();
    }

    public void Start() {
        listener = new Thread(new ThreadStart(ProcessClientMessage)) {
            IsBackground = true
        };
        listener.Start();
    }

    public ConcurrentQueue<ImageData> StartListeningImages() {
        active = true;
        listener = new Thread(new ThreadStart(ProcessClientImages)) {
            IsBackground = true
        };
        listener.Start();
        imageQueue = new ConcurrentQueue<ImageData>();
        return imageQueue;
    }

    private void ProcessClientMessage() {
        Byte[] bytes = new Byte[1024];
        NetworkStream stream = tcpClient.GetStream();
        int length;
        if ((length = stream.Read(bytes, 0, bytes.Length)) != 0) {
            entityName = GetMessageFromByteBuffer(bytes, length);
            callbackQueue.Enqueue(this);
            Debug.Log("client name as: " + entityName);
        }
    }

    private void ProcessClientImages() {
        while (active) {
            if (imageQueue != null && !imageQueue.IsEmpty)
                if (imageQueue.TryDequeue(out ImageData imageData)) {
                    SendImageToClient(imageData);
                }
        }
    }

    private string GetMessageFromByteBuffer(Byte[] bytes, int length) {
        var incommingData = new byte[length];
        Array.Copy(bytes, 0, incommingData, 0, length);
        return Encoding.ASCII.GetString(incommingData);
    }

    public void SendImageToClient(ImageData imageData) {
        string imageDataJson = JsonUtility.ToJson(imageData);
        SendMessageToClient(imageDataJson);
    }

    public void SendMessageToClient(string message) {
        try {
            NetworkStream stream = tcpClient.GetStream();
            if (stream.CanWrite) {
                byte[] serverMessageAsByteArray = Encoding.ASCII.GetBytes(message);
                stream.Write(serverMessageAsByteArray, 0, serverMessageAsByteArray.Length);
                Debug.Log(" imagen mandada a " + EntityName + ": " + serverMessageAsByteArray.Length);
            }
        } catch (SocketException socketException) {
            Debug.Log("Socket exception: " + socketException);
        }
    }
}
