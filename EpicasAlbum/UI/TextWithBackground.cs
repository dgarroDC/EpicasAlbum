using UnityEngine;
using UnityEngine.UI;

namespace EpicasAlbum.UI;

public class TextWithBackground : MonoBehaviour
{
    public VerticalLayoutGroup layoutGroup;
    public Image backgroundImage;
    public Text innerText;

    public static TextWithBackground Create()
    {
        GameObject go = new GameObject("TextWithBackground", typeof(Image), typeof(TextWithBackground), 
            typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        TextWithBackground textWithBackground = go.GetComponent<TextWithBackground>();
        textWithBackground.backgroundImage = go.GetComponent<Image>();
        textWithBackground.layoutGroup = go.GetComponent<VerticalLayoutGroup>();
        textWithBackground.layoutGroup.childControlWidth = true;
        textWithBackground.layoutGroup.childControlHeight = true;
        textWithBackground.layoutGroup.childForceExpandWidth = true;
        textWithBackground.layoutGroup.childForceExpandHeight = true;
        textWithBackground.layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        ContentSizeFitter sizeFitter = go.GetComponent<ContentSizeFitter>();
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        GameObject innerTextGo = new GameObject("InnerText", typeof(Text));
        innerTextGo.transform.parent = go.transform;
        textWithBackground.innerText = innerTextGo.GetComponent<Text>();
        textWithBackground.innerText.color = Color.black;

        return textWithBackground;
    }

    public void SetBackgroundColor(Color color)
    {
        backgroundImage.color = color;
    }

    public void SetText(string text)
    {
        innerText.text = text;
    }

    public void SetFontSize(int size)
    {
        innerText.fontSize = size;
    }

    public void SetFont(Font font)
    {
        innerText.font = font;
    }

    public void SetPadding(int padding)
    {
        layoutGroup.padding = new RectOffset(padding, padding, padding, padding);
    }

}
