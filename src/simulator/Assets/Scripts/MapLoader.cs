using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MapLoader : MonoBehaviour
{
    [SerializeField] string mapFileName = "map";
    [SerializeField] string configFileName = "map_config";
    [SerializeField] GameObject[] instantiablePrefabs;

    private InfoCollection objects;
    private TextureLoader textureLoader;
    private Dictionary<string, GameObject> prefabs;
    private Dictionary<string, GameObject> spawners;

    //private int objectsSpawned;

    private void Awake() {
        //objectsSpawned = 0;
        //var t0 = DateTime.Now;
        textureLoader = new TextureLoader();
        spawners = new Dictionary<string, GameObject>();
        prefabs = PopulateInstantiablePrefabs(instantiablePrefabs);
        objects = LoadMapFileInfo(mapFileName, configFileName);
        PlaceLightsAndObjects(objects, prefabs);
        //var tf = DateTime.Now;
        //LogTime(t0, tf);
    }

    /*
    private void LogTime(DateTime t0, DateTime tf) {
        string filename = "time_log.txt";
        var td = tf - t0;
        File.AppendAllText(filename, $"{objectsSpawned} objects in {td.TotalSeconds}s.\n");
    }
    */

    private Dictionary<string, GameObject> PopulateInstantiablePrefabs(GameObject[] instantiablePrefabs) {
        var dict = new Dictionary<string, GameObject>();
        foreach (GameObject prefab in instantiablePrefabs)
            dict.Add(prefab.name, prefab);
        return dict;
    }

    private InfoCollection LoadMapFileInfo(string filename, string mapConfigFilename) {
        string editorExtension = ".txt";
        string resultExtension = ".json";
        string mapConfigFullFilename = mapConfigFilename + ".json";
        if (!File.Exists(mapConfigFullFilename))
            GenerateDefaultMapConfigFile(mapConfigFullFilename);
        if (File.Exists(filename + editorExtension)) {
            GenerateJsonFromEditor(filename + editorExtension, mapConfigFullFilename);
        } else {
            if (!File.Exists(filename + resultExtension))
                return GenerateDefaultTemplate(filename + resultExtension);
        }
        return ReadMapFile(filename + resultExtension);
    }

    private void GenerateJsonFromEditor(string filename, string mapConfigFilename) {
        MapConfiguration mapConfiguration = ReadMapConfigFile(mapConfigFilename);
        int spawnerCounter = 0;
        float currentX = mapConfiguration.origin.x;
        float currentY = mapConfiguration.origin.y;
        float currentZ = mapConfiguration.origin.z;
        IEnumerable<string> lines = File.ReadLines(filename);
        foreach (string line in lines) {
            char[] chars = line.ToCharArray();
            foreach (char c in chars) {
                if (c != ' ') {
                    var pos = new Vector3(currentX, currentY, currentZ);
                    InstantiateSymbol(mapConfiguration.SymbolToPrefabMapping, c, pos, ref spawnerCounter);
                    //objectsSpawned++;
                }
                currentX += mapConfiguration.distance.x;
            }
            currentX = mapConfiguration.origin.x;
            currentZ += mapConfiguration.distance.y;
        }
    }

    private void InstantiateSymbol(Dictionary<string, SymbolPrefabPair> symbolToPrefabMapping, char symbol, Vector3 pos, ref int spawnerCounter) {
        ObjectInfo instantiable = new ObjectInfo {
            active = true,
            position = pos,
            objectPrefabName = symbolToPrefabMapping[symbol.ToString()].prefabName,
            objectName = symbolToPrefabMapping[symbol.ToString()].prefabName,
            dataFolder = symbolToPrefabMapping[symbol.ToString()].dataFolder,
        };
        if (instantiable.objectPrefabName.Equals("Spawner"))
            instantiable.objectName += " " + ++spawnerCounter;
        PlaceObject(instantiable, prefabs);
    }

    private void GenerateDefaultMapConfigFile(string filename, bool prettyPrint = true) {
        MapConfiguration mapConfig = new MapConfiguration {
            distance = new Vector2(1.2f, 1.2f)
        };
        mapConfig.SymbolToPrefabMapping = new Dictionary<string, SymbolPrefabPair> {
            { "T", new SymbolPrefabPair { symbol = "T", prefabName = "Tree" } },
            { "A", new SymbolPrefabPair { symbol = "A", prefabName = "Spawner" } },
            { "F", new SymbolPrefabPair { symbol = "F", prefabName = "Tree Fruit Variant" } },
        };
        mapConfig.ArrayLetterToPrefabMapping();
        string json = JsonUtility.ToJson(mapConfig, prettyPrint);
        File.WriteAllText(filename, json);
    }

    private void PlaceLightsAndObjects(InfoCollection infoCollection, Dictionary<string, GameObject> prefabs) {
        PlaceLights(infoCollection.lights, prefabs);
        PlaceObjects(infoCollection.objects, prefabs);
    }

    private void PlaceLights(LightInfo[] lights, Dictionary<string, GameObject> prefabs) {
        foreach (LightInfo light in lights) {
            if (light.active) {
                var lightInstance = Instantiate(
                        prefabs[light.objectPrefabName],
                        light.position,
                        Quaternion.identity
                );
                lightInstance.transform.Rotate(light.rotation.x, light.rotation.y, light.rotation.z, Space.Self);
                lightInstance.name = light.objectName;
                var lightComponent = lightInstance.GetComponent<Light>();
                lightComponent.color = light.color;
                lightComponent.intensity = light.intensity;
            }
        }
    }

    private void PlaceObjects(ObjectInfo[] objects, Dictionary<string, GameObject> prefabs) {
        foreach (ObjectInfo obj in objects)
            PlaceObject(obj, prefabs);
    }

    private void PlaceObject(ObjectInfo objectInfo, Dictionary<string, GameObject> prefabs) {
        if (objectInfo.active) {
            var objInstance = Instantiate(
                    prefabs[objectInfo.objectPrefabName],
                    objectInfo.position,
                    Quaternion.identity
            );
            objInstance.transform.Rotate(objectInfo.rotation.x, objectInfo.rotation.y, objectInfo.rotation.z, Space.Self);
            objInstance.name = objectInfo.objectName;
            if (!string.IsNullOrEmpty(objectInfo.dataFolder)) {
                try {
                    var changeTexture = objInstance.GetComponent<ChangeTexture>();
                    var textures = textureLoader.GetTextures(objectInfo.dataFolder);
                    changeTexture.ApplyRandomTextures(textures);
                } catch (NullReferenceException) {
                    Debug.LogError(objectInfo.objectName + " (" + objectInfo.objectPrefabName + ") ChangeTexture component is missing.");
                }
            }
            if (objectInfo.objectPrefabName == "Spawner")
                spawners.Add(objInstance.name, objInstance);
        }
    }

    private InfoCollection ReadMapFile(string filename) {
        string json = File.ReadAllText(filename);
        return JsonUtility.FromJson<InfoCollection>(json);
    }

    private MapConfiguration ReadMapConfigFile(string filename) {
        string json = File.ReadAllText(filename);
        var mapConfiguration = JsonUtility.FromJson<MapConfiguration>(json);
        mapConfiguration.InitLetterToPrefabMapping();
        return mapConfiguration;
    }

    private InfoCollection GenerateDefaultTemplate(string filename, bool prettyPrint = true) {
        LightInfo[] lightInfos = {
            new LightInfo {
                active = true,
                objectName = "Sun Light",
                objectPrefabName = "Light",
                position = new Vector3(0, 3, 0),
                rotation = new Vector3(35, 40, 0),
                color = new Color(1, 0.95f, 0.84f),
                intensity = 1
            },
            new LightInfo {
                active = false,
                objectName = "Moon Light",
                objectPrefabName = "Light",
                position = new Vector3(0, 3, 0),
                rotation = new Vector3(154, 224, 0),
                color = new Color(0.51f, 0.70f, 1),
                intensity = 1.2f
            }
        };
        ObjectInfo[] list = {
            new ObjectInfo {
                active = true,
                objectName = "Spawner Inicial",
                objectPrefabName = "Spawner",
                position = new Vector3(3.5f, 0, 0)
            },
            new ObjectInfo {
                active = true,
                objectName = "Tree 1",
                objectPrefabName = "Tree",
                position = new Vector3(-2.6f, 0, 0)
            }
        };
        InfoCollection objects = new InfoCollection {
            lights = lightInfos,
            objects = list
        };
        string json = JsonUtility.ToJson(objects, prettyPrint);
        File.WriteAllText(filename, json);
        return objects;
    }

    public Dictionary<string, GameObject> GetSpawners() {
        return spawners;
    }
}
