using System;
using UnityEngine;

namespace EpicasAlbum.API;

public class EpicasAlbumAPI : IEpicasAlbumAPI
{
    public void OpenSnapshotChooserDialog(string defaultSnapshotName, Action<string> selectedSnapshotNameConsumer)
    {
        EpicasAlbum.Instance.OpenSnapshotChooserDialog(defaultSnapshotName, selectedSnapshotNameConsumer);
    }

    public Sprite GetSnapshotSprite(string snapshotName)
    {
        return EpicasAlbum.Instance.GetSnapshotSprite(snapshotName);
    }
}
