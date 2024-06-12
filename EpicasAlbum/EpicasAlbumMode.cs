using System;
using System.Collections.Generic;
using System.Linq;
using EpicasAlbum.CustomModesAPIs;
using EpicasAlbum.UI;
using OWML.Common;
using UnityEngine;
using UnityEngine.UI;

namespace EpicasAlbum;

public class EpicasAlbumMode : ShipLogMode
{
    public const string Name = "Épicas Album";
    private const string EmptyText = "Empty album, upload your scout snapshots to view them here!";

    public AlbumStore Store;
    public ItemListWrapper ItemList;

    private AlbumLayout _layout;
    private List<string> _lastSnapshotNames;
    private string _selectedSnapshotName;
    private Image _itemListPhoto;
    private List<Tuple<string,bool,bool,bool>> _confirmationItems;

    private OWAudioSource _oneShotSource;
    private ScreenPromptList _upperRightPromptList;

    private ScreenPrompt _showOnDiskPrompt;
    private ScreenPrompt _deletePrompt;
    private ScreenPrompt _selectPrompt;
    private ScreenPrompt _cancelPrompt;

    private State _currentState;
    private Action<string> _selectedSnapshotNameConsumer;

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

        _itemListPhoto = ItemList.GetPhoto();
        _itemListPhoto.preserveAspect = true;

        if (ItemList is ShipLogItemListWrapper)
        {
            _layout = AlbumLayout.Create(gameObject, oneShotSource);
            _layout.SetName(Name);
            // TODO: Translation
            _layout.SetEmptyMessage(EmptyText);
 
            _itemListPhoto.gameObject.SetActive(true);
        }
        else
        {
            // We know it's cleared because Suit Log's, just open or close when needed since it's not automatic like the layout's msg
            ItemList.DescriptionFieldGetNextItem().DisplayText(EmptyText);
            (!) THIS IS SHOWN TWICE?
        } 

        _confirmationItems = new();
        // TODO: Translation
        _confirmationItems.Add(new Tuple<string, bool, bool, bool>("Yes", false, false, false));
        _confirmationItems.Add(new Tuple<string, bool, bool, bool>("No", false, false, false));

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
        UpdateSnapshots();
        _oneShotSource.PlayOneShot(AudioType.ToolProbeTakePhoto);
        if (ItemList is ShipLogItemListWrapper)
        {
            _layout.Open();
        }
        else
        {
            ItemList.Open();
        }

        if (ItemList is ShipLogItemListWrapper)
        {
            Locator.GetPromptManager().AddScreenPrompt(_showOnDiskPrompt, _layout.promptList, TextAnchor.MiddleCenter);
            Locator.GetPromptManager().AddScreenPrompt(_deletePrompt, _layout.promptList, TextAnchor.MiddleCenter);
        }
        else
        {
            Locator.GetPromptManager().AddScreenPrompt(_showOnDiskPrompt, _upperRightPromptList, TextAnchor.MiddleRight);
            Locator.GetPromptManager().AddScreenPrompt(_deletePrompt, _upperRightPromptList, TextAnchor.MiddleRight);   
        }
        Locator.GetPromptManager().AddScreenPrompt(_cancelPrompt, _upperRightPromptList, TextAnchor.MiddleRight);
        Locator.GetPromptManager().AddScreenPrompt(_selectPrompt, _upperRightPromptList, TextAnchor.MiddleRight);
    }

    private void UpdateSnapshots()
    {
        if (_lastSnapshotNames == null || !Store.SnapshotNames.SequenceEqual(_lastSnapshotNames))
        {
            _lastSnapshotNames = Store.SnapshotNames.ToList(); // Make sure to copy...
            // Show new ones on top! Already sorted that way from store
            if (ItemList is ShipLogItemListWrapper)
            {
                List<Func<Sprite>> sprites = new();
                foreach (string snapshotName in _lastSnapshotNames)
                {
                    Func<Sprite> spriteProvider = () => Store.GetSprite(snapshotName);
                    sprites.Add(spriteProvider);
                }
                _layout.sprites = sprites;
            }
            else
            {
                List<Tuple<string, bool, bool, bool>> items = new();
                foreach (string snapshotName in _lastSnapshotNames)
                {
                    items.Add(new Tuple<string, bool, bool, bool>(snapshotName, false, false, false));
                }
                (!) CALL THIS IF UNCHANGED!
                ItemList.SetItems(items);
                ItemList.SetName(Name);
                if (items.Count == 0)
                {
                    ((SuitLogItemListWrapper)ItemList).DescriptionFieldOpen();
                    _itemListPhoto.gameObject.SetActive(false);
                }
                else
                {
                    ((SuitLogItemListWrapper)ItemList).DescriptionFieldClose();
                    _itemListPhoto.gameObject.SetActive(true);
                }
            }
        }
    }

    public override void UpdateMode()
    {
        if (_currentState is State.Main or State.Choosing && ItemList is ShipLogItemListWrapper)
        {
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

        if (_currentState is State.Main or State.Choosing)
        { 
            _selectedSnapshotName = snapshotsAvailable ? _lastSnapshotNames[GetSelectedIndex()] : null;
            if (snapshotsAvailable)
            {
                _itemListPhoto.sprite = Store.GetSprite(_selectedSnapshotName);
            }
        }
        
        switch (_currentState)
        {
            case State.Main:
                if (!snapshotsAvailable)
                {
                    return;
                }
                if (OWInput.IsNewlyPressed(InputLibrary.toolActionPrimary))
                {
                    Store.ShowOnDisk(_selectedSnapshotName);
                } 
                else if (OWInput.IsNewlyPressed(InputLibrary.toolActionSecondary))
                {
                    _currentState = State.Deleting;
                    if (ItemList is ShipLogItemListWrapper)
                    {
                        ItemList.Open();
                        // No need to show photo, already done every frame...
                    }
                    ItemList.SetItems(_confirmationItems);
                    ItemList.SetName("Delete Snapshot?");
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
                        Store.DeleteSnapshot(_selectedSnapshotName); // The only reason with have this field is for this + Suit Log case (shared item list)...
                        UpdateSnapshots();
                        (!) INDEX WRONG, JUST REMEMBER PREV INDEX?????
                        if (GetSelectedIndex() >= _lastSnapshotNames.Count && _lastSnapshotNames.Count > 0)
                        {
                            // Move selected in case last one deleted, but don't select -1!
                            SetSelectedIndex(_lastSnapshotNames.Count - 1);
                        }
                        (!) TODO STILL NEED TO SET IN SUIT LOG ALWAYS
                    }
                }
                else if (OWInput.IsNewlyPressed(InputLibrary.cancel))
                {
                    closeDialog = true; // This is the same as selecting "No" (1)
                }

                if (closeDialog)
                {
                    CloseDeletionDialog();
                }
                break;
            case State.Choosing:
                if (snapshotsAvailable && OWInput.IsNewlyPressed(InputLibrary.interact))
                {
                    CloseSnapshotChooserDialog(_selectedSnapshotName);
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

    private void SetSelectedIndex(int index)
    {
        if (ItemList is ShipLogItemListWrapper)
        {
            _layout.selectedIndex = index;
        }
        ItemList.SetSelectedIndex(index);
    }

    private int GetSelectedIndex()
    {
        if (ItemList is ShipLogItemListWrapper)
        {
            return _layout.selectedIndex;
        }
        return ItemList.GetSelectedIndex();
    }

    public override void ExitMode()
    {
        // In case of electrical failure (Choosing is not possible here because it wouldn't be the active mode)
        if (_currentState == State.Deleting)
        {
            CloseDeletionDialog();
        }
        CloseLayout();
        if (_currentState != State.Main)
        {
            EpicasAlbum.Instance.ModHelper.Console.WriteLine($"Unexpected state {_currentState} on exit!", MessageType.Error);
        }
        _currentState = State.Disabled;
    }

    private void CloseLayout()
    {
        if (ItemList is ShipLogItemListWrapper)
        {
            _layout.Close();
        }
        else
        {
            ItemList.Close();
        }
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
                SetSelectedIndex(defaultIndex);
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
    
    private void CloseDeletionDialog()
    {
        _currentState = State.Main;
        if (ItemList is ShipLogItemListWrapper)
        {
            ItemList.Close();
        }
        else
        {
            int index = _lastSnapshotNames.IndexOf(_selectedSnapshotName);
            if (index >= 0)
            {
                // Case were the snapshot wasn't deleted
                ItemList.SetSelectedIndex(index);
            }
        }
        _oneShotSource.PlayOneShot(AudioType.ShipLogDeselectPlanet);
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
        // For unexpected shutdown (electrical failure)
        if (_currentState == State.Choosing)
        {
            CloseSnapshotChooserDialog(null);
        }
    }
 
    public override string GetFocusedEntryID()
    {
        return "";
    }
}
