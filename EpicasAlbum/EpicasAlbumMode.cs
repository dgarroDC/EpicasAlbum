using System;
using System.Collections.Generic;
using System.Linq;
using EpicasAlbum.CustomShipLogModes;
using UnityEngine;
using UnityEngine.UI;

namespace EpicasAlbum;

public class EpicasAlbumMode : ShipLogMode
{
    public const string Name = "Épicas Album";
    public AlbumStore Store;

    private AlbumLayout _layout;

    public override void Initialize(ScreenPromptList centerPromptList, ScreenPromptList upperRightPromptList, OWAudioSource oneShotSource)
    {
        _layout = AlbumLayout.Create(gameObject, oneShotSource);
    }

    public override void EnterMode(string entryID = "", List<ShipLogFact> revealQueue = null)
    {
        List<Func<Sprite>> sprites = new();
        foreach (string snapshotName in Store.SnapshotNames)
        {
            Func<Sprite> spriteProvider = () => Store.GetSprite(snapshotName);
            sprites.Add(spriteProvider);
        }
        _layout.sprites = sprites;
    }

    public override void UpdateMode()
    {
        _layout.UpdateLayout();
    }

    public override void ExitMode()
    {

    }

    public override void OnEnterComputer()
    {
        // No-op
    }

    public override void OnExitComputer()
    {
        // No-op
    }

    public override bool AllowModeSwap()
    {
        return true;
    }

    public override bool AllowCancelInput()
    {
        return true;
    }

    public override string GetFocusedEntryID()
    {
        return "";
    }
}
