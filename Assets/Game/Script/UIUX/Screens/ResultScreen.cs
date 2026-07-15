using System;
using UnityEngine;
using UnityEngine.UI;

// Màn kết quả: 2 trạng thái Win/Lose (bật đúng 1 root) + 1 button Retry. Bắn
// OnRetryClicked cho UIManager. Kéo 2 root và button vào inspector.
public class ResultScreen : UIScreen
{
    [SerializeField] private GameObject _winRoot;
    [SerializeField] private GameObject _loseRoot;
    [SerializeField] private Button _retryButton;

    public event Action OnRetryClicked;

    private void Awake()
    {
        if (_retryButton != null)
        {
            _retryButton.onClick.AddListener(HandleClick);
        }
    }

    private void OnDestroy()
    {
        if (_retryButton != null)
        {
            _retryButton.onClick.RemoveListener(HandleClick);
        }
    }

    // Hiện màn kết quả ở đúng trạng thái. isWin = true -> hiện root Win, ẩn Lose.
    public void ShowResult(bool isWin)
    {
        Show();
        if (_winRoot != null)
        {
            _winRoot.SetActive(isWin);
        }

        if (_loseRoot != null)
        {
            _loseRoot.SetActive(!isWin);
        }
    }

    private void HandleClick()
    {
        OnRetryClicked?.Invoke();
    }
}