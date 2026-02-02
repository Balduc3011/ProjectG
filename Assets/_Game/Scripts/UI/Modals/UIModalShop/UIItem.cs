using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIItem : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI valueTxt;

    public void Init(Sprite sprite, string value)
    {
        icon.sprite = sprite;
        valueTxt.text = value;
    }
}
