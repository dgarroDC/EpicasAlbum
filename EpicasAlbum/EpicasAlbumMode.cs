using System;
using System.Collections.Generic;
using System.Linq;
using EpicasAlbum.CustomShipLogModes;
using EpicasAlbum.UI;
using UnityEngine;
using UnityEngine.UI;

namespace EpicasAlbum;

public class EpicasAlbumMode : ShipLogMode
{
    public const string Name = "Épicas Album";

    public AlbumStore Store;
    public ItemListWrapper ItemList;

    private AlbumLayout _layout;
    private List<string> _lastSnapshotNames;

    private OWAudioSource _oneShotSource;
    private ScreenPromptList _upperRightPromptList;

    private ScreenPrompt _showOnDiskPrompt;
    private ScreenPrompt _deletePrompt;

    private bool _deleteDialogOpen;
    private Image _itemListPhoto;
    private ScreenPrompt _deleteSelectPrompt;
    private ScreenPrompt _deleteCancelPrompt;

    public override void Initialize(ScreenPromptList centerPromptList, ScreenPromptList upperRightPromptList, OWAudioSource oneShotSource)
    {
        _oneShotSource = oneShotSource;
        _upperRightPromptList = upperRightPromptList;
        // TODO: Translation
        _showOnDiskPrompt = new ScreenPrompt(InputLibrary.toolActionPrimary, "Show on Disk");
        _deletePrompt = new ScreenPrompt(InputLibrary.toolActionSecondary, "Delete");
        _deleteCancelPrompt = new ScreenPrompt(InputLibrary.cancel, "Cancel");
        _deleteSelectPrompt = new ScreenPrompt(InputLibrary.interact, "Select Option");

        _layout = AlbumLayout.Create(gameObject, oneShotSource);
        _layout.SetName(Name);
        // TODO: Translation
        _layout.SetEmptyMessage("Empty album, upload your scout snapshots to view them here!");
        
        ItemList.SetName("Delete Snapshot?");
        _itemListPhoto = ItemList.GetPhoto();
        _itemListPhoto.gameObject.SetActive(true);
        _itemListPhoto.preserveAspect = true; 
        List<Tuple<string,bool,bool,bool>> items = new();
        // TODO: Translation
        items.Add(new Tuple<string, bool, bool, bool>("Yes", false, false, false));
        items.Add(new Tuple<string, bool, bool, bool>("No", false, false, false));
        ItemList.SetItems(items);
    }

    public override void EnterMode(string entryID = "", List<ShipLogFact> revealQueue = null)
    {
        // TODO: If empty?
        UpdateSnaphots();
        _oneShotSource.PlayOneShot(AudioType.ToolProbeTakePhoto);
        _layout.Open();

        Locator.GetPromptManager().AddScreenPrompt(_showOnDiskPrompt, _layout.promptList, TextAnchor.MiddleCenter);
        Locator.GetPromptManager().AddScreenPrompt(_deletePrompt, _layout.promptList, TextAnchor.MiddleCenter);
        Locator.GetPromptManager().AddScreenPrompt(_deleteCancelPrompt, _upperRightPromptList, TextAnchor.MiddleCenter);
        Locator.GetPromptManager().AddScreenPrompt(_deleteSelectPrompt, _upperRightPromptList, TextAnchor.MiddleCenter);
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
        if (!_deleteDialogOpen)
        {
            _layout.UpdateLayout();
        }
        else
        {
            ItemList.UpdateList();
        }

        bool snapshotsAvailable = _lastSnapshotNames.Count > 0;
        bool snapshotSelectedInAlbum = !_deleteDialogOpen && snapshotsAvailable;
        _showOnDiskPrompt.SetVisibility(snapshotSelectedInAlbum);
        _deletePrompt.SetVisibility(snapshotSelectedInAlbum);
        _deleteCancelPrompt.SetVisibility(_deleteDialogOpen);
        _deleteSelectPrompt.SetVisibility(_deleteDialogOpen);

        if (snapshotSelectedInAlbum) {
            if (OWInput.IsNewlyPressed(InputLibrary.toolActionPrimary))
            {
                Store.ShowOnDisk(GetSelectedSnapshotName());
            }
            if (OWInput.IsNewlyPressed(InputLibrary.toolActionSecondary))
            {
                _deleteDialogOpen = true;
                ItemList.GetPhoto().sprite = Store.GetSprite(GetSelectedSnapshotName());
                ItemList.Open();
                ItemList.SetSelectedIndex(0);
                _oneShotSource.PlayOneShot(AudioType.ShipLogSelectPlanet);
            }
            if (OWInput.IsNewlyPressed(InputLibrary.markEntryOnHUD))
            {
                _layout.sprites.AddRange(_layout.sprites);
            }
        }

        if (_deleteDialogOpen)
        {
            bool closeDialog = false;
            if (OWInput.IsNewlyPressed(InputLibrary.interact))
            {
                closeDialog = true;
                if (ItemList.GetSelectedIndex() == 0)
                {
                    Store.DeleteSnapshot(GetSelectedSnapshotName());
                    UpdateSnaphots();
                    if (_layout.selectedIndex >= _lastSnapshotNames.Count && _lastSnapshotNames.Count > 0)
                    {
                        // Move selected in case last one deleted, but don't select -1!
                        _layout.selectedIndex = _lastSnapshotNames.Count - 1;
                    }
                }
            }
            if (OWInput.IsNewlyPressed(InputLibrary.cancel))
            {
                closeDialog = true; // This is the same as selecting "No" (1)
            }

            if (closeDialog)
            {
                _deleteDialogOpen = false;
                ItemList.Close();
                _oneShotSource.PlayOneShot(AudioType.ShipLogDeselectPlanet);
            }
        }
    }

    private string GetSelectedSnapshotName()
    {
        // Reversed in layout
        return _lastSnapshotNames[_lastSnapshotNames.Count - 1 - _layout.selectedIndex];
    }

    public override void ExitMode()
    {
        _layout.Close();

        Locator.GetPromptManager().RemoveScreenPrompt(_showOnDiskPrompt);
        Locator.GetPromptManager().RemoveScreenPrompt(_deletePrompt);
        Locator.GetPromptManager().RemoveScreenPrompt(_deleteSelectPrompt);
        Locator.GetPromptManager().RemoveScreenPrompt(_deleteCancelPrompt);
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
        return !_deleteDialogOpen;
    }

    public override bool AllowCancelInput()
    {
        return !_deleteDialogOpen;
    }

    public override string GetFocusedEntryID()
    {
        return "";
    }
}
