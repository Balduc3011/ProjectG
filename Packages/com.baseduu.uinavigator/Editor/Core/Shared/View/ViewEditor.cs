using UnityEditor;
using Baseduu.UINavigator.Core.Views;

namespace Baseduu.UINavigator.Editor.Core.Shared.Views
{
    [CustomEditor(typeof(View), editorForChildClasses: true)]
    public class ViewEditor
#if ODIN_INSPECTOR
        : Sirenix.OdinInspector.Editor.OdinEditor
#else
        : UnityEditor.Editor
#endif
    {
        public override void OnInspectorGUI()
        {
            if (target is View view && view.DontAddCanvasGroupAutomatically == false)
            {
                var _ = view.CanvasGroup;
            }

            base.OnInspectorGUI();
        }
    }
}
