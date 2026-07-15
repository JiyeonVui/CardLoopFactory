using UnityEngine;

// Base cho mọi màn UI phủ (overlay). Giữ tối giản: chỉ bật/tắt GameObject. Các screen
// con thêm hành vi riêng (button, anim). UIManager điều khiển vòng đời qua Show/Hide.
public abstract class UIScreen : MonoBehaviour
{
    public virtual void Show()
    {
        gameObject.SetActive(true);
    }

    public virtual void Hide()
    {
        gameObject.SetActive(false);
    }
}