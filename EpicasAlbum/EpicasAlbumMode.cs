using System;
using System.Collections.Generic;
using System.Linq;
using EpicasAlbum.UI;
using UnityEngine;

namespace EpicasAlbum;

public class EpicasAlbumMode : ShipLogMode
{
    public const string Name = "Épicas Album";
    public AlbumStore Store;

    private AlbumLayout _layout;
    private List<string> _lastSnapshotNames;

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
        // TODO: Translation
        _layout.SetEmptyMessage("Empty album, upload your scout snapshots to view them here!");
    }

    public override void EnterMode(string entryID = "", List<ShipLogFact> revealQueue = null)
    {
        // TODO: If empty?
        UpdateSnaphots();
        _oneShotSource.PlayOneShot(AudioType.ToolProbeTakePhoto);
        _layout.Open();

        Locator.GetPromptManager().AddScreenPrompt(_showOnDiskPrompt, _centerPromptList, TextAnchor.MiddleCenter);
        Locator.GetPromptManager().AddScreenPrompt(_deletePrompt, _centerPromptList, TextAnchor.MiddleCenter);
    }

    private void UpdateSnaphots()
    {
        if (_lastSnapshotNames == null || !Store.SnapshotNames.SequenceEqual(_lastSnapshotNames))
        {
            _lastSnapshotNames = Store.SnapshotNames.ToList(); // Make sure to copy...
            // Show new ones on top!
            List<Func<Sprite>> sprites = new();
            for (var i = _lastSnapshotNames.Count - 1; i >= 0; i--)
            {
                string snapshotName = _lastSnapshotNames[i];
                Func<Sprite> spriteProvider = () => Store.GetSprite(snapshotName);
                sprites.Add(spriteProvider);
            }
            _layout.sprites = sprites;
        }
    }

    public override void UpdateMode()
    {
        _layout.UpdateLayout();

        bool snapshotsAvailable = _lastSnapshotNames.Count > 0;
        _showOnDiskPrompt.SetVisibility(snapshotsAvailable);
        _deletePrompt.SetVisibility(snapshotsAvailable);
        if (snapshotsAvailable) {
            if (OWInput.IsNewlyPressed(InputLibrary.toolActionPrimary))
            {
                // TODO: Could SnapshotNames change???
                Store.ShowOnDisk(Store.SnapshotNames[_layout.selectedIndex]);
            }
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
