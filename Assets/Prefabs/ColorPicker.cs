using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace GuanYao.Tool.SpatialDrawing
{
    public class ColorPicker : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        private enum DragMode
        {
            None,
            SVDial,
            HueRing
        }

        private DragMode currentDragMode = DragMode.None; // 跟踪当前正在拖动的区域

        // --- UI 引用 ---
        [Header("UI References")] public RawImage colorPanelRawImage; // 饱和度/亮度调色板
        public RectTransform colorHandle; // 饱和度/亮度选择图标
        public Image colorResult; // 显示选中颜色的 Image

        public RawImage hueRingRawImage; // 色相环 RawImage
        public RectTransform hueIndicator; // 色相指示器 RectTransform

        public Scrollbar alphaScrollbar; // 色相滚动条
        
        // --- 颜色变量 ---
        [Header("Color Settings")] [Range(0f, 1f)]
        public float currentHue = 0.5f; // 当前的色相值 (0.0 到 1.0)

        private RectTransform colorPanelRect;
        private RectTransform hueRingRect; // hueRingRawImage 的 RectTransform

        private Texture2D colorTexture; // 饱和度/亮度面板的纹理
        private Texture2D hueRingTexture; // 色相环的纹理

        void Awake()
        {
            colorPanelRect = colorPanelRawImage.GetComponent<RectTransform>();
            hueRingRect = hueRingRawImage.GetComponent<RectTransform>();

            // 1. 生成并设置饱和度/亮度面板纹理
            GenerateColorPanelTexture();
            colorPanelRawImage.texture = colorTexture;

            // 2. 生成并设置色相环纹理
            GenerateHueRingTexture();
            hueRingRawImage.texture = hueRingTexture;

            // 3. 初始化时根据当前 Hue 和 Handle 位置计算一次颜色
            CalculateColorFromPosition(colorHandle.localPosition);

            // 4. 更新色相指示器的位置
            UpdateHueIndicatorPosition();
            
            alphaScrollbar.onValueChanged.AddListener((val) =>
            {
                Color color = colorResult.color;
                color.a = val;
                colorResult.color = color;
                MainManager.Instance.SetColorData(color);
            });
        }


        /// <summary>
        /// 检查屏幕上的点是否落在了色相环的可见区域 (内半径和外半径之间)。
        /// </summary>
        private bool IsPointInHueRing(Vector3 screenPosition, Camera eventCamera)
        {
            // 1. 将屏幕坐标转换为 HueRingRawImage 的本地坐标
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    hueRingRect,
                    screenPosition,
                    eventCamera,
                    out Vector2 localPoint))
            {
                // 2. 计算本地点到中心点的距离 (因为我们把锚点设置在中心，localPoint 就是到中心的向量)
                float distance = localPoint.magnitude;

                // 3. 获取 Hue Ring 的半径 (以 Canvas 空间为单位)
                float halfWidth = hueRingRect.rect.width / 2f;

                // 我们在生成纹理时使用了 0.7 的比例来确定内半径
                const float innerRadiusRatio = 0.7f;

                // 计算 Canvas 空间下的实际内半径和外半径
                float canvasOuterRadius = halfWidth;
                float canvasInnerRadius = halfWidth * innerRadiusRatio;

                // 4. 检查距离是否在环形区域内
                return distance >= canvasInnerRadius && distance <= canvasOuterRadius;
            }

            return false;
        }


        // --- 拖动事件实现 (colorPanel) ---
        public void OnBeginDrag(PointerEventData eventData)
        {
            // 检查是否在 Color Panel 区域开始拖动
            if (RectTransformUtility.RectangleContainsScreenPoint(colorPanelRect, eventData.position,
                    eventData.pressEventCamera))
            {
                currentDragMode = DragMode.SVDial;
                OnDrag(eventData);
            }
            // 【关键修改】只在点击点位于实际绘制的环形区域时才启动 HueRing 模式
            else if (IsPointInHueRing(eventData.position, eventData.pressEventCamera))
            {
                currentDragMode = DragMode.HueRing;
                HandleHueRingDrag(eventData.position, eventData.pressEventCamera);
            }
            else
            {
                // 如果点击或拖动开始在 S/V 面板和圆环上都不是，则不处理任何拖动
                currentDragMode = DragMode.None;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (currentDragMode == DragMode.SVDial)
            {
                // 只有当模式是 SVDial 时，才处理 S/V (饱和度/亮度) 的拖动
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        colorPanelRect,
                        eventData.position,
                        eventData.pressEventCamera,
                        out Vector2 localPoint))
                {
                    Vector2 clampedPoint = ClampPointToPanel(localPoint);
                    colorHandle.localPosition = clampedPoint;
                    CalculateColorFromPosition(clampedPoint);
                }
            }
            else if (currentDragMode == DragMode.HueRing)
            {
                // 只有当模式是 HueRing 时，才处理色相环的拖动
                // 注意：这里我们不再检查指针是否在 Rect 内，因为我们已在 BeginDrag 中锁定了模式
                HandleHueRingDrag(eventData.position, eventData.pressEventCamera);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // 拖动结束时，重置模式
            currentDragMode = DragMode.None;
        }

        // --- 辅助方法 (colorPanel) ---

        private Vector2 ClampPointToPanel(Vector2 point)
        {
            float halfWidth = colorPanelRect.rect.width / 2f;
            float halfHeight = colorPanelRect.rect.height / 2f;

            point.x = Mathf.Clamp(point.x, -halfWidth, halfWidth);
            point.y = Mathf.Clamp(point.y, -halfHeight, halfHeight);

            return point;
        }

        private void CalculateColorFromPosition(Vector2 localPos)
        {
            float panelWidth = colorPanelRect.rect.width;
            float panelHeight = colorPanelRect.rect.height;

            float S = (localPos.x + (panelWidth / 2f)) / panelWidth;
            float V = (localPos.y + (panelHeight / 2f)) / panelHeight;

            S = Mathf.Clamp01(S);
            V = Mathf.Clamp01(V);

            Color selectedColor = Color.HSVToRGB(currentHue, S, V);
            selectedColor.a = alphaScrollbar.value;
            // Debug.Log(selectedColor.ToString());
            colorResult.color = selectedColor;
            MainManager.Instance.SetColorData(selectedColor);
            // colorHandle.GetComponent<Image>().color = (selectedColor.grayscale > 0.5f) ? Color.black : Color.white; 
        }

        // --- 核心改动 1: 生成色彩板纹理 ---
        private void GenerateColorPanelTexture()
        {
            int textureWidth = (int)colorPanelRect.rect.width;
            int textureHeight = (int)colorPanelRect.rect.height;

            if (textureWidth == 0 || textureHeight == 0)
            {
                textureWidth = 256;
                textureHeight = 256;
            }

            if (colorTexture == null || colorTexture.width != textureWidth || colorTexture.height != textureHeight)
            {
                colorTexture = new Texture2D(textureWidth, textureHeight);
            }

            Color[] pixels = new Color[textureWidth * textureHeight];
            for (int y = 0; y < textureHeight; y++)
            {
                for (int x = 0; x < textureWidth; x++)
                {
                    float s = (float)x / (textureWidth - 1);
                    float v = (float)y / (textureHeight - 1);
                    pixels[y * textureWidth + x] = Color.HSVToRGB(currentHue, s, v);
                }
            }

            colorTexture.SetPixels(pixels);
            colorTexture.Apply();
        }

        // --- 核心改动 2: 生成色相环纹理 ---
        private void GenerateHueRingTexture()
        {
            int textureSize = (int)hueRingRect.rect.width; // 假设是正方形

            if (textureSize == 0)
            {
                textureSize = 256;
            }

            if (hueRingTexture == null || hueRingTexture.width != textureSize || hueRingTexture.height != textureSize)
            {
                hueRingTexture = new Texture2D(textureSize, textureSize);
            }

            Color[] pixels = new Color[textureSize * textureSize];
            Vector2 center = new Vector2(textureSize / 2f, textureSize / 2f);
            float outerRadius = textureSize / 2f;
            float innerRadius = outerRadius * 0.7f; // 色相环的内半径，可调整

            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    Vector2 currentPos = new Vector2(x, y);
                    Vector2 diff = currentPos - center;
                    float distance = diff.magnitude; // 当前像素到中心的距离

                    if (distance > innerRadius && distance < outerRadius)
                    {
                        // 在环形区域内
                        float angle = Mathf.Atan2(diff.y, diff.x); // 弧度
                        float hue = (angle / (2f * Mathf.PI) + 1f) % 1f; // 将角度映射到 0-1 的 Hue (加1并取模是为了处理负角度)
                        pixels[y * textureSize + x] = Color.HSVToRGB(hue, 1f, 1f); // 饱和度1，亮度1
                    }
                    else
                    {
                        pixels[y * textureSize + x] = Color.clear; // 透明
                    }
                }
            }

            hueRingTexture.SetPixels(pixels);
            hueRingTexture.Apply();
        }

        // --- 核心改动 3: 更新色相指示器的位置 ---
        private void UpdateHueIndicatorPosition()
        {
            // 根据 currentHue 计算指示器在色相环上的角度
            float angle = currentHue * 2f * Mathf.PI; // 转换为弧度 (0-1 -> 0-2PI)
            float radius = hueRingRect.rect.width * 0.4f; // 指示器距离中心点的半径，可以调整到色相环的中间位置

            // 计算指示器的本地坐标
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;

            hueIndicator.localPosition = new Vector2(x, y);

            // 可选: 让指示器自身的颜色与它指向的色相保持一致
            // hueIndicator.GetComponent<Image>().color = Color.HSVToRGB(currentHue, 1f, 1f);
        }

        // --- 核心改动 4: 处理色相环拖动事件 ---
        private void HandleHueRingDrag(Vector3 screenPosition, Camera eventCamera)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    hueRingRect,
                    screenPosition,
                    eventCamera,
                    out Vector2 localPoint))
            {
                // 将本地坐标转换为相对于中心点的向量
                Vector2 diff = localPoint; // hueRingRect 的锚点通常在中心，所以 localPoint 就是相对于中心的

                float angle = Mathf.Atan2(diff.y, diff.x);
                float newHue = (angle / (2f * Mathf.PI) + 1f) % 1f; // 计算新的 Hue 值

                SetHue(newHue); // 调用 SetHue 来更新并重绘
            }
        }

        // SetHue 方法的更新 (已包含在之前版本，这里只是再次强调它的作用)
        public void SetHue(float newHue)
        {
            currentHue = newHue;

            GenerateColorPanelTexture(); // 重新生成饱和度/亮度面板纹理
            colorPanelRawImage.texture = colorTexture;

            CalculateColorFromPosition(colorHandle.localPosition); // 重新计算当前选中的颜色
            UpdateHueIndicatorPosition(); // 更新色相指示器的位置
        }

        void OnDestroy()
        {
            if (colorTexture != null) Destroy(colorTexture);
            if (hueRingTexture != null) Destroy(hueRingTexture);
        }
    }
}