using UnityEngine;
using UnityEngine.UI;

namespace UI.Views
{
    public class PopUpWindowView : View
    {
        [SerializeField] private Button backButton;
        
        /// <inheritdoc/>
        public override void Initialize()
        {
            backButton.onClick.AddListener(MainManager.Instance.ViewManager.ShowLast);
        }
    }
}
