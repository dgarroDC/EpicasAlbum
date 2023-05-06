using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace EpicasAlbum;

public class AlbumStore
{
    private string _folder;
    private List<string> _snapshotNames = new();
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
            _snapshotNames.Add(file.Name);
        }
    }
    
    public void Save(Texture2D snapshotTexture)
    {
        // Based on ProbeLauncher.SaveSnapshotToFile (but with full year and in 24 hours format)
        byte[] data = snapshotTexture.EncodeToPNG();
        string baseName = $"ProbePhoto_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
        string fileName = baseName + ".png";
        int num = 1;
        while (File.Exists(Path.Combine(_folder, fileName)))
        {
            fileName = $"{baseName}_{num}.png";
            num++;
        }
        File.WriteAllBytes(Path.Combine(_folder, fileName), data);

        _snapshotNames.Add(fileName);
        _loadedSnapshots.Add(fileName, snapshotTexture);
    }
}
