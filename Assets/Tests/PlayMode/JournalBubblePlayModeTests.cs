using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JournalBubblePlayModeTests
{
    [Test]
    public void FramedChatBubble_PreservesSuppliedSprite()
    {
        var root = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
        var templateObject = new GameObject("Template", typeof(RectTransform), typeof(TextMeshProUGUI));
        templateObject.transform.SetParent(root.transform, false);
        var template = templateObject.GetComponent<TMP_Text>();
        template.gameObject.SetActive(false);
        var texture = new Texture2D(8, 8);
        var sprite = Sprite.Create(texture, new Rect(0, 0, 8, 8), Vector2.one * 0.5f, 1f,
                                   0, SpriteMeshType.FullRect, Vector4.one);

        ChatBubbleFactory.Add((RectTransform)root.transform, template, "Hello", false,
                              Color.white, Color.black, sprite, out GameObject row);

        Assert.IsNotNull(row);
        Assert.AreSame(sprite, row.transform.Find("Bubble").GetComponent<Image>().sprite);
        Object.DestroyImmediate(sprite);
        Object.DestroyImmediate(texture);
        Object.DestroyImmediate(root);
    }
}
