using System;
using UnityEngine;

namespace EpicasAlbum.API;

public interface IEpicasAlbumAPI
{
    public void OpenSnapshotChooserDialog(string defaultSnapshotName, Action<string> selectedSnapshotNameConsumer);
    public Sprite GetSnapshotSprite(string snapshotName);
}
