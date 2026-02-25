using System;
using Balduu;
using Baseduu.ReactiveFields;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class UIResourceBar : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI valueTxt;
    public Button getMoreBtn;
    public GameObject addMoreGO;
    public ReactiveValue<float> currentValue;
    private Action getMoreAction;

    private void Start()
    {
        getMoreBtn.AddListenerUI(OnGetMoreBtn, false);
    }

    public void Init(ReactiveValue<float> value, Action getMoreAction)
    {
        currentValue = value;
        this.getMoreAction = getMoreAction;
        UpdateValue(currentValue.Value);
        value.Property.Subscribe(UpdateValue).AddTo(gameObject.GetCancellationTokenOnDestroy());
    }

    void UpdateValue(float value)
    {
        valueTxt.ChangeText(value.ToStringUI(2));
    }
    
    private void OnGetMoreBtn()
    {
        getMoreAction?.Invoke();
    }
}
