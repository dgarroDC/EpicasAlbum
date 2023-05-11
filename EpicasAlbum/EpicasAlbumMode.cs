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
    public ItemListWrapper ItemList;
    public AlbumStore Store;

    private Image _photo;
    private AlbumLayout _layout;

    public override void Initialize(ScreenPromptList centerPromptList, ScreenPromptList upperRightPromptList, OWAudioSource oneShotSource)
    {
        ItemList.SetName(Name);
        _photo = ItemList.GetPhoto();
        _photo.gameObject.SetActive(true);
        _photo.preserveAspect = true;

        GameObject canvas = GameObject.Find("Ship_Body/Module_Cabin/Systems_Cabin/ShipLogPivot/ShipLog/ShipLogPivot/ShipLogCanvas/");
        _layout = AlbumLayout.Create(canvas, oneShotSource);
    }

    public override void EnterMode(string entryID = "", List<ShipLogFact> revealQueue = null)
    {
        // ItemList.Open();
        // List<Tuple<string,bool,bool,bool>> items = Store.SnapshotNames
        //     .Select(snapshotName => new Tuple<string, bool, bool, bool>(snapshotName, false, false, false))
        //     .ToList();
        // ItemList.SetItems(items);
        // ItemList.SetSelectedIndex(0);
        // OnItemSelected();
        List<Func<Sprite>> sprites = new();
        foreach (string snapshotName in Store.SnapshotNames)
        {
            Func<Sprite> spriteProvider = () => Store.GetSprite(snapshotName);
            sprites.Add(spriteProvider);
        }
        _layout.sprites = sprites;
    }
    
    private void OnItemSelected()
    {
        int selectedIndex = ItemList.GetSelectedIndex();
        Texture2D texture = Store.GetTexture(Store.SnapshotNames[selectedIndex]);
        _photo.sprite = Store.GetSprite(Store.SnapshotNames[selectedIndex]);
    }
    
    public override void UpdateMode()
    {
        // if (ItemList.UpdateList() != 0)
        // {
        //     OnItemSelected();
        // }
        
        _layout.UpdateLayout();
    }

    public override void ExitMode()
    {
        // ItemList.Close();
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
