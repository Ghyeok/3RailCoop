using UnityEngine;

public class UI_Popup : UI_Base
{
    public void Close()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ClosePopup(this);
        }
    }
}
