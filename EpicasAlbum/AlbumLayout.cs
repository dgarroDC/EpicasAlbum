using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace EpicasAlbum;

public class AlbumLayout : MonoBehaviour
{
    private static Color DEFAULT_BORDER = new(0.3882f, 0.498f, 0.8431f);
    private static Color SELECT_BORDER = new(1f, 0.6429f, 0.191f);
    private const int HORIZONTAL_OFFSET = 50;
    private const int BIG_IMAGE_SIZE = 470;
    private const int GRID_IMAGE_SIZE = 100;
    private const int GRID_SPACING = 15;
    private const int GRID_COLUMNS = 4;
    private const int GRID_ROWS = 5 + 2;

    private BorderedImage _bigImage;
    private List<BorderedImage> _gridImages;
    private GridNavigator _gridNavigator;
    private OWAudioSource _oneShotSource; // TODO: Remove from Create (ItemList like)?
    private CanvasGroupAnimator _animator;

    public int selectedIndex;
    public List<Func<Sprite>> sprites = new(); // TODO: Can be used for animations?

    public static AlbumLayout Create(GameObject albumLayoutGo, OWAudioSource oneShotSource)
    {
        RectTransform albumLayoutRect = albumLayoutGo.AddComponent<RectTransform>();
        AlbumLayout albumLayout = albumLayoutGo.AddComponent<AlbumLayout>();
        albumLayoutGo.transform.SetSiblingIndex(1); // So we get prompts and border mask over it, but not the background...
        albumLayoutRect.localPosition = Vector3.zero;
        albumLayoutRect.localEulerAngles = Vector3.zero;
        albumLayoutRect.localScale = Vector3.one;
        albumLayoutRect.anchorMin = Vector2.zero;
        albumLayoutRect.anchorMax = Vector2.one;
        albumLayoutRect.pivot = Vector2.zero; // For the animation (from left)
        albumLayoutRect.offsetMin = Vector2.zero;
        albumLayoutRect.offsetMax = Vector2.zero;

        BorderedImage borderedImage = BorderedImage.Create();

        albumLayout._bigImage = Instantiate(borderedImage, albumLayout.transform);
        albumLayout._bigImage.name = "BigImage";
        albumLayout._bigImage.SetBorderColor(DEFAULT_BORDER);
        albumLayout._bigImage.SetBorderSize(2);
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

        albumLayout._animator = albumLayoutGo.AddComponent<CanvasGroupAnimator>();
        albumLayout._animator.SetImmediate(1f, new Vector3(0f, 1f, 1f));

        return albumLayout;
    }

    public void Open()
    {
        _animator.AnimateTo(1f, Vector3.one, 0.3f);
        UpdateLayoutUI(); // Maybe not necessary? Not doing it in item lists...
    }

    public void Close()
    {
        _animator.AnimateTo(1f, new Vector3(0f, 1f, 1f), 0.3f);
    }

    // TODO: Another name? Return delta?
    public void UpdateLayout()
    {
        // This is really similar to ShipLogItemList.UpdateList() from Custom Ship Log Modes...
        Vector2 selectionChange = Vector2.zero;

        if (sprites.Count >= 2)
        {
            selectionChange = _gridNavigator.GetSelectionChange();
            if (selectionChange != Vector2.zero)
            {
                // TODO: Less complicated way to calculate this?
                // Note that we don't use GRID_ROWS here, the "real" number of rows is totalRows
                int selectedColumn = selectedIndex % GRID_COLUMNS + (int)selectionChange.x;
                selectedColumn = (selectedColumn + GRID_COLUMNS) % GRID_COLUMNS;
                int selectedRow = selectedIndex / GRID_COLUMNS + (int)selectionChange.y;
                int totalRows = (sprites.Count - 1) / GRID_COLUMNS + 1; // ie: row index of the last sprite + 1
                selectedRow = (selectedRow + totalRows) % totalRows;
                selectedIndex = selectedRow * GRID_COLUMNS + selectedColumn;
                // Non-complete last row case
                if (selectedIndex >= sprites.Count)
                {
                    selectedIndex = sprites.Count - 1;
                }

                _oneShotSource.PlayOneShot(AudioType.ShipLogMoveBetweenEntries);
            }
        }
        
        // Important to call even if it seems nothing changed, the sprite funcs could return a different thing
        UpdateLayoutUI();
    }

    private void UpdateLayoutUI()
    {
        // Make this load before any of the grid images => guaranteed not null
        _bigImage.SetVisible(true); // Just in case sprite was null, but it shouldn't
        _bigImage.DisplaySprite(sprites[selectedIndex].Invoke());

        // TODO: Less complicated way to calculate this?
        int selectedGridImage = selectedIndex % GRID_COLUMNS + GRID_COLUMNS; // If kept at top
        int offset = selectedIndex - selectedGridImage;
        int minAllowedEmptyRow = Math.Min((sprites.Count - 1) / GRID_COLUMNS + 2, // + 2 because first emtpy + the top row
            GRID_ROWS - 1); // The last row doesn't matter
        int firstEmptyRow = ((sprites.Count - 1) - offset) / GRID_COLUMNS + 1;
        if (firstEmptyRow < minAllowedEmptyRow)
        {
            offset -= (minAllowedEmptyRow - firstEmptyRow) * GRID_COLUMNS;
        }
        for (int i = 0; i < _gridImages.Count; i++)
        {   
            int imageIndex = i + offset;
            BorderedImage gridImage = _gridImages[i];
            if (imageIndex >= 0 && imageIndex < sprites.Count)
            {
                if (imageIndex == selectedIndex)
                {
                    gridImage.SetBorderColor(SELECT_BORDER);
                    gridImage.SetBorderSize(2);
                    gridImage.SetAlpha(1f);
                }
                else
                {
                    gridImage.SetBorderColor(DEFAULT_BORDER);
                    gridImage.SetBorderSize(4);
                    gridImage.SetAlpha(0.92f);
                }
                gridImage.SetVisible(true);
                gridImage.DisplaySprite(sprites[imageIndex].Invoke()); // Do this after in case of null
            }
            else
            {
                gridImage.SetVisible(false);
            }
        }
    }
}
