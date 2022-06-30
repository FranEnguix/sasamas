using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class TcpServer : MonoBehaviour
{
	[SerializeField] private string address = "127.0.0.1";
	[SerializeField] private int commandPort = 6066;
	[SerializeField] private int imagesPort = 6067;
    [SerializeField] private GameObject mapLoader;
    [SerializeField] private GameObject[] agentsPrefabs;

	private TcpListener tcpCommandListener;
	private TcpListener tcpImageListener;
	private Thread tcpListenerCommandThread;
	private Thread tcpListenerImageThread;
    private List<TcpCommandManager> tcpCommandClients;
    private List<TcpImageManager> tcpImageClients;
    private ConcurrentQueue<ICommand> commandQueue;
    private ConcurrentQueue<TcpImageManager> imageEntityNameQueue;
    private Dictionary<string, GameObject> entities;
    private Dictionary<string, GameObject> spawners;

	private bool listen;

    private void Awake() {
        if (PlayerPrefs.HasKey("ipAddress"))
            address = PlayerPrefs.GetString("ipAddress");
        if (PlayerPrefs.HasKey("commandPort"))
            commandPort = PlayerPrefs.GetInt("commandPort");
        if (PlayerPrefs.HasKey("imagePort"))
            imagesPort = PlayerPrefs.GetInt("imagePort");
    }

    private void Start() {
		listen = true;
        tcpCommandClients = new List<TcpCommandManager>();
        tcpImageClients = new List<TcpImageManager>();
        commandQueue = new ConcurrentQueue<ICommand>();
        imageEntityNameQueue = new ConcurrentQueue<TcpImageManager>();
        entities = new Dictionary<string, GameObject>();
        spawners = new Dictionary<string, GameObject>();
        LinkSpawners();
		LaunchListenerCommandThread();
        LaunchListenerEntityNameThread();
	}

	private void Update() {
        DequeueAndProcessCommand();
        DequeueAndProcessEntityName();
		// if (Input.GetKeyDown(KeyCode.J))
		//	listen = !listen;
	}

    void OnDestroy() {
        ShutdownTcpListeners();
    }

    private void LinkSpawners() {
        var mapLoaderScript = mapLoader.GetComponent<MapLoader>();
        spawners = mapLoaderScript.GetSpawners();
    }

    private void ShutdownTcpListeners() {
		if (tcpListenerCommandThread != null && tcpListenerCommandThread.IsAlive)
			tcpListenerCommandThread.Abort();
		if (tcpListenerImageThread != null && tcpListenerImageThread.IsAlive)
			tcpListenerImageThread.Abort();
        foreach (TcpCommandManager tcpCommandClient in tcpCommandClients)
            tcpCommandClient.Stop();
        foreach (TcpImageManager tcpImageClient in tcpImageClients)
            tcpImageClient.Stop();
    }

    private void DequeueAndProcessCommand() {
        if (!commandQueue.IsEmpty)
            if (commandQueue.TryDequeue(out ICommand command)) {
                command.Execute(entities);
                if (command is CreateCommand create) {
                    if (!entities.ContainsKey(create.AgentName))
                        CreateEntity(create);
                    else
                        ReconnectEntity(create);
                    SendPositionOfAvatarAgent(entities[create.AgentName]);
                }
            }
    }
    private void DequeueAndProcessEntityName() {
        if (!imageEntityNameQueue.IsEmpty)
            if (imageEntityNameQueue.TryDequeue(out TcpImageManager imageManager)) {
                if (entities.TryGetValue(imageManager.EntityName, out GameObject entity)) {
                    var entityScript = entity.GetComponent<Entity>();
                    if (entityScript.TcpImageManager != null)
                        tcpImageClients.Remove(entityScript.TcpImageManager);
                    entityScript.TcpImageManager = imageManager;
                    entityScript.StartListeningImages();
                } else {
                    imageEntityNameQueue.Enqueue(imageManager);
                }
            }
    }

	private void CreateEntity(CreateCommand command) {
        GameObject agentPrefab = GetAgentPrefab(command.AgentPrefab);
        GameObject entity = InstantiateEntity(agentPrefab, command);
        entity.name = command.AgentName;
        entities.Add(entity.name, entity);
        var tcpClient = tcpCommandClients.Find(x => x.AgentName == command.AgentName);
        var entityComponent = entity.GetComponent<Entity>();
        entityComponent.TcpCommandManager = tcpClient;
        entityComponent.AgentCollision = command.AgentCollision;
    }

    private GameObject InstantiateEntity(GameObject agentPrefab, CreateCommand command) {
        if (command.StarterPosition != null) {
            Vector3 position = command.StarterPosition3();
            return Instantiate(agentPrefab, position, Quaternion.identity);
        } else {
            var spawner = spawners[command.SpawnerName];
            Vector3 spawnerPosition = spawner.transform.position;
            return Instantiate(agentPrefab, spawnerPosition, Quaternion.identity);
        }
    }

    private void ReconnectEntity(CreateCommand command) {
        var entity = entities[command.AgentName];
        var tcpCommandManagers = tcpCommandClients.FindAll(x => x.AgentName == command.AgentName);
        TcpCommandManager tcpCommandManager = null;
        bool commandManagerConnection = false;
        foreach (var commandManager in tcpCommandManagers)
            if (commandManager.IsConnected()) {
                if (!commandManagerConnection)
                    tcpCommandManager = commandManager;
                else {
                    commandManager.Stop();
                    tcpCommandClients.Remove(commandManager);
                }
                commandManagerConnection = true;
            } else
                tcpCommandClients.Remove(commandManager);
        entity.GetComponent<Entity>().TcpCommandManager = tcpCommandManager;
    }

    private void SendPositionOfAvatarAgent(GameObject agent) {
        var entityComponent = agent.GetComponent<Entity>();
        entityComponent.SendPosition(agent.transform.position);
    }

	private void LaunchListenerCommandThread() {
        tcpListenerCommandThread = new Thread(new ThreadStart(ListenConnectionRequests)) {
            IsBackground = true
        };
        tcpListenerCommandThread.Start();
    }

	private void LaunchListenerEntityNameThread() {
        tcpListenerImageThread = new Thread(new ThreadStart(ListenImageConnections)) {
            IsBackground = true
        };
        tcpListenerImageThread.Start();
    }

    private void ListenConnectionRequests() {
        try {
            tcpCommandListener = new TcpListener(IPAddress.Parse(address), commandPort);
            tcpCommandListener.Start();
            Debug.Log("COMMAND Server is listening");
            while (listen) {
                TcpClient connectedTcpClient = tcpCommandListener.AcceptTcpClient();
                Debug.Log("COMMAND client connected");
                var tcpClientThread = new TcpCommandManager(connectedTcpClient, commandQueue);
                tcpCommandClients.Add(tcpClientThread);
                tcpClientThread.Start();
            }
        } catch (SocketException socketException) {
            Debug.Log("SocketException " + socketException.ToString());
        } finally {
            if (tcpCommandListener != null)
                tcpCommandListener.Stop();
        }
	}

    private void ListenImageConnections() {
        try {
            tcpImageListener = new TcpListener(IPAddress.Parse(address), imagesPort);
            tcpImageListener.Start();
            Debug.Log("IMAGE Server is listening");
            while (listen) {
                TcpClient connectedTcpClient = tcpImageListener.AcceptTcpClient();
                Debug.Log("IMAGE client connected");
                var tcpImageManager = new TcpImageManager(connectedTcpClient, imageEntityNameQueue);
                tcpImageClients.Add(tcpImageManager);
                tcpImageManager.Start();
            }
        } catch (SocketException socketException) {
            Debug.Log("SocketException " + socketException.ToString());
        } finally {
            if (tcpImageListener != null)
                tcpImageListener.Stop();
        }
	}

    private GameObject GetAgentPrefab(string agentPrefabName) {
        foreach(var agentPrefab in agentsPrefabs)
            if (agentPrefab.name.Equals(agentPrefabName, StringComparison.InvariantCultureIgnoreCase))
                return agentPrefab;
        return null;
    }

    /*
    // Deprecated
	private string Vector3ToPosition(Vector3 position) {
        string newPosition = "position";
        newPosition += " " + position.x.ToString("G", CultureInfo.InvariantCulture);
        newPosition += " " + position.y.ToString("G", CultureInfo.InvariantCulture);
        newPosition += " " + position.z.ToString("G", CultureInfo.InvariantCulture);
		return newPosition;
	}

    // Deprecated
	private void SendMessageToClient(string serverMessage) {
        try {
            NetworkStream stream = connectedTcpClient.GetStream();
            if (stream.CanWrite) {
                byte[] serverMessageAsByteArray = Encoding.ASCII.GetBytes(serverMessage);
                stream.Write(serverMessageAsByteArray, 0, serverMessageAsByteArray.Length);
                stream.Flush();
                Debug.Log("Server -> client: " + serverMessage);
            }
        } catch (SocketException socketException) {
            Debug.Log("Socket exception: " + socketException);
        }
	}
    */
}