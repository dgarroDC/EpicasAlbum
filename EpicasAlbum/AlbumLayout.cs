using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace EpicasAlbum;

public class AlbumLayout : MonoBehaviour
{
    private static Color DEFAULT_BORDER = new(0.3882f, 0.498f, 0.8431f);
    private static Color SELECT_BORDER = Color.white;
    private const int HORIZONTAL_OFFSET = 50;
    private const int BIG_IMAGE_SIZE = 470;
    private const int GRID_IMAGE_SIZE = 100;
    private const int GRID_SPACING = 15;
    private const int GRID_COLUMNS = 4;
    private const int GRID_ROWS = 5 + 2;

    private BorderedImage _bigImage;
    private List<BorderedImage> _gridImages;
    private GridNavigator _gridNavigator;
    private OWAudioSource _oneShotSource;

    // TODO: Provider or something to avoid loading all at once! List<Lazy<>>? Of Sprites!
    public int selectedIndex;
    public List<Texture2D> textures = new();

    public static AlbumLayout Create(GameObject canvas, OWAudioSource oneShotSource)
    {
        GameObject albumLayoutGo = new GameObject("AlbumLayout");
        RectTransform albumLayoutRect = albumLayoutGo.AddComponent<RectTransform>();
        AlbumLayout albumLayout = albumLayoutGo.AddComponent<AlbumLayout>();
        albumLayoutGo.transform.parent = canvas.transform; // TODO: Outside?
        albumLayoutGo.transform.SetSiblingIndex(1); // So we get prompts and border mask over it, but not the background...
        albumLayoutRect.localPosition = Vector3.zero;
        albumLayoutRect.localEulerAngles = Vector3.zero;
        albumLayoutRect.localScale = Vector3.one;
        albumLayoutRect.anchorMin = Vector2.zero;
        albumLayoutRect.anchorMax = Vector2.one;
        albumLayoutRect.pivot = new Vector2(0.5f, 0.5f);
        albumLayoutRect.offsetMin = Vector2.zero;
        albumLayoutRect.offsetMax = Vector2.zero;

        BorderedImage borderedImage = BorderedImage.Create();
        borderedImage.SetBorderColor(DEFAULT_BORDER);

        albumLayout._bigImage = Instantiate(borderedImage, albumLayout.transform);
        albumLayout._bigImage.name = "BigImage";
        RectTransform bigImageRect = albumLayout._bigImage.GetComponent<RectTransform>();
        // Centered vertically, offset from right
        bigImageRect.anchorMin = new Vector2(1, 0.5f);
        bigImageRect.anchorMax = new Vector2(1, 0.5f);
        bigImageRect.pivot = new Vector2(1, 0.5f);
        bigImageRect.anchoredPosition = new Vector2(-HORIZONTAL_OFFSET, 0);
        bigImageRect.sizeDelta = new Vector2(BIG_IMAGE_SIZE, BIG_IMAGE_SIZE);

        GameObject gridGo = new GameObject("Grid", typeof(GridLayoutGroup));
        RectTransform gridRect = gridGo.GetComponent<RectTransform>();
        gridRect.parent = albumLayout.transform;
        gridRect.localPosition = Vector3.zero;
        gridRect.localEulerAngles = Vector3.zero;
        gridRect.localScale = Vector3.one;
        // Centered vertically, offset from left
        gridRect.anchorMin = new Vector2(0, 0.5f);
        gridRect.anchorMax = new Vector2(0, 0.5f);
        gridRect.pivot = new Vector2(0, 0.5f);
        gridRect.anchoredPosition = new Vector2(HORIZONTAL_OFFSET, 0);
        GridLayoutGroup grid = gridGo.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(GRID_IMAGE_SIZE, GRID_IMAGE_SIZE);
        grid.spacing = new Vector2(GRID_SPACING, GRID_SPACING);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.MiddleLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = GRID_COLUMNS;
        albumLayout._gridImages = new List<BorderedImage>();
        for (int i = 0; i < GRID_ROWS * GRID_COLUMNS; i++)
        {
            BorderedImage gridImage = Instantiate(borderedImage, gridRect);
            gridImage.name = "GridImage_" + i;
            albumLayout._gridImages.Add(gridImage);
        }

        albumLayout._gridNavigator = new GridNavigator();
        albumLayout._oneShotSource = oneShotSource;

        return albumLayout;
    }
    
    // TODO: Another name? Return delta?
    public void UpdateLayout()
    {
        // This is really similar to ShipLogItemList.UpdateList() from Custom Ship Log Modes...
        Vector2 selectionChange = Vector2.zero;

        if (textures.Count >= 2)
        {
            selectionChange = _gridNavigator.GetSelectionChange();
            if (selectionChange != Vector2.zero)
            {
                selectedIndex += (int)selectionChange.y * GRID_COLUMNS + (int)selectionChange.x;
                _bigImage.SetImage(textures[selectedIndex]); // TODO: Also do this on enter!
                // TODO: Range checks etc.
                _oneShotSource.PlayOneShot(AudioType.ShipLogMoveBetweenEntries);
            }
        }
        
        UpdateLayoutUI();
    }

    private void UpdateLayoutUI()
    {
        int selectedGridImage = selectedIndex % GRID_COLUMNS + GRID_COLUMNS;
        int offset = selectedIndex - selectedGridImage;
        for (int i = 0; i < _gridImages.Count; i++)
        {
            int imageIndex = i + offset;
            BorderedImage gridImage = _gridImages[i];
            if (imageIndex >= 0 && imageIndex < textures.Count)
            {
                gridImage.SetImage(textures[imageIndex]);
                if (imageIndex == selectedIndex)
                {
                    gridImage.SetBorderColor(SELECT_BORDER);
                }
                else
                {
                    gridImage.SetBorderColor(DEFAULT_BORDER);
                }
                gridImage.SetVisible(true);
            }
            else
            {
                gridImage.SetVisible(false);
            }
        }
    }
}
