using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

// Màn transition: các tấm cloud có 2 layout cố định.
// - Open (tản ra, lộ sân): OpenPositions.
// - Covered (chụm lại che kín màn hình): CoveredPositions.
//
// Mở đầu cloud nằm ở Open. PlayCover đưa chúng về Covered; PlayReveal đưa về lại Open.
// Thời gian tween chỉnh được từ editor qua _coverDuration / _revealDuration.
public class TransitionScreen : UIScreen
{
    [SerializeField] private RectTransform[] _clouds;
    [SerializeField] private float _coverDuration = 3f;
    [SerializeField] private float _revealDuration = 3f;

    // Vị trí che kín (chụm giữa). Thứ tự khớp với thứ tự object con trong hierarchy (8 cloud).
    private static readonly Vector2[] CoveredPositions =
    {
        new Vector2(-88.43f, -775f),    // Cloud
        new Vector2(4.39f, 589.93f),    // Cloud 1
        new Vector2(-222.31f, 277.02f), // Cloud 2
        new Vector2(13f, -63f),         // Cloud 3
        new Vector2(-266f, -721f),      // Cloud 4
        new Vector2(-48f, 50f),         // Cloud 5
        new Vector2(174f, -508f),       // Cloud 6
        new Vector2(220f, 385f),        // Cloud 7
    };

    // Vị trí tản ra (đẩy xa tâm, lộ sân). Cùng thứ tự con với CoveredPositions.
    private static readonly Vector2[] OpenPositions =
    {
        new Vector2(-73f, -2108f),   // Cloud
        new Vector2(4.39f, 1860f),   // Cloud 1
        new Vector2(-1497f, 277f),   // Cloud 2
        new Vector2(1411f, 16.47f),  // Cloud 3
        new Vector2(-1599f, -712f),  // Cloud 4
        new Vector2(-1338f, 50f),    // Cloud 5
        new Vector2(1528f, -595f),   // Cloud 6
        new Vector2(1585f, 385f),    // Cloud 7
    };

    private void Awake()
    {
        // Khởi đầu ở trạng thái tản ra + ẩn, để không che sân lúc vào game.
        ApplyPositionsInstant(OpenPositions);
        Hide();
    }

    // Cloud chụm lại che kín màn hình.
    public async UniTask PlayCoverAsync()
    {
        Show();
        ApplyPositionsInstant(OpenPositions);
        await AnimateTo(CoveredPositions, _coverDuration, Ease.OutQuad);
    }

    // Cloud tản ra lộ sân; xong thì ẩn màn transition.
    public async UniTask PlayRevealAsync()
    {
        Show();
        await AnimateTo(OpenPositions, _revealDuration, Ease.InQuad);
        Hide();
    }

    private void ApplyPositionsInstant(Vector2[] positions)
    {
        int count = CloudCount(positions);
        for (int i = 0; i < count; i++)
        {
            _clouds[i].anchoredPosition = positions[i];
        }
    }

    // Tween đồng thời tất cả cloud tới target rồi hoàn tất. Dùng UniTaskCompletionSource
    // để await được mà không phụ thuộc module DOTween-UniTask.
    private UniTask AnimateTo(Vector2[] targets, float duration, Ease ease)
    {
        int count = CloudCount(targets);
        if (count == 0)
        {
            return UniTask.CompletedTask;
        }

        var completion = new UniTaskCompletionSource();
        Sequence sequence = DOTween.Sequence();
        for (int i = 0; i < count; i++)
        {
            sequence.Join(_clouds[i].DOAnchorPos(targets[i], duration).SetEase(ease));
        }

        sequence.OnComplete(() => completion.TrySetResult());
        return completion.Task;
    }

    // Số cloud xử lý an toàn khi số RectTransform gán trong editor và số vị trí không khớp.
    private int CloudCount(Vector2[] positions)
    {
        if (_clouds == null)
        {
            return 0;
        }

        return Mathf.Min(_clouds.Length, positions.Length);
    }
}