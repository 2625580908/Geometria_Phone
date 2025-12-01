using System;
using UnityEngine;
using UnityEngine.UI;

public class KeyboardHeightTracker : MonoBehaviour
{
    public Text info;
    public GameObject canvas;
    public InputField inputField;
    public float keyboardHeight = 0f;

    private void Start()
    {
        inputField.onValueChanged.AddListener((ValidationLevel) =>
        {
            // 1. 获取当前的 offsetMin (左下角相对于最小锚点的偏移)
            Vector2 newOffsetMin =   canvas.GetComponent<RectTransform>().offsetMin;
            // 2. 只修改 Y 分量，即底部值
            newOffsetMin.y = keyboardHeight;
            // 3. 将修改后的 Vector2 重新赋值给 offsetMin
            canvas.GetComponent<RectTransform>().offsetMin = newOffsetMin;
        });
        
        inputField.onEndEdit.AddListener((val) =>
        {
            // 1. 获取当前的 offsetMin (左下角相对于最小锚点的偏移)
            Vector2 newOffsetMin =   canvas.GetComponent<RectTransform>().offsetMin;
            // 2. 只修改 Y 分量，即底部值
            newOffsetMin.y = 0f;
            // 3. 将修改后的 Vector2 重新赋值给 offsetMin
            canvas.GetComponent<RectTransform>().offsetMin = newOffsetMin; 
        });
    }
}
