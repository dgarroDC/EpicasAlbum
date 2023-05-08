using UnityEngine;
using UnityEngine.UI;

namespace EpicasAlbum;

public class AlbumLayout : MonoBehaviour
{
    private static Color DEFAULT_BORDER = new Color(0.3882f, 0.498f, 0.8431f);
    private const int HORIZONTAL_OFFSET = 50;
    private const int BIG_IMAGE_SIZE = 470;
    private const int GRID_IMAGE_SIZE = 100;
    private const int GRID_SPACING = 15;
    private const int GRID_COLUMNS = 4;
    private const int GRID_ROWS = 5 + 2;

    public static void Create(GameObject canvas)
    {
        GameObject albumLayout = new GameObject("AlbumLayout");
        albumLayout.transform.parent = canvas.transform; // TODO: Outside?

        BorderedImage borderedImage = BorderedImage.Create();
        borderedImage.SetBorderColor(DEFAULT_BORDER);

        BorderedImage bigImage = Instantiate(borderedImage, canvas.transform); // TODO: albumLayout (make it stretch etc)
        bigImage.name = "BigImage";
        RectTransform bigImageRect = bigImage.GetComponent<RectTransform>();
        // Centered vertically, offset from right
        bigImageRect.anchorMin = new Vector2(1, 0.5f);
        bigImageRect.anchorMax = new Vector2(1, 0.5f);
        bigImageRect.pivot = new Vector2(1, 0.5f);
        bigImageRect.anchoredPosition = new Vector2(-HORIZONTAL_OFFSET, 0);
        bigImageRect.sizeDelta = new Vector2(BIG_IMAGE_SIZE, BIG_IMAGE_SIZE);

        GameObject gridGo = new GameObject("Grid", typeof(GridLayoutGroup));
        RectTransform gridRect = gridGo.GetComponent<RectTransform>();
        gridRect.parent = canvas.transform; // TODO: albumLayout (make it stretch etc)
        // TODO: NOT WORKING
        // gridRect.position = Vector3.zero;
        // gridRect.eulerAngles = Vector3.zero;
        // gridRect.localScale = Vector3.one;
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
        for (int i = 0; i < GRID_ROWS * GRID_COLUMNS; i++)
        {
            Instantiate(borderedImage, gridRect);
        }
    }
}
