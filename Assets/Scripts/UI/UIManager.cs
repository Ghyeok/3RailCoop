using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private GameObject popupRoot; 

    private Stack<UI_Popup> popupStack;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Debug.LogError("UIManager가 이미 존재합니다.");
            Destroy(this);
            return;
        }

        Instance = this;
        popupStack = new Stack<UI_Popup>();
    }

    public UI_Popup OpenPopup(UI_Popup popupPrefab)
    {
        if (popupPrefab == null)
        {
            Debug.LogWarning("열 프리팹이 없습니다.");
            return null;
        }

        if (popupRoot == null)
        {
            Debug.LogWarning("팝업 루트가 할당되지 않았습니다.");
            return null;
        }

        UI_Popup popup = Instantiate(popupPrefab, popupRoot.transform, false);
        popup.transform.SetAsLastSibling();
        popupStack.Push(popup);

        return popup;
    }

    public void ClosePopup()
    {
        if (popupStack.Count <= 0) return;

        UI_Popup popup = popupStack.Pop();
        if (popup != null)
        {
            Destroy(popup.gameObject);
        }
    }

    public void CloseAllPopups()
    {
        while(popupStack.Count > 0)
        {
            ClosePopup();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
