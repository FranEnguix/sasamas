using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TextureLoader
{
    private Dictionary<string, Texture2D[]> textures;

    public TextureLoader() {
        textures = new Dictionary<string, Texture2D[]>();
    }

    public Texture2D[] GetTextures(string texturesFolderPath) {
        Texture2D[] loadedTextures;
        if (!textures.ContainsKey(texturesFolderPath)) {
            loadedTextures = LoadTexturesFromDisk(texturesFolderPath);
            if (loadedTextures.Length > 0)
                textures.Add(texturesFolderPath, loadedTextures);
        } else
            loadedTextures = textures[texturesFolderPath];
        return loadedTextures;
    }

    private Texture2D[] LoadTexturesFromDisk(string texturesFolderPath) {
        Texture2D[] textures = new Texture2D[0];
        try {
            string[] filePaths = Directory.GetFiles(texturesFolderPath);
            var loadedTextures = new List<Texture2D>();
            for (int i = 0; i < filePaths.Length; i++) {
                var texture = LoadTexture(filePaths[i]);
                if (texture != null)
                    loadedTextures.Add(texture);
            }
            textures = loadedTextures.ToArray();
        } catch (DirectoryNotFoundException) {
            Debug.LogError("Directory " + texturesFolderPath + " not found.");
        }
        return textures;
    }

    private Texture2D LoadTexture(string filePath) {
        Texture2D texture = null;
        if (File.Exists(filePath)) {
            byte[] data;
            data = File.ReadAllBytes(filePath);
            texture = new Texture2D(2, 2);
            texture.LoadImage(data);
        }
        return texture;
    }
}
