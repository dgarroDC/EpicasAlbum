﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EpicasAlbum.UI;

public class AlbumLayout : MonoBehaviour
{
    private static Color DEFAULT_BORDER = new(0.3882f, 0.498f, 0.8431f);
    private static Color SELECT_BORDER = new(0.9333f, 0.5569f, 0.2314f);
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
    private TextWithBackground _nameLabel; // TODO: Corner Label (generic)
    private TextWithBackground _emptyLabel; // TODO: Center Label (generic)
    private Scrollbar _scrollbar;

    public int selectedIndex;
    public List<Func<Sprite>> sprites = new(); // TODO: Can be used for animations?
    public ScreenPromptList promptList;

    public static AlbumLayout Create(GameObject albumLayoutGo, OWAudioSource oneShotSource)
    {
        RectTransform albumLayoutRect = albumLayoutGo.AddComponent<RectTransform>();
        AlbumLayout albumLayout = albumLayoutGo.AddComponent<AlbumLayout>();
        albumLayoutGo.transform.SetSiblingIndex(1); // So we get prompts and border mask over it, but not the background...
        ResetTransform(albumLayoutRect);
        albumLayoutRect.anchorMin = Vector2.zero;
        albumLayoutRect.anchorMax = Vector2.one;
        albumLayoutRect.pivot = Vector2.zero; // For the animation (from bellow)
        albumLayoutRect.offsetMin = Vector2.zero;
        albumLayoutRect.offsetMax = Vector2.zero;

        BorderedImage borderedImage = BorderedImage.Create();

        albumLayout._bigImage = Instantiate(borderedImage, albumLayoutRect);
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
        gridRect.parent = albumLayoutRect;
        ResetTransform(gridRect);
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
        albumLayout._animator.SetImmediate(1f, new Vector3(1f, 0f, 1f));

        // TODO: Avoid this?
        GameObject mapModeTextLabel = GameObject.Find("Ship_Body/Module_Cabin/Systems_Cabin/ShipLogPivot/ShipLog/ShipLogPivot/ShipLogCanvas/MapMode/NamePanelRoot/Name");
        Font font = mapModeTextLabel.GetComponent<Text>().font;

        albumLayout._nameLabel = TextWithBackground.Create();
        albumLayout._nameLabel.name = "NameLabel";
        albumLayout._nameLabel.SetBackgroundColor(DEFAULT_BORDER);
        albumLayout._nameLabel.SetPadding(7);
        albumLayout._nameLabel.SetFontSize(24);
        albumLayout._nameLabel.SetFont(font);
        RectTransform nameLabelRect = albumLayout._nameLabel.GetComponent<RectTransform>();
        nameLabelRect.parent = albumLayoutRect;
        ResetTransform(nameLabelRect);
        nameLabelRect.anchorMin = new Vector2(1, 0);
        nameLabelRect.anchorMax = new Vector2(1, 0);
        nameLabelRect.pivot = new Vector2(1, 0);
        nameLabelRect.anchoredPosition = new Vector2(-27, 25); // Hardcoded to be next to border...
        
        albumLayout._emptyLabel = TextWithBackground.Create();
        albumLayout._emptyLabel.name = "EmptyLabel";
        albumLayout._emptyLabel.SetBackgroundColor(SELECT_BORDER);
        albumLayout._emptyLabel.SetPadding(7);
        albumLayout._emptyLabel.SetFontSize(21);
        albumLayout._emptyLabel.SetFont(font);
        RectTransform emptyLabelRect = albumLayout._emptyLabel.GetComponent<RectTransform>();
        emptyLabelRect.parent = albumLayoutRect;
        ResetTransform(emptyLabelRect);
        emptyLabelRect.anchorMin = Vector2.zero;
        emptyLabelRect.anchorMax = Vector2.one;
        emptyLabelRect.pivot = new Vector2(0.5f, 0.5f);
        emptyLabelRect.anchoredPosition = Vector2.zero;
        
        GameObject promptListGo = new GameObject("PromptList", typeof(HorizontalLayoutGroup), typeof(ScreenPromptList));
        RectTransform promptListRect = promptListGo.GetComponent<RectTransform>();
        promptListRect.parent = albumLayoutRect;
        ResetTransform(promptListRect);
        promptListRect.anchorMin = new Vector2(1, 0);
        promptListRect.anchorMax = new Vector2(1, 0);
        promptListRect.pivot = Vector2.zero;
        promptListRect.anchoredPosition = new Vector2(-515, 65); // Hardcoded...
        promptListRect.sizeDelta = new Vector2(515, 25);
        albumLayout.promptList = promptListGo.GetComponent<ScreenPromptList>();
        albumLayout.promptList.SetMinElementDimensionsAndFontSize(25, 25, 21);
        HorizontalLayoutGroup promptListLayoutGroup = promptListGo.GetComponent<HorizontalLayoutGroup>();
        promptListLayoutGroup.spacing = 10;
        promptListLayoutGroup.childForceExpandWidth = false;
        promptListLayoutGroup.childForceExpandHeight = false;

        GameObject scrollbarGo = new GameObject("Scrollbar", typeof(Image), typeof(Scrollbar));
        RectTransform scrollbarRect = scrollbarGo.GetComponent<RectTransform>();
        scrollbarRect.parent = albumLayoutRect;
        ResetTransform(scrollbarRect);
        scrollbarRect.anchorMin = new Vector2(0.5f, 0.5f);
        scrollbarRect.anchorMax = new Vector2(0.5f, 0.5f);
        scrollbarRect.pivot = new Vector2(0.5f, 0.5f);
        scrollbarRect.anchoredPosition = new Vector2(-12, 0); // Not centered, would cover big image...
        scrollbarRect.sizeDelta = new Vector2(10, (GRID_ROWS - 2) * GRID_IMAGE_SIZE + (GRID_ROWS - 3) * GRID_SPACING);
        Image scrollbarImage = scrollbarGo.GetComponent<Image>();
        scrollbarImage.color = DEFAULT_BORDER;
        albumLayout._scrollbar = scrollbarGo.GetComponent<Scrollbar>();
        albumLayout._scrollbar.direction = Scrollbar.Direction.TopToBottom;

        GameObject handleGo = new GameObject("Handle", typeof(Image));
        RectTransform handleRect = handleGo.GetComponent<RectTransform>();
        handleRect.parent = scrollbarRect;
        albumLayout._scrollbar.handleRect = handleRect;
        ResetTransform(handleRect);
        handleRect.anchoredPosition = Vector2.zero;
        handleRect.sizeDelta = Vector2.zero;
        Image handleImage = handleGo.GetComponent<Image>();
        handleImage.color = SELECT_BORDER;

        return albumLayout;
    }

    private static void ResetTransform(Transform transform)
    {
        transform.localPosition = Vector3.zero;
        transform.localEulerAngles = Vector3.zero;
        transform.localScale = Vector3.one;
    }

    public void Open()
    {
        _animator.AnimateTo(1f, Vector3.one, 0.3f);
        UpdateLayoutUI(); // Maybe not necessary? Not doing it in item lists...
    }

    public void Close()
    {
        _animator.AnimateTo(1f, new Vector3(1f, 0f, 1f), 0.3f);
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
                int selectedRow = selectedIndex / GRID_COLUMNS + (int)selectionChange.y;
                int totalRows = (sprites.Count - 1) / GRID_COLUMNS + 1; // ie: row index of the last sprite + 1
                selectedRow = (selectedRow + totalRows) % totalRows;

                int selectedColumn = selectedIndex % GRID_COLUMNS + (int)selectionChange.x;
                // Important to calculated the "final selected row" before this
                int selectedRowLenght = (selectedRow == totalRows -1)? ((sprites.Count - 1) % GRID_COLUMNS + 1) : GRID_COLUMNS;
                if (selectionChange.y != 0 && selectedColumn >= selectedRowLenght)
                {
                    // Don't use mod here, we always want to land on last image in this case
                    selectedColumn = selectedRowLenght - 1;
                }
                else
                {
                    selectedColumn = (selectedColumn + selectedRowLenght) % selectedRowLenght;
                }
                selectedIndex = selectedRow * GRID_COLUMNS + selectedColumn;

                _oneShotSource.PlayOneShot(AudioType.ShipLogMoveBetweenEntries);
            }
        }
        
        // Important to call even if it seems nothing changed, the sprite funcs could return a different thing
        UpdateLayoutUI();
    }

    private void UpdateLayoutUI()
    {
        if (sprites.Count == 0)
        {
            _bigImage.SetVisible(false);
            foreach (BorderedImage gridImage in _gridImages)
            {
                gridImage.SetVisible(false);
            }
            _scrollbar.gameObject.SetActive(false);
            _emptyLabel.gameObject.SetActive(true);
            return;
        }
        
        _emptyLabel.gameObject.SetActive(false);

        // Make this load before any of the grid images => guaranteed not null
        _bigImage.SetVisible(true); // Just in case sprite was null, but it shouldn't, but also if it was empty before
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
        
        // Scrollbar
        if (sprites.Count <= (GRID_ROWS - 2) * GRID_COLUMNS)
        {
            _scrollbar.gameObject.SetActive(false);
            return;
        }
        _scrollbar.gameObject.SetActive(true);
        int totalRows = (sprites.Count - 1) / GRID_COLUMNS + 1; // Copied from other method...
        int topRow = (offset + GRID_COLUMNS) / GRID_COLUMNS;
        int visibleRows = GRID_ROWS - 2;
        _scrollbar.value = (float)topRow / (totalRows - visibleRows); // Important to consider visibles!
        _scrollbar.size = Math.Max((float)visibleRows / totalRows, 0.025f);
    }

    public void SetName(string nameValue)
    {
        _nameLabel.SetText(nameValue);
    }
    
    public void SetEmptyMessage(string message)
    {
        _emptyLabel.SetText(message);
    }
}
