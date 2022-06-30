using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateCommand : ICommand {

    public string AgentName { get; set; }

    public string AgentPrefab { get; set; }

    public string SpawnerName { get; set; }

    public bool AgentCollision { get; set; }

    public float[] StarterPosition { get; set; }


    public void Execute(Dictionary<string, GameObject> gameObjects) {
    }

    public Vector3 StarterPosition3 () {
        if (StarterPosition != null && StarterPosition.Length == 3)
            return new Vector3(StarterPosition[0], StarterPosition[1], StarterPosition[2]);
        else 
            return new Vector3(0, 0, 0);
    }
}
