using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

// Màn transition: vài tấm cloud chụm lại che kín màn hình (Cover) rồi tản ra (Reveal).
//
// Cách dùng trong editor: đặt các cloud (RectTransform) ở LAYOUT CHE KÍN màn hình rồi
// kéo vào _clouds. Script tự tính vị trí "tản ra" = đẩy mỗi cloud ra xa tâm theo hướng
// của nó một đoạn SpreadDistance (px). Vì vậy không cần cấu hình 2 vị trí thủ công.
public class TransitionScreen : UIScreen
{
    private const float CoverDuration = 0.35f;
    private const float RevealDuration = 0.35f;
    // Khoảng đẩy cloud ra khỏi màn hình khi tản ra (px anchored). Đủ lớn để lộ hết sân.
    private const float SpreadDistance = 2500f;

    [SerializeField] private RectTransform[] _clouds;

    // Vị trí che kín (đọc từ layout trong editor) và vị trí tản ra (tự tính).
    private Vector2[] _coveredPositions;
    private Vector2[] _openPositions;

    private void Awake()
    {
        CachePositions();
        // Khởi đầu ở trạng thái tản ra + ẩn, để không che sân lúc vào game.
        ApplyOpenInstant();
        Hide();
    }

    private void CachePositions()
    {
        int count = _clouds != null ? _clouds.Length : 0;
        _coveredPositions = new Vector2[count];
        _openPositions = new Vector2[count];

        for (int i = 0; i < count; i++)
        {
            Vector2 covered = _clouds[i].anchoredPosition;
            _coveredPositions[i] = covered;

            // Hướng đẩy ra: từ tâm màn hình tới cloud. Cloud nằm ngay tâm thì đẩy lên.
            Vector2 dir = covered.sqrMagnitude > 0.01f ? covered.normalized : Vector2.up;
            _openPositions[i] = covered + dir * SpreadDistance;
        }
    }

    // Cloud chụm lại che kín màn hình.
    public async UniTask PlayCoverAsync()
    {
        Show();
        ApplyOpenInstant();
        await AnimateTo(_coveredPositions, CoverDuration, Ease.OutQuad);
    }

    // Cloud tản ra lộ sân; xong thì ẩn màn transition.
    public async UniTask PlayRevealAsync()
    {
        Show();
        await AnimateTo(_openPositions, RevealDuration, Ease.InQuad);
        Hide();
    }

    private void ApplyOpenInstant()
    {
        for (int i = 0; i < _clouds.Length; i++)
        {
            _clouds[i].anchoredPosition = _openPositions[i];
        }
    }

    // Tween đồng thời tất cả cloud tới target rồi hoàn tất. Dùng UniTaskCompletionSource
    // để await được mà không phụ thuộc module DOTween-UniTask.
    private UniTask AnimateTo(Vector2[] targets, float duration, Ease ease)
    {
        if (_clouds == null || _clouds.Length == 0)
        {
            return UniTask.CompletedTask;
        }

        var completion = new UniTaskCompletionSource();
        Sequence sequence = DOTween.Sequence();
        for (int i = 0; i < _clouds.Length; i++)
        {
            sequence.Join(_clouds[i].DOAnchorPos(targets[i], duration).SetEase(ease));
        }

        sequence.OnComplete(() => completion.TrySetResult());
        return completion.Task;
    }
}