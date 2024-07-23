using UnityEngine;
using UnityEngine.UI; // 如果使用UI Text
using TMPro; // 如果使用TextMeshPro

public class FollowObject : MonoBehaviour
{
    public Transform objectToFollow; // 要跟随的2D物体
    public RectTransform uiElement; // UI文本的RectTransform

    void Update()
    {
        if (objectToFollow != null)
        {
            // 将世界坐标转换为屏幕坐标
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(objectToFollow.position);
            uiElement.position = screenPosition;
        }
    }
}
