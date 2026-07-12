using System.Collections.Generic;
using UnityEngine;

// Bake danh sách waypoint thành polyline dày với góc BO TRÒN (quadratic bezier), rồi
// cho sample vị trí world theo arc-length. Dùng cho belt hình chữ U: card tiến theo
// `distance` dọc path thay vì cộng thẳng trục X.
public class BeltPath
{
    // Các điểm đã bake (đoạn thẳng + cung bo). _cumLength[i] = tổng độ dài tới _points[i].
    private readonly List<Vector3> _points = new List<Vector3>();
    private readonly List<float> _cumLength = new List<float>();

    public float TotalLength { get; private set; }
    public bool IsValid => _points.Count >= 2 && TotalLength > 0f;

    public BeltPath(IReadOnlyList<Vector3> waypoints, float cornerRadius, int cornerSegments)
    {
        Bake(waypoints, Mathf.Max(0f, cornerRadius), Mathf.Max(1, cornerSegments));
        ComputeLengths();
    }

    // Trả vị trí world tại arc-length `distance` (clamp trong [0, TotalLength]).
    public Vector3 GetPositionAtDistance(float distance)
    {
        if (_points.Count == 0)
        {
            return Vector3.zero;
        }

        if (!IsValid)
        {
            return _points[0];
        }

        float d = Mathf.Clamp(distance, 0f, TotalLength);

        // Số điểm nhỏ nên duyệt tuyến tính là đủ nhanh.
        for (int i = 1; i < _points.Count; i++)
        {
            if (d <= _cumLength[i])
            {
                float segLen = _cumLength[i] - _cumLength[i - 1];
                float t = segLen > 0f ? (d - _cumLength[i - 1]) / segLen : 0f;
                return Vector3.Lerp(_points[i - 1], _points[i], t);
            }
        }

        return _points[_points.Count - 1];
    }

    // Hướng đi (tangent chuẩn hoá) tại arc-length `distance`. Mặc định +X nếu path
    // chưa hợp lệ, để card không bị xoay lung tung.
    public Vector3 GetDirectionAtDistance(float distance)
    {
        if (!IsValid)
        {
            return Vector3.right;
        }

        float d = Mathf.Clamp(distance, 0f, TotalLength);

        for (int i = 1; i < _points.Count; i++)
        {
            if (d <= _cumLength[i])
            {
                Vector3 dir = _points[i] - _points[i - 1];
                return dir.sqrMagnitude > 0f ? dir.normalized : Vector3.right;
            }
        }

        Vector3 last = _points[_points.Count - 1] - _points[_points.Count - 2];
        return last.sqrMagnitude > 0f ? last.normalized : Vector3.right;
    }

    private void Bake(IReadOnlyList<Vector3> waypoints, float cornerRadius, int cornerSegments)
    {
        if (waypoints == null || waypoints.Count == 0)
        {
            return;
        }

        _points.Add(waypoints[0]);

        // Bo tròn tại từng đỉnh trong (bỏ đỉnh đầu/cuối vì là điểm mút, không cua).
        for (int i = 1; i < waypoints.Count - 1; i++)
        {
            Vector3 prev = waypoints[i - 1];
            Vector3 corner = waypoints[i];
            Vector3 next = waypoints[i + 1];

            Vector3 toPrev = corner - prev;
            Vector3 toNext = next - corner;

            // Cắt tối đa nửa cạnh ngắn hơn để fillet không tràn qua waypoint kế bên.
            float cut = Mathf.Min(cornerRadius, 0.5f * toPrev.magnitude, 0.5f * toNext.magnitude);

            Vector3 enter = corner - toPrev.normalized * cut; // điểm vào cua
            Vector3 exit = corner + toNext.normalized * cut;  // điểm ra cua

            _points.Add(enter);
            for (int s = 1; s < cornerSegments; s++)
            {
                float t = (float)s / cornerSegments;
                _points.Add(QuadraticBezier(enter, corner, exit, t));
            }
            _points.Add(exit);
        }

        _points.Add(waypoints[waypoints.Count - 1]);
    }

    private void ComputeLengths()
    {
        _cumLength.Clear();
        if (_points.Count == 0)
        {
            TotalLength = 0f;
            return;
        }

        float acc = 0f;
        _cumLength.Add(0f);
        for (int i = 1; i < _points.Count; i++)
        {
            acc += Vector3.Distance(_points[i - 1], _points[i]);
            _cumLength.Add(acc);
        }

        TotalLength = acc;
    }

    private static Vector3 QuadraticBezier(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        float u = 1f - t;
        return u * u * a + 2f * u * t * b + t * t * c;
    }
}