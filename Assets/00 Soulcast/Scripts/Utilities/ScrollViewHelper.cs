using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class ScrollViewHelper
{
    public static void SetupInvisibleScrollView(GameObject scrollViewObject)
    {
        var invisibleScroll = scrollViewObject.GetComponent<InvisibleScrollView>();
        if (invisibleScroll == null)
            invisibleScroll = scrollViewObject.AddComponent<InvisibleScrollView>();

        var scrollRect = scrollViewObject.GetComponent<ScrollRect>();
        if (scrollRect != null)
        {
            // Hide scrollbars but keep functionality
            if (scrollRect.horizontalScrollbar != null)
                scrollRect.horizontalScrollbar.gameObject.SetActive(false);
            if (scrollRect.verticalScrollbar != null)
                scrollRect.verticalScrollbar.gameObject.SetActive(false);
        }
    }

    public static void CreateScrollableContent(Transform parent, Vector2 contentSize)
    {
        // Create ScrollView structure
        GameObject scrollView = new GameObject("InvisibleScrollView");
        scrollView.transform.SetParent(parent);

        // Add components
        var rectTransform = scrollView.AddComponent<RectTransform>();
        var scrollRect = scrollView.AddComponent<ScrollRect>();
        var invisibleScroll = scrollView.AddComponent<InvisibleScrollView>();

        // Create Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform);
        var viewportRect = viewport.AddComponent<RectTransform>();
        viewport.AddComponent<Image>().color = new Color(1, 1, 1, 0.01f); // Almost transparent
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        // Create Content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform);
        var contentRect = content.AddComponent<RectTransform>();

        // Setup RectTransforms
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(0, 1);
        contentRect.pivot = new Vector2(0, 1);
        contentRect.sizeDelta = contentSize;

        // Configure ScrollRect
        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = true;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
    }
}
