using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class CommandParser
{
    public static ICommand ParseCommand(string order) {
        ICommand command = null;
        CommandInfo info = JsonUtility.FromJson<CommandInfo>(order);
        if (info.commandName.StartsWith("moveTo"))
            command = MoveTo(info);
        else if (info.commandName.StartsWith("create"))
            command = Create(info);
        else if (info.commandName.StartsWith("image"))
            command = Image(info);
        else if (info.commandName.StartsWith("cameraFov"))
            command = CameraFov(info);
        else if (info.commandName.StartsWith("cameraRotate"))
            command = CameraRotate(info);
        else if (info.commandName.StartsWith("cameraMove"))
            command = CameraMove(info);
        else if (info.commandName.StartsWith("color"))
            command = GetColorCommand(info);
        return command;
    }


    private static ColorCommand GetColorCommand(CommandInfo info) {
        var color = JsonUtility.FromJson<Color>(info.data[0]);
        return new ColorCommand {
            AgentColor = color
        };
    }

    private static MoveToCommand MoveTo(CommandInfo info) {
        float[] position = ParseCoordinates(info.data[0]);
        return new MoveToCommand {
            Position = position,
        };
    }

    private static CreateCommand Create(CommandInfo info) {
        var data = info.data;
        bool agentCollision = bool.Parse(data[3]);
        var createCommand = new CreateCommand {
            AgentName = data[0],
            AgentPrefab = data[1],
            AgentCollision = agentCollision,
        };
        if (data[2].StartsWith("("))
            createCommand.StarterPosition = ParseCoordinates(data[2]);
        else
            createCommand.SpawnerName = data[2];
        return createCommand;
    }

    private static ImageCommand Image(CommandInfo info) {
        return new ImageCommand {
            CaptureFrequency = float.Parse(info.data[0])
        };
    }

    private static CameraFovCommand CameraFov(CommandInfo info) {
        return new CameraFovCommand {
            CameraIndex = int.Parse(info.data[0]),
            Fov = float.Parse(info.data[1])
        };
    }

    private static CameraRotateCommand CameraRotate(CommandInfo info) {
        var axis = GetAxis(info);
        return new CameraRotateCommand {
            CameraIndex = int.Parse(info.data[0]),
            RotateAxis = axis,
            Degrees = float.Parse(info.data[2])
        };
    }

    private static CameraMoveCommand CameraMove(CommandInfo info) {
        var axis = GetAxis(info);
        return new CameraMoveCommand {
            CameraIndex = int.Parse(info.data[0]),
            MoveAxis = axis,
            Units = float.Parse(info.data[2])
        };
    }

    private static Vector3 GetAxis(CommandInfo info) {
        var axis = new Vector3();
        switch(int.Parse(info.data[1])) {
            case 0:
                axis.x = 1;
                break;
            case 1:
                axis.y = 1;
                break;
            case 2:
                axis.z = 1;
                break;
        }
        return axis;
    }
    private static float[] ParseCoordinates(string message) {
        string regexPattern = @"^\((-?\d+(\.\d+)?) (-?\d+(\.\d+)?) (-?\d+(\.\d+)?)?\)$";
        Regex regex = new Regex(regexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        MatchCollection matches = regex.Matches(message);
        Match match = matches[0];
        GroupCollection groups = match.Groups;
        int components = (groups.Count - 1) / 2;
        float[] result = new float[components];
        for (int i = 0; i < components; i++)
            result[i] = float.Parse(groups[i * 2 + 1].Value);
        return result;
    }

    /*
    private static Color GetColor(string colorString) {
        // DEPRECATED
        Color color = new Color();
        var colorStringSplitted = colorString.Split(' ');
        color.r = float.Parse(colorStringSplitted[0]);
        color.g = float.Parse(colorStringSplitted[1]);
        color.b = float.Parse(colorStringSplitted[2]);
        color.a = float.Parse(colorStringSplitted[3]);
        return color;
    }

    private static string GetAgentName(CommandInfo info) {
        // DEPRECATED
        string composedName = info.gameObject;
        int pEnd = composedName.IndexOf(" (");
        return composedName.Substring(0, pEnd);
    }

    private static string GetAgentPrefabName(CommandInfo info) {
        // DEPRECATED
        string composedName = info.gameObject;
        int pIni = composedName.IndexOf("(") + 1;
        int pEnd = composedName.IndexOf(")");
        return composedName.Substring(pIni, pEnd - pIni);
    }
    */
}
