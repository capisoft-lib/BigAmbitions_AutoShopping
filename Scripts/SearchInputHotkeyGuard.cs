using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AutoShopping
{
    /// <summary>
    /// Keeps the search field registered as the EventSystem selection while focused so
    /// GameManager.HasInputSelected blocks vanilla letter/space hotkeys.
    /// </summary>
    internal sealed class SearchInputHotkeyGuard : MonoBehaviour
    {
        private TMP_InputField _field;

        internal void Bind(TMP_InputField field) => _field = field;

        private void LateUpdate()
        {
            if (_field == null || !_field.isFocused)
                return;

            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return;

            if (eventSystem.currentSelectedGameObject != _field.gameObject)
                eventSystem.SetSelectedGameObject(_field.gameObject);
        }
    }
}
