using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;

namespace EpicasAlbum;

// TODO: fileName -> snapshotName?
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
        while (File.Exists(Path.Combine(_folder, fileName)))
        {
            fileName = $"{baseName}_{num}.png";
            num++;
        }
        File.WriteAllBytes(Path.Combine(_folder, fileName), data);

        SnapshotNames.Add(fileName);
        _loadedTextures.Add(fileName, snapshotTexture);
    }

    public Texture2D GetTexture(string fileName)
    {
        if (_loadedTextures.ContainsKey(fileName))
        {
            return _loadedTextures[fileName];
        }
        if (EpicasAlbum.Instance.texturesLoadedThisFrame > 0)
        {
            return null;
        }
        EpicasAlbum.Instance.texturesLoadedThisFrame++;
        
        var timer = new Stopwatch();
        timer.Start();
        byte[] data = File.ReadAllBytes(Path.Combine(_folder, fileName));
        timer.Stop();
        EpicasAlbum.Instance.ModHelper.Console.WriteLine("TIME LOAD FILE="+timer.Elapsed.Milliseconds);
        timer.Start();
        Texture2D texture = new Texture2D(2, 2);
        timer.Stop();
        EpicasAlbum.Instance.ModHelper.Console.WriteLine("TIME CREATE TEXTURE="+timer.Elapsed.Milliseconds);
        timer.Start();
        texture.LoadImage(data);
        timer.Stop();
        EpicasAlbum.Instance.ModHelper.Console.WriteLine("TIME LOAD TEXTURE="+timer.Elapsed.Milliseconds);
        _loadedTextures.Add(fileName, texture);
        return texture;
    }

    public Sprite GetSprite(string fileName)
    {
        if (_loadedSprites.ContainsKey(fileName))
        {
            return _loadedSprites[fileName];
        }

        EpicasAlbum.Instance.ModHelper.Console.WriteLine("LOADING " + fileName);
        Texture2D texture = GetTexture(fileName);
        if (texture == null)
        {
            return null;
        }
        var timer = new Stopwatch();
        timer.Start();
        // The mesh time seems to make a lot of difference in the time the sprite takes to create!  
        Sprite sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), 
            new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect);
        timer.Stop();
        EpicasAlbum.Instance.ModHelper.Console.WriteLine("TIME LOAD SPRITE="+timer.Elapsed.Milliseconds);
        _loadedSprites.Add(fileName, sprite);
        return sprite;
    }
}
