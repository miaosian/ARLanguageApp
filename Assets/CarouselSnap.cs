using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[RequireComponent(typeof(ScrollRect))]
public class CarouselSnap : MonoBehaviour, IEndDragHandler
{
    public float snapSpeed = 10f;
    private ScrollRect scrollRect;
    private RectTransform content;
    private List<RectTransform> items = new List<RectTransform>();
    private bool isSnapping;
    private float targetX;

    void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
        content    = scrollRect.content;

        // Cache all immediate children as items
        foreach (Transform child in content)
            if (child is RectTransform rt)
                items.Add(rt);
    }

    void Update()
    {
        if (!isSnapping) return;

        // Smoothly move content.x toward targetX
        float newX = Mathf.Lerp(content.anchoredPosition.x, targetX, Time.deltaTime * snapSpeed);
        content.anchoredPosition = new Vector2(newX, content.anchoredPosition.y);

        if (Mathf.Abs(newX - targetX) < 0.01f)
        {
            content.anchoredPosition = new Vector2(targetX, content.anchoredPosition.y);
            isSnapping = false;
        }
    }

    // Called when the user releases the drag
    public void OnEndDrag(PointerEventData eventData)
    {
        // Find the item whose center is nearest the viewport’s center
        float closestDistance = float.MaxValue;
        RectTransform closest = null;
        float viewportWidth = ((RectTransform)scrollRect.viewport).rect.width;
        Vector2 viewportCenter = new Vector2(
            -content.anchoredPosition.x + viewportWidth / 2,
            0);

        foreach (var item in items)
        {
            // item position relative to content
            Vector2 itemCenter = item.anchoredPosition + item.rect.size * item.pivot;
            float dist = Mathf.Abs(itemCenter.x - viewportCenter.x);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closest = item;
            }
        }

        if (closest != null)
        {
            // Compute the new content.x so that `closest` sits at viewport’s center
            float desiredContentX = -(closest.anchoredPosition.x - viewportWidth / 2 + closest.rect.width * (closest.pivot.x - 0.5f));
            targetX = desiredContentX;
            isSnapping = true;
        }
    }
}
