using System.Collections.Generic;
using UnityEngine;

// Điều khiển các mũi tên (con của object này) chạy dọc quỹ đạo belt để tạo cảm giác
// belt đang di chuyển. Chỉ cần DATA quỹ đạo (waypoints + cornerRadius) truyền vào qua
// Init — tự dựng BeltPath riêng, KHÔNG phụ thuộc BeltController. Tốc độ mũi tên độc lập,
// không cần sync nhịp với belt.
public class BeltArrowAnimator : MonoBehaviour
{
    // Số đoạn nội suy mỗi góc bo — khớp độ mượt với belt (BeltController dùng 8).
    private const int CornerSegments = 8;

    [Tooltip("Tốc độ mũi tên chạy dọc belt (arc-length / giây).")]
    [SerializeField] private float _speed = 12f;

    [Tooltip("Khoảng cách arc-length giữa 2 mũi tên liên tiếp.")]
    [SerializeField] private float _arrowSpacing = 3f;

    [Tooltip("Trục 'mũi' của model mũi tên; sẽ được xoay khớp theo hướng belt.")]
    [SerializeField] private Vector3 _arrowForward = Vector3.forward;

    private readonly List<Transform> _arrows = new List<Transform>();
    private BeltPath _path;
    private float _headDistance;
    private bool _isRunning;

    private void Awake()
    {
        CacheArrows();
    }

    // Đọc các mũi tên là con trực tiếp của object này (arrowHolder).
    private void CacheArrows()
    {
        _arrows.Clear();
        foreach (Transform child in transform)
        {
            _arrows.Add(child);
        }
    }

    // Dựng quỹ đạo từ data belt của level. Gọi lúc LoadLevel (GameContext), cùng nguồn
    // waypoint với BeltController nên mũi tên chạy trùng đường belt.
    public void Init(IReadOnlyList<Vector3> waypoints, float cornerRadius)
    {
        _path = new BeltPath(waypoints, cornerRadius, CornerSegments);
        _headDistance = 0f;

        if (_arrows.Count == 0)
        {
            CacheArrows();
        }
    }

    // Bắt đầu cho mũi tên chạy (gọi khi vào gameplay).
    public void Play()
    {
        _isRunning = true;
    }

    // Dừng mũi tên (gọi khi win/lose hoặc rời gameplay). Giữ nguyên vị trí hiện tại.
    public void Stop()
    {
        _isRunning = false;
    }

    private void Update()
    {
        if (!_isRunning || _path == null || !_path.IsValid || _arrows.Count == 0)
        {
            return;
        }

        float length = _path.TotalLength;
        // Mũi tên đầu tiến đều dọc path và cuộn vòng ở cuối để chạy lặp vô hạn.
        _headDistance = Mathf.Repeat(_headDistance + _speed * Time.deltaTime, length);

        for (int i = 0; i < _arrows.Count; i++)
        {
            float distance = Mathf.Repeat(_headDistance + i * _arrowSpacing, length);
            Transform arrow = _arrows[i];
            arrow.position = _path.GetPositionAtDistance(distance);
            arrow.rotation = Quaternion.FromToRotation(_arrowForward, _path.GetDirectionAtDistance(distance));
        }
    }
}