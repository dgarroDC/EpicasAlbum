using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace EpicasAlbum;

public class AlbumStore
{
    private string _folder;
    public List<string> SnapshotNames = new();
    private Dictionary<string, Texture2D> _loadedSnapshots = new();

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
        _loadedSnapshots.Add(fileName, snapshotTexture);
    }

    public Texture2D GetTexture(string fileName)
    {
        if (_loadedSnapshots.ContainsKey(fileName))
        {
            return _loadedSnapshots[fileName];
        }

        byte[] data = File.ReadAllBytes(Path.Combine(_folder, fileName));
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(data);
        _loadedSnapshots.Add(fileName, texture);
        return texture;
    }
}
