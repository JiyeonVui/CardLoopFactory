using System;
using UnityEngine;
using UnityEngine.UI;

// Màn Start: chỉ 1 button. Bắn OnStartClicked cho UIManager, không tự quyết định luồng
// game (để GameContext điều phối). Kéo button vào inspector.
public class StartScreen : UIScreen
{
    [SerializeField] private Button _startButton;

    public event Action OnStartClicked;

    private void Awake()
    {
        if (_startButton != null)
        {
            _startButton.onClick.AddListener(HandleClick);
        }
    }

    private void OnDestroy()
    {
        if (_startButton != null)
        {
            _startButton.onClick.RemoveListener(HandleClick);
        }
    }

    private void HandleClick()
    {
        OnStartClicked?.Invoke();
    }
}