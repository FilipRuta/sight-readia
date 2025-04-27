using UnityEngine;

namespace UI
{
    public abstract class View : MonoBehaviour
    {
        /// <summary>
        /// Initialize view. Called once when loaded.
        /// </summary>
        public abstract void Initialize();

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnEscapePressed();
            }
        }

        /// <summary>
        /// Show last page on escape pressed.
        /// </summary>
        protected virtual void OnEscapePressed()
        {
            MainManager.Instance.ViewManager.ShowLast();
        }

        /// <summary>
        /// Hides the view.
        /// </summary>
        public virtual void Hide() => gameObject.SetActive(false);

        /// <summary>
        /// Shows the view. Prepares and updates all the visual elements.
        /// </summary>
        public virtual void Show() => gameObject.SetActive(true);
    }
}