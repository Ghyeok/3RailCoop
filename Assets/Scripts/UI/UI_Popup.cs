using UnityEngine;

public class UI_Popup : UIBase
{
    public void Close()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ClosePopup(this);
        }
    }
}
