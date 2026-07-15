using System;
using Cysharp.Threading.Tasks;
using Extension;
using UnityEngine;

[Service(nameof(IUIManager))]
public interface IUIManager
{
    // Người chơi bấm Start / Retry. GameContext lắng nghe để điều phối luồng game.
    event Action OnStartClicked;
    event Action OnRetryClicked;

    void ShowStart();
    void HideStart();

    // Hiện màn kết quả ở trạng thái Win (true) hoặc Lose (false).
    void ShowResult(bool isWin);
    void HideResult();

    // Transition: cloud chụm lại che kín (Cover) rồi tản ra (Reveal).
    UniTask PlayCoverAsync();
    UniTask PlayRevealAsync();
}

// Điểm quản lý tập trung 3 màn UI (Start / Transition / Result). Kéo 3 screen vào
// inspector. Bản thân UIManager được GameContext gán và Provide làm service IUIManager.
// UIManager chỉ lo hiện/ẩn + bắn event bấm nút; luồng game do GameContext quyết định.
public class UIManager : MonoBehaviour, IUIManager
{
    [SerializeField] private TransitionScreen _transitionScreen;
    [SerializeField] private StartScreen _startScreen;
    [SerializeField] private ResultScreen _resultScreen;

    public event Action OnStartClicked;
    public event Action OnRetryClicked;

    private void Awake()
    {
        if (_startScreen != null)
        {
            _startScreen.OnStartClicked += HandleStartClicked;
            _startScreen.Hide();
        }

        if (_resultScreen != null)
        {
            _resultScreen.OnRetryClicked += HandleRetryClicked;
            _resultScreen.Hide();
        }
    }

    private void OnDestroy()
    {
        if (_startScreen != null)
        {
            _startScreen.OnStartClicked -= HandleStartClicked;
        }

        if (_resultScreen != null)
        {
            _resultScreen.OnRetryClicked -= HandleRetryClicked;
        }
    }

    public void ShowStart() => _startScreen.Show();
    public void HideStart() => _startScreen.Hide();
    public void ShowResult(bool isWin) => _resultScreen.ShowResult(isWin);
    public void HideResult() => _resultScreen.Hide();
    public UniTask PlayCoverAsync() => _transitionScreen.PlayCoverAsync();
    public UniTask PlayRevealAsync() => _transitionScreen.PlayRevealAsync();

    private void HandleStartClicked() => OnStartClicked?.Invoke();
    private void HandleRetryClicked() => OnRetryClicked?.Invoke();
}