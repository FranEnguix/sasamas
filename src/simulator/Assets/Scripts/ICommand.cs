using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICommand
{
    public void Execute(Dictionary<string, GameObject> gameObjects);
    public string AgentName { get; set; }
}
