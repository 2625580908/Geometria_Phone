using UnityEngine;

public class MobileCameraController : MonoBehaviour
{
    [Header("目标与距离")]
    public Transform target;                 // 旋转围绕的目标
    public float distance = 5f;              // 起始距离
    public float minDistance = 1.5f;
    public float maxDistance = 20f;

    [Header("速度")]
    public float rotateSpeed = 0.2f;         // 旋转灵敏度（触摸）
    public float zoomSpeed = 0.02f;          // 捏合灵敏度

    [Header("垂直限制")]
    public float minYAngle = -20f;
    public float maxYAngle = 80f;

    float currentX = 0f; // 偏航角
    float currentY = 20f; // 俯仰角

    void Start()
    {
        if (target == null)
        {
            // 若未指定目标，使用世界原点
            GameObject go = new GameObject("CameraTarget");
            go.transform.position = Vector3.zero;
            target = go.transform;
        }

        // 初始化角度
        Vector3 angles = transform.eulerAngles;
        currentX = angles.y;
        currentY = angles.x;
        UpdateCameraPosition(); 
    }

    void Update()
    {
        // --- 移动触摸处理 ---
        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Moved)
            {
                Vector2 d = t.deltaPosition;
                currentX += d.x * rotateSpeed;
                currentY -= d.y * rotateSpeed;
                currentY = Mathf.Clamp(currentY, minYAngle, maxYAngle);
                UpdateCameraPosition();
            }
        }
        else if (Input.touchCount == 2)
        {
            // 捏合缩放
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            Vector2 prev0 = t0.position - t0.deltaPosition;
            Vector2 prev1 = t1.position - t1.deltaPosition;
            float prevDist = (prev0 - prev1).magnitude;
            float curDist = (t0.position - t1.position).magnitude;
            float diff = prevDist - curDist;

            distance += diff * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
            UpdateCameraPosition();
        }

        // --- 编辑器 / 鼠标回退测试 ---
#if UNITY_EDITOR
        if (Input.GetMouseButton(0))
        {
            Vector2 d = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            currentX += d.x * rotateSpeed * 8f;
            currentY -= d.y * rotateSpeed * 8f;
            currentY = Mathf.Clamp(currentY, minYAngle, maxYAngle);
            UpdateCameraPosition();
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            distance -= scroll * 5f;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
            UpdateCameraPosition();
        }
#endif
    }

    void UpdateCameraPosition()
    {
        Quaternion rot = Quaternion.Euler(currentY, currentX, 0);
        Vector3 negForward = rot * new Vector3(0f, 0f, -distance);
        transform.position = (target.position) + negForward;
        transform.rotation = rot;
    }
}