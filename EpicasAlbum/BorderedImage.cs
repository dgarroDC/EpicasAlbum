using UnityEngine;
using UnityEngine.UI;

namespace EpicasAlbum;

public class BorderedImage : MonoBehaviour
{
    public Image borderImage;
    public RectTransform innerImageRect;
    public Image innerImage;
    public CanvasGroup canvasGroup;

    public static BorderedImage Create()
    {
        GameObject borderedImageGo = new GameObject("BorderedImage", typeof(Image), typeof(BorderedImage));
        BorderedImage borderedImage =  borderedImageGo.GetComponent<BorderedImage>();
        borderedImage.borderImage = borderedImageGo.GetComponent<Image>();

        GameObject innerImageGo = new GameObject("InnerImage", typeof(Image));
        borderedImage.innerImage = innerImageGo.GetComponent<Image>();
        borderedImage.innerImageRect = innerImageGo.GetComponent<RectTransform>();
        borderedImage.innerImageRect.parent = borderedImageGo.transform;
        borderedImage.innerImageRect.anchorMin = Vector2.zero;
        borderedImage.innerImageRect.anchorMax = Vector2.one;
        borderedImage.innerImageRect.pivot = new Vector2(0.5f, 0.5f);
        borderedImage.SetBorderSize(2);

        borderedImage.innerImage.preserveAspect = true;

        borderedImage.canvasGroup = borderedImageGo.AddComponent<CanvasGroup>();

        return borderedImage;
    }

    public void SetBorderSize(int borderSize)
    {
        innerImageRect.offsetMin = new Vector2(borderSize, borderSize);
        innerImageRect.offsetMax = new Vector2(-borderSize, -borderSize);
    }

    public Image GetInnerImage()
    {
        return innerImage;
    }

    public void SetBorderColor(Color borderColor)
    {
        borderImage.color = borderColor;
    }

    public void DisplaySprite(Sprite sprite)
    {
        innerImage.sprite = sprite;
    }

    public void SetVisible(bool visible)
    {
        borderImage.enabled = visible;
        innerImage.enabled = visible;
    }

    public void SetAlpha(float alpha)
    {
        canvasGroup.alpha = alpha;
    }
}
