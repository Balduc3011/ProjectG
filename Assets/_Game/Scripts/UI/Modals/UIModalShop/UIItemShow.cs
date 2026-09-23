using DataSave;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIItemShow : MonoBehaviour
{
    // Khai báo slot này hiển thị resource nào, để nơi dùng map theo type/defineValue thay vì theo thứ tự.
    public ResourceType resourceType;
    public int defineValue;

    public Image icon;
    public TextMeshProUGUI valueTxt;

    public bool Matches(GameResource resource)
    {
        return resource.Type == resourceType && resource.DefineValue == defineValue;
    }

    public void Init(Sprite sprite, string value)
    {
        icon.sprite = sprite;
        valueTxt.text = value;
    }
}
