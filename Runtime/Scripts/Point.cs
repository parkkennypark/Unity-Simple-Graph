using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Point : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private GraphController graphController;
    private Vector2 point;

    public void SetupPoint(Vector2 point, GraphController gC)
    {
        graphController = gC;
        this.point = point;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        graphController.DisplayInspectorPoint(this);
        transform.localScale = new Vector3(1.3f, 1.3f, 1f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        graphController.ClearInspectorPoint();
        transform.localScale = new Vector3(1f, 1f, 1f);
    }

    public Vector2 GetPoint()
    {
        return point;
    }
}
