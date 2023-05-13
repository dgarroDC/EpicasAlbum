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
    private OWAudioSource _oneShotSource;
    private ScreenPromptList _centerPromptList;

    private ScreenPrompt _showOnDiskPrompt;
    private ScreenPrompt _deletePrompt;

    public override void Initialize(ScreenPromptList centerPromptList, ScreenPromptList upperRightPromptList, OWAudioSource oneShotSource)
    {
        _oneShotSource = oneShotSource;
        _centerPromptList = centerPromptList;
        // TODO: Translation
        _showOnDiskPrompt = new ScreenPrompt(InputLibrary.toolActionPrimary, "Show on Disk");
        _deletePrompt = new ScreenPrompt(InputLibrary.toolActionSecondary, "Delete");

        _layout = AlbumLayout.Create(gameObject, oneShotSource);
        _layout.SetName(Name);
    }

    public override void EnterMode(string entryID = "", List<ShipLogFact> revealQueue = null)
    {
        // TODO: If empty?
        List<Func<Sprite>> sprites = new();
        foreach (string snapshotName in Store.SnapshotNames)
        {
            Func<Sprite> spriteProvider = () => Store.GetSprite(snapshotName);
            sprites.Add(spriteProvider);
        }
        _layout.sprites = sprites;
        _oneShotSource.PlayOneShot(AudioType.ToolProbeTakePhoto);
        _layout.Open();

        Locator.GetPromptManager().AddScreenPrompt(_showOnDiskPrompt, _centerPromptList, TextAnchor.MiddleCenter, -1, true);
        Locator.GetPromptManager().AddScreenPrompt(_deletePrompt, _centerPromptList, TextAnchor.MiddleCenter, -1, true);
    }

    public override void UpdateMode()
    {
        _layout.UpdateLayout();

        if (OWInput.IsNewlyPressed(InputLibrary.toolActionPrimary))
        {
            // TODO: Could SnapshotNames change???
            Store.ShowOnDisk(Store.SnapshotNames[_layout.selectedIndex]);
        }
    }

    public override void ExitMode()
    {
        _layout.Close();

        Locator.GetPromptManager().RemoveScreenPrompt(_showOnDiskPrompt);
        Locator.GetPromptManager().RemoveScreenPrompt(_deletePrompt);
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
