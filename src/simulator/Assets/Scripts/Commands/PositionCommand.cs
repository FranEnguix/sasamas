using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionCommand : ICommand
{
    Dictionary<string, float[]> positions;

    string agentName;

    public string AgentName { get => agentName; set => agentName = value; }

    public PositionCommand() {
        positions = new Dictionary<string, float[]>();
    }
    public PositionCommand(Dictionary<string, float[]> positions) {
        this.positions = positions;
    }

    public void Execute(Dictionary<string, GameObject> gameObjects) {
        foreach(KeyValuePair<string, float[]> kvp in positions) {
            GameObject gameObj = gameObjects[kvp.Key];
            if (kvp.Value.Length == 2)
                gameObj.transform.position = new Vector2(kvp.Value[0], kvp.Value[1]);
            else if (kvp.Value.Length == 3)
                gameObj.transform.position = new Vector3(kvp.Value[0], kvp.Value[1], kvp.Value[2]);
        }
    }
}
