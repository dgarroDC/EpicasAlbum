using UnityEngine;
using UnityEngine.UI;

namespace EpicasAlbum;

public class BorderedImage : MonoBehaviour
{
    private static Image _borderImage;
    private RectTransform _innerImageRect;
    private Image _innerImage;

    public static BorderedImage Create()
    {
        GameObject borderedImageGo = new GameObject("BorderedImage", typeof(Image), typeof(BorderedImage));
        BorderedImage borderedImage =  borderedImageGo.GetComponent<BorderedImage>();
        _borderImage = borderedImageGo.GetComponent<Image>();

        GameObject innerImageGo = new GameObject("InnerImage", typeof(Image));
        borderedImage._innerImage = innerImageGo.GetComponent<Image>();
        borderedImage._innerImageRect = innerImageGo.GetComponent<RectTransform>();
        borderedImage._innerImageRect.parent = borderedImageGo.transform;
        borderedImage._innerImageRect.anchorMin = Vector2.zero;
        borderedImage._innerImageRect.anchorMax = Vector2.one;
        borderedImage._innerImageRect.pivot = new Vector2(0.5f, 0.5f);
        borderedImage.SetBorderSize(2);

        return borderedImage;
    }

    public void SetBorderSize(int borderSize)
    {
        _innerImageRect.offsetMin = new Vector2(borderSize, borderSize);
        _innerImageRect.offsetMax = new Vector2(-borderSize, -borderSize);
    }

    public Image GetInnerImage()
    {
        return _innerImage;
    }

    public void SetBorderColor(Color borderColor)
    {
        _borderImage.color = borderColor;
    }
}
