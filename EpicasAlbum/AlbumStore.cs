using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OWML.Common;
using UnityEngine;

namespace EpicasAlbum;

public class AlbumStore
{
    private string _folder;
    private Dictionary<string, Texture2D> _loadedTextures = new(); // TODO: Remove? Why did I suggest this?
    private Dictionary<string, Sprite> _loadedSprites = new();
    private CancellationTokenSource _cts;
    private Dictionary<string, AlbumStoreFileInfo> _fileInfos = new();
    private List<string> _removedFiles = new();

    public AlbumStore(string profileName)
    {
        _folder = Path.Combine(EpicasAlbum.Instance.ModHelper.Manifest.ModFolderPath, "snapshots", profileName);
        if (!Directory.Exists(_folder))
        {
            Directory.CreateDirectory(_folder);
        }
        // We don't support the directory to be deleted while the game is running, it may be unable to save files?
        StartPolling(); // Should this only run while the mode is open?
    }

    public void StartPolling()
    {
        _cts = new CancellationTokenSource();
        CancellationToken token = _cts.Token;
        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    CheckSnapshotFiles();
                }
                catch (Exception e)
                {
                    EpicasAlbum.Instance.ModHelper.Console.WriteLine($"Error checking snapshot files: {e}", MessageType.Error);
                }
                await Task.Delay(500, token);
            }
        }, token);
    }

    private void CheckSnapshotFiles()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        string[] extensions = { ".png", ".jpg" };
        Dictionary<string, AlbumStoreFileInfo> newFileInfos = new DirectoryInfo(_folder).GetFiles()
            .Where(f => extensions.Contains(f.Extension.ToLower()))
            .Select(AlbumStoreFileInfo.ForFile)
            .Where(f => f != null) // File could be deleted while doing this
            .ToDictionary(f => f.Name, f => f);
        stopwatch.Stop();
        EpicasAlbum.Instance.ModHelper.Console.WriteLine("GETFILES="+stopwatch.ElapsedMilliseconds);
        
        // Invalidate removed or changed snapshots
        foreach (var (snapshotName, fileInfo) in _fileInfos)
        {
            AlbumStoreFileInfo newFileInfo;
            if (newFileInfos.TryGetValue(snapshotName, out newFileInfo) && !fileInfo.Equals(newFileInfo))
            {
                _loadedSprites.Remove(snapshotName);
                _loadedTextures.Remove(snapshotName);
            }
        }

        _fileInfos = newFileInfos;
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

        // I guess we could wait for the polling, no need to add it to names yet...
        // Keep the texture, since this creates a file it won't be invalidated,
        // although maybe we should be more careful with memory consumption...
        _loadedTextures.Add(fileName, snapshotTexture);
    }

    public Texture2D GetTexture(string snapshotName, bool bypassFrameLimit)
    {
        // TODO: Add option for async?
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
            // TODO: Maybe add here to the excludes too?
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
        // Don't wait for the polling (except for invalidation), there could be a delay, make it exclude
        _removedFiles.Add(snapshotName);
    }

    public void StopPolling()
    {
        _cts.Cancel();
    }

    public List<String> GetSnapshotNames()
    {
        List<string> snapshotNames = _fileInfos
            .OrderByDescending(f => f.Value.LastWriteTime)
            .Select(f => f.Key)
            .ToList();
        foreach (string removedFile in _removedFiles.ToList()) // Maybe not efficient, but we can remove
        {
            if (!snapshotNames.Remove(removedFile))
            {
                // Already not present in infos, we can't stop excluding
                _removedFiles.Remove(removedFile);
            }
            // TODO: Race condition if file with same name was added shortly after?!
        }
        return snapshotNames;
    }
}