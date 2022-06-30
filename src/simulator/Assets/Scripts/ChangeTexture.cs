using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ChangeTexture : MonoBehaviour
{
    [SerializeField] private GameObject instantiableObject;
    [SerializeField] private Vector3[] instantiableObjectPositions;

    private MeshRenderer[] meshRenderers;
    private GameObject[] objects;

    void Awake() {
        InstantiateObjectsAndGetMeshRenderers();
    }


    private void InstantiateObjectsAndGetMeshRenderers() {
        int numObjects = instantiableObjectPositions.Length;
        objects = new GameObject[numObjects];
        meshRenderers = new MeshRenderer[numObjects];
        for (int i = 0; i < numObjects; i++) {
            Vector3 objectPosition = instantiableObjectPositions[i] + transform.position;
            objects[i] = Instantiate(instantiableObject, objectPosition, Quaternion.identity, transform);
            meshRenderers[i] = objects[i].GetComponent<MeshRenderer>();
        }
    }

    public void ApplyRandomTextures(Texture2D[] textures) {
        foreach (var meshRenderer in meshRenderers) {
            var randomTexture = textures[Random.Range(0, textures.Length)];
            meshRenderer.material.mainTexture = randomTexture;
        }
    }

    /*
    private void Test() {
        var texture = LoadTexture(texturesFolderPath);
        foreach (var meshr in meshRenderers) {
            meshr.material.mainTexture = texture;
        }
    }

    private void UpdateFruitsMaterial(Material material) {
        foreach(var meshRenderer in meshRenderers)
            meshRenderer.material = material;
    }
    */
}
