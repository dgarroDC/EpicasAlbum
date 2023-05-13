using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;

namespace EpicasAlbum;

public class AlbumStore
{
    private string _folder;
    public List<string> SnapshotNames = new();
    private Dictionary<string, Texture2D> _loadedTextures = new(); // TODO: Remove?
    private Dictionary<string, Sprite> _loadedSprites = new();

    public AlbumStore(string folder)
    {
        _folder = folder;
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
        foreach (FileInfo file in new DirectoryInfo(folder).GetFiles().OrderBy(f => f.CreationTime))
        {
            SnapshotNames.Add(file.Name);
        }
    }
    
    public void Save(Texture2D snapshotTexture)
    {
        // Based on ProbeLauncher.SaveSnapshotToFile
        byte[] data = snapshotTexture.EncodeToPNG();
        string baseName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
        string fileName = baseName + ".png";
        int num = 1;
        while (File.Exists(GetPath(fileName)))
        {
            fileName = $"{baseName}_{num}.png";
            num++;
        }
        File.WriteAllBytes(Path.Combine(_folder, fileName), data);

        SnapshotNames.Add(fileName);
        _loadedTextures.Add(fileName, snapshotTexture);
    }

    public Texture2D GetTexture(string snapshotName)
    {
        if (_loadedTextures.ContainsKey(snapshotName))
        {
            return _loadedTextures[snapshotName];
        }
        if (EpicasAlbum.Instance.texturesLoadedThisFrame > 0)
        {
            return null;
        }
        EpicasAlbum.Instance.texturesLoadedThisFrame++;
        
        byte[] data = File.ReadAllBytes(GetPath(snapshotName));
        Texture2D texture = new Texture2D(2, 2);
        texture.name = snapshotName;
        texture.LoadImage(data); // Slow!!!
        _loadedTextures.Add(snapshotName, texture);
        return texture;
    }

    private string GetPath(string fileName)
    {
        return Path.Combine(_folder, fileName);
    }

    public Sprite GetSprite(string snapshotName)
    {
        if (_loadedSprites.ContainsKey(snapshotName))
        {
            return _loadedSprites[snapshotName];
        }

        Texture2D texture = GetTexture(snapshotName);
        if (texture == null)
        {
            return null;
        }
        
        // The mesh time seems to make a lot of difference in the time the sprite takes to create!  
        Sprite sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), 
            new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect);
        sprite.name = snapshotName;
        _loadedSprites.Add(snapshotName, sprite);
        return sprite;
    }

    public void ShowOnDisk(string snapshotName)
    {
        // Application.OpenURL("file://" + GetPath(snapshotName));
        // TODO: Linux support?
        string path = GetPath(snapshotName).Replace(@"/", @"\");   // explorer doesn't like front slashes
        Process.Start("explorer.exe", "/select," + path);
    }
}
