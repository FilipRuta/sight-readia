using System.Collections.Generic;
using System.Linq;
using TMPro;
using UI.Views;
using UnityEngine;

namespace UI
{
    public class ViewManager : MonoBehaviour
    {
        // Code from UI management tutorial: https://www.youtube.com/watch?v=rdXC2om16lo

        [SerializeField] private View startingView;
        [SerializeField] private View popUpView;

        private readonly List<View> _views = new();
        private View _currentView;
        private readonly Stack<View> _history = new();

        private void Start()
        {
            FindAllViews();
            foreach (var view in _views)
            {
                view.Initialize();
                view.Hide();
            }

            if (startingView != null)
            {
                Show(startingView, true);
            }
        }

        public T GetView<T>() where T : View
        {
            foreach (var view in _views)
            {
                if (view is T tView)
                {
                    return tView;
                }
            }

            return null;
        }

        public void Show<T>(bool remember = true, bool clearHistory = false) where T : View
        {
            foreach (var view in _views.OfType<T>())
            {
                Show(view, remember, clearHistory);
            }
        }

        private void Show(View view, bool remember = true, bool clearHistory = false)
        {
            if (remember && _currentView != null)
            {
                _history.Push(_currentView);
            }

            _currentView?.Hide();
            _currentView = view;
            view.Show();
            
            if (clearHistory)
                ClearHistory();
        }

        public void ShowPopUp(string text)
        {
            if (popUpView == null)
                return;
            
            var textElement = popUpView.GetComponentInChildren<TextMeshProUGUI>();
            textElement.text = text;
            Show<PopUpWindowView>();
        }

        public void ShowLast()
        {
            if (_history.Count != 0)
            {
                Show(_history.Pop(), false);
            }
        }

        private void ClearHistory()
        {
            _history.Clear();
        }

        private void FindAllViews()
        {
            _views.Clear();

            // Find ALL objects, even inactive ones
            var views = Resources.FindObjectsOfTypeAll<View>();

            foreach (var view in views)
            {
                if (view.gameObject.layer == LayerMask.NameToLayer("UI"))
                {
                    _views.Add(view);
                }
            }

            Debug.Log($"Found {_views.Count} UI Views (including inactive).");
        }
    }
}