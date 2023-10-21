using System;
using System.Collections.Generic;
using System.Linq;
using EpicasAlbum.CustomShipLogModes;
using EpicasAlbum.UI;
using OWML.Common;
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

    private State _currentState;
    private Action<string> _selectedSnapshotNameConsumer;

    private Image _itemListPhoto;
    private ScreenPrompt _selectPrompt;
    private ScreenPrompt _cancelPrompt;

    // Same as I did for Journal
    public enum State
    {
        Disabled,
        Main,
        Deleting,
        Choosing
    }

    public override void Initialize(ScreenPromptList centerPromptList, ScreenPromptList upperRightPromptList, OWAudioSource oneShotSource)
    {
        _oneShotSource = oneShotSource;
        _upperRightPromptList = upperRightPromptList;
        // TODO: Translation
        _showOnDiskPrompt = new ScreenPrompt(InputLibrary.toolActionPrimary, "Show on Disk");
        _deletePrompt = new ScreenPrompt(InputLibrary.toolActionSecondary, "Delete");
        _cancelPrompt = new ScreenPrompt(InputLibrary.cancel, "Cancel");
        _selectPrompt = new ScreenPrompt(InputLibrary.interact, "Select");

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

        _currentState = State.Disabled;
    }

    public override void EnterMode(string entryID = "", List<ShipLogFact> revealQueue = null)
    {
        OpenLayout();
        if (_currentState != State.Disabled)
        {
            EpicasAlbum.Instance.ModHelper.Console.WriteLine($"Unexpected state {_currentState} on enter!", MessageType.Error);
        }
        _currentState = State.Main;
    }

    private void OpenLayout()
    {
        UpdateSnaphots();
        _oneShotSource.PlayOneShot(AudioType.ToolProbeTakePhoto);
        _layout.Open();

        Locator.GetPromptManager().AddScreenPrompt(_showOnDiskPrompt, _layout.promptList, TextAnchor.MiddleCenter);
        Locator.GetPromptManager().AddScreenPrompt(_deletePrompt, _layout.promptList, TextAnchor.MiddleCenter);
        Locator.GetPromptManager().AddScreenPrompt(_cancelPrompt, _upperRightPromptList, TextAnchor.MiddleRight);
        Locator.GetPromptManager().AddScreenPrompt(_selectPrompt, _upperRightPromptList, TextAnchor.MiddleRight);
    }

    private void UpdateSnaphots()
    {
        if (_lastSnapshotNames == null || Store.IsChanged())
        {
            // The index change is important if it was empty (I could check count > 0 but just in case...)
            string prevSelectedSnapshotName = 
                _lastSnapshotNames != null && _layout.selectedIndex < _lastSnapshotNames.Count? 
                    GetSelectedSnapshotName() : null;

            _lastSnapshotNames = Store.GetSnapshotNames();
            // Show new ones on top! Already sorted that way from store
            List<Func<Sprite>> sprites = new();
            foreach (string snapshotName in _lastSnapshotNames)
            {
                Func<Sprite> spriteProvider = () => Store.GetSprite(snapshotName);
                sprites.Add(spriteProvider);
            }
            _layout.sprites = sprites;
            
            int prevSelectedIndex = _lastSnapshotNames.FindIndex(snapshotName => snapshotName.Equals(prevSelectedSnapshotName));
            if (prevSelectedIndex >= 0)
            {
                // Try to keep the same snapshot selected (at least by name, replacing file would select the new)
                _layout.selectedIndex = prevSelectedIndex;
            }
            else if (_layout.selectedIndex >= _lastSnapshotNames.Count)
            {
                // Move selected in case last one deleted, but don't select -1! (for some reason)
                _layout.selectedIndex = Mathf.Max(0, _lastSnapshotNames.Count - 1);
            }
        }
    }

    public override void UpdateMode()
    {
        if (_currentState is State.Main or State.Choosing)
        {
            // Don't update whie deleting, could make it delete other!
            // TODO: Make it remember selected name! (and maybe autoclose?)
            UpdateSnaphots();
            _layout.UpdateLayout();
        }
        else
        {
            ItemList.UpdateList();
        }

        bool snapshotsAvailable = _lastSnapshotNames.Count > 0;
        _showOnDiskPrompt.SetVisibility(snapshotsAvailable && _currentState == State.Main);
        _deletePrompt.SetVisibility(snapshotsAvailable && _currentState == State.Main);
        _cancelPrompt.SetVisibility(_currentState is State.Deleting or State.Choosing);
        _selectPrompt.SetVisibility(_currentState == State.Deleting || _currentState == State.Choosing && snapshotsAvailable);

        switch (_currentState)
        {
            case State.Main:
                if (!snapshotsAvailable)
                {
                    return;
                }
                if (OWInput.IsNewlyPressed(InputLibrary.toolActionPrimary))
                {
                    Store.ShowOnDisk(GetSelectedSnapshotName());
                } 
                else if (OWInput.IsNewlyPressed(InputLibrary.toolActionSecondary))
                {
                    _currentState = State.Deleting;
                    ItemList.GetPhoto().sprite = Store.GetSprite(GetSelectedSnapshotName());
                    ItemList.Open();
                    ItemList.SetSelectedIndex(0);
                    _oneShotSource.PlayOneShot(AudioType.ShipLogSelectPlanet);
                }
                break; 
            case State.Deleting:
                bool closeDialog = false;
                if (OWInput.IsNewlyPressed(InputLibrary.interact))
                {
                    closeDialog = true;
                    if (ItemList.GetSelectedIndex() == 0)
                    {
                        Store.DeleteSnapshot(GetSelectedSnapshotName());
                    }
                }
                else if (OWInput.IsNewlyPressed(InputLibrary.cancel))
                {
                    closeDialog = true; // This is the same as selecting "No" (1)
                }

                if (closeDialog)
                {
                    _currentState = State.Main;
                    ItemList.Close();
                    _oneShotSource.PlayOneShot(AudioType.ShipLogDeselectPlanet);
                }
                break;
            case State.Choosing:
                if (snapshotsAvailable && OWInput.IsNewlyPressed(InputLibrary.interact))
                {
                    CloseSnapshotChooserDialog(GetSelectedSnapshotName());
                }
                else if (OWInput.IsNewlyPressed(InputLibrary.cancel))
                {
                    CloseSnapshotChooserDialog(null);
                }
                break;
            default:
                EpicasAlbum.Instance.ModHelper.Console.WriteLine($"Unexpected state {_currentState} on update!", MessageType.Error);
                break;
        }
    }

    private string GetSelectedSnapshotName()
    {
        return _lastSnapshotNames[_layout.selectedIndex];
    }

    public override void ExitMode()
    {
        CloseLayout();
        if (_currentState != State.Main)
        {
            EpicasAlbum.Instance.ModHelper.Console.WriteLine($"Unexpected state {_currentState} on exit!", MessageType.Error);
        }
        _currentState = State.Disabled;
    }

    private void CloseLayout()
    {
        _layout.Close();

        Locator.GetPromptManager().RemoveScreenPrompt(_showOnDiskPrompt);
        Locator.GetPromptManager().RemoveScreenPrompt(_deletePrompt);
        Locator.GetPromptManager().RemoveScreenPrompt(_selectPrompt);
        Locator.GetPromptManager().RemoveScreenPrompt(_cancelPrompt);
    }

    public void OpenSnapshotChooserDialog(string defaultSnapshotName, Action<string> selectedSnapshotNameConsumer)
    {
        OpenLayout();
        if (defaultSnapshotName != null)
        {
            int defaultIndex = _lastSnapshotNames.FindIndex(snapshotName => snapshotName.Equals(defaultSnapshotName));
            if (defaultIndex >= 0)
            {
                _layout.selectedIndex = defaultIndex;
            }
            else
            {
                EpicasAlbum.Instance.ModHelper.Console.WriteLine(
                    $"Snapshot with name {defaultSnapshotName} not found when opening dialog!", MessageType.Error);
                // Keep the previous index
            }
        }

        _selectedSnapshotNameConsumer = selectedSnapshotNameConsumer;
        
        if (_currentState != State.Disabled)
        {
            EpicasAlbum.Instance.ModHelper.Console.WriteLine($"Unexpected state {_currentState} on open chooser dialog!", MessageType.Error);
        }
        _currentState = State.Choosing;
    }
    
    private void CloseSnapshotChooserDialog(string selectedSnapshotName)
    {
        _selectedSnapshotNameConsumer.Invoke(selectedSnapshotName);
        _selectedSnapshotNameConsumer = null;
        
        CloseLayout();
        if (_currentState != State.Choosing)
        {
            EpicasAlbum.Instance.ModHelper.Console.WriteLine($"Unexpected state {_currentState} on close chooser dialog!", MessageType.Error);
        }
        _currentState = State.Disabled;
    }

    public bool IsActiveButNotCurrent()
    {
        // The mod is active on choosing dialog mode but it's not the current SL mode!
        return _currentState == State.Choosing;
    }

    public override bool AllowModeSwap()
    {
        return _currentState == State.Main;
    }

    public override bool AllowCancelInput()
    {
        // Chooser is closeable but handled by the mode itself (adding it here wouldn't make a difference tho)
        return _currentState == State.Main;
    }

    public override void OnEnterComputer()
    {
        // No-op
    }

    public override void OnExitComputer()
    {
        // No-op
    }
 
    public override string GetFocusedEntryID()
    {
        return "";
    }

    public void OnDestroy()
    {
        // Is this the best place to do this? What if the mode wasn't created yet?
        Store.StopPolling();
    }
}
