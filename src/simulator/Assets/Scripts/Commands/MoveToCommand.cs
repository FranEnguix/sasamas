using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveToCommand : ICommand
{
    public string AgentName { get; set; }
    public float[] Position { get; set; }

    public void Execute(Dictionary<string, GameObject> gameObjects) {
        GameObject gameObj = gameObjects[AgentName];
        var entityComponent = gameObj.GetComponent<Entity>();
        if (entityComponent.NavMeshAgent != null) {
            var newPosition = new Vector3(Position[0], Position[1], Position[2]);
            entityComponent.SetTargetPosition(newPosition);
        }
    }
}
