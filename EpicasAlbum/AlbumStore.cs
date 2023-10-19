﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using OWML.Common;
using UnityEngine;

namespace EpicasAlbum;

public class AlbumStore
{
    private string _folder;
    public List<string> SnapshotNames;
    private Dictionary<string, Texture2D> _loadedTextures = new(); // TODO: Remove? Why did I suggest this?
    private Dictionary<string, Sprite> _loadedSprites = new();
    private ConcurrentQueue<FileSystemEventArgs> _fsEvents = new(); // Important to be concurrent!

    public AlbumStore(string profileName)
    {
        _folder = Path.Combine(EpicasAlbum.Instance.ModHelper.Manifest.ModFolderPath, "snapshots", profileName);
        RefreshSnapshotNames();

        // Should I create watchers for each file type?
        FileSystemWatcher watcher = new FileSystemWatcher();
        watcher.Path = _folder;
        watcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.DirectoryName | NotifyFilters.FileName |
                               NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.Size; // Not sure
        watcher.Changed += (_, args) => _fsEvents.Enqueue(args);
        watcher.Created += (_, args) => _fsEvents.Enqueue(args);
        watcher.Deleted += (_, args) => _fsEvents.Enqueue(args);
        watcher.Renamed += (_, args) => _fsEvents.Enqueue(args); // This isn't raised? Only Deleted -> Created I see...
        watcher.EnableRaisingEvents = true;
    }

    public bool CheckChanges()
    {
        if (_fsEvents.IsEmpty) return false;

        while (!_fsEvents.IsEmpty)
        {                
            
            FileSystemEventArgs args;
            _fsEvents.TryDequeue(out args);
            if (args is { ChangeType: WatcherChangeTypes.Changed })
            {
                // The image could be different now and we don't want to display the old one
                // TODO: Name is returning same as FullPath, Mono bug? What if FullPath is broken too?
                string snapshotName = Path.GetFileName(args.FullPath);
                _loadedSprites.Remove(snapshotName);
                _loadedTextures.Remove(snapshotName);
            }
        }
        
        // This invalidates for deleted or renamed ones
        RefreshSnapshotNames();
        return true;
    }

    private void RefreshSnapshotNames()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        if (!Directory.Exists(_folder))
        {
            Directory.CreateDirectory(_folder);
        }
        string[] extensions = { ".png", ".jpg" };
        SnapshotNames = new DirectoryInfo(_folder).GetFiles()
            .Where(f => extensions.Contains(f.Extension.ToLower()))
            .OrderBy(f => f.CreationTime)
            .Select(f => f.Name)
            .Reverse()
            .ToList();
        stopwatch.Stop();
        // TODO: THIS IS SLOW
        EpicasAlbum.Instance.ModHelper.Console.WriteLine("GETFILES="+stopwatch.ElapsedMilliseconds);
        
        // Invalidate removed snapshots
        List<string> toRemove = _loadedTextures.Keys.Except(SnapshotNames).ToList();
        foreach (string snapshotName in toRemove)
        {
            _loadedSprites.Remove(snapshotName);
            _loadedTextures.Remove(snapshotName);
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

        // I guess we could wait for the watcher, no need to add it to names yet...
        // Keep the texture, since this creates a file it won't be invalidated,
        // although maybe we should be more careful with memory consumption...
        _loadedTextures.Add(fileName, snapshotTexture);
    }

    public Texture2D GetTexture(string snapshotName, bool bypassFrameLimit)
    {
        // TODO: Fix bypassFrameLimit
        if (_loadedTextures.ContainsKey(snapshotName))
        {
            return _loadedTextures[snapshotName];
        }
        if (EpicasAlbum.Instance.texturesLoadedThisFrame > 0)
        {
            return null;
        }
        
        string path = GetPath(snapshotName);
        if (!File.Exists(path))
        {
            EpicasAlbum.Instance.ModHelper.Console.WriteLine($"File {path} not found, manually deleted?", MessageType.Error);
            return null;
        }

        EpicasAlbum.Instance.texturesLoadedThisFrame++;
        byte[] data = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2);
        texture.name = snapshotName;
        texture.LoadImage(data); // Slow!!!
        _loadedTextures.Add(snapshotName, texture);
        return texture;
    }

    private string GetPath(string fileName)
    {
        // TODO: Here check for "screenshot/"
        return Path.Combine(_folder, fileName);
    }

    public Sprite GetSprite(string snapshotName, bool bypassFrameLimit = false)
    {
        if (_loadedSprites.ContainsKey(snapshotName))
        {
            return _loadedSprites[snapshotName];
        }

        Texture2D texture = GetTexture(snapshotName, bypassFrameLimit);
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

    public void DeleteSnapshot(string snapshotName)
    {
        File.Delete(GetPath(snapshotName));
        // Don't wait for the watcher, there could be a delay
        SnapshotNames.Remove(snapshotName);
        _loadedTextures.Remove(snapshotName);
        _loadedSprites.Remove(snapshotName);
    }
}
