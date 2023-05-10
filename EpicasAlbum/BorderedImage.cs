﻿using UnityEngine;
using UnityEngine.UI;

namespace EpicasAlbum;

public class BorderedImage : MonoBehaviour
{
    public Image borderImage;
    public RectTransform innerImageRect;
    public Image innerImage;

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

    public void SetImage(Texture2D texture)
    {
        // TODO: Cache sprites? Or at least take Sprite so big image resuses the one in grid!
        innerImage.sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    public void SetVisible(bool visible)
    {
        borderImage.enabled = visible;
        innerImage.enabled = visible;
    }
}