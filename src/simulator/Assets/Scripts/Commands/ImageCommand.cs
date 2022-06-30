using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImageCommand : ICommand {
    public string AgentName { get; set; }
    public int CameraIndex { get; set; }
    public float CaptureFrequency { get; set; }

    public void Execute(Dictionary<string, GameObject> gameObjects) {
        var entity = gameObjects[AgentName].GetComponent<Entity>();
        entity.LaunchTakePicture(CameraIndex, CaptureFrequency);
    }
}
