using UnityEngine;
using UnityEngine.UI; // 如果使用UI Text
using TMPro; // 如果使用TextMeshPro
using System.Collections.Generic;
using UnityEditor;

public class SquareRoleController : MonoBehaviour
{
    public Transform squareRole; // 2D物体
    public RectTransform nameLabel; // UI文本的RectTransform
    public SpriteRenderer squareRenderer; // 方块的SpriteRenderer（用于颜色）
    public TextMeshProUGUI nameText; // TextMeshProUGUI，如果使用TextMeshPro
                                     // public Text nameText; // 如果使用UI Text

    private List<string> names = new List<string> { "Alpha", "Bravo", "Charlie", "Delta", "Echo" }; // 可用的名字列表

    public void RandomName()
    {
        // 随机设置名字
        if (nameText != null) // 检查是否使用TextMeshPro
        {
            string randomName = names[Random.Range(0, names.Count)];
            nameText.text = randomName;
            Debug.Log(randomName);
        }
    }

    public void SetName(string name)
    {
        // 随机设置名字
        if (nameText != null) // 检查是否使用TextMeshPro
        {
            nameText.text = name;
            Debug.Log(name);
        }
    }

    public void RandomColor()
    {
        // 随机设置颜色
        Color randomColor = new Color(Random.value, Random.value, Random.value);
        if (squareRenderer != null)
        {
            squareRenderer.color = randomColor;
        }
    }

    public void Move(long dt, string cmd)
    {
        float horizontal = 0;
        float vertical = 0;

        if ("UP" == cmd)
        {
            horizontal = 0;
            vertical = 1;
        }
        else if ("DOWN" == cmd)
        {
            horizontal = 0;
            vertical = -1;
        }
        else if ("LEFT" == cmd)
        {
            horizontal = - 1;
            vertical = 0;
        }
        else if ("RIGHT" == cmd)
        {
            horizontal = 1;
            vertical = 0;
        }

        if (horizontal != 0 || vertical != 0)
        {
            horizontal = horizontal;
            vertical = vertical;
        }

        Vector2 movement = new Vector2(horizontal, vertical) * 0.001f * dt;
        transform.Translate(movement);
    }
}
