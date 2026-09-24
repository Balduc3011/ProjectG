using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LanguageBtn : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI languageTxt;
    [SerializeField] private Button button;

    public LanguageType Language { get; private set; }
    public Button Button => button;

    public void Setup(LanguageType language, string languageName)
    {
        if (button == null) button = GetComponent<Button>();
        Language = language;
        languageTxt.text = languageName;
    }

    public void SetSelected(LanguageType current)
    {
        button.interactable = Language != current;
    }
}
