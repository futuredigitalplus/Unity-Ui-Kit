using FutureDigitalPlus.UIKit.DesignSystem.Theme.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit
{
    /// <summary>
    /// Drop this next to a UIDocument and it calls <see cref="UIKitInstaller.Install"/> on the document's root
    /// whenever the document (re)builds its tree. Change <see cref="Theme"/> at runtime to re-theme the screen.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("Future Digital Plus UI Kit/UI Kit Host")]
    public class UIKitHost : MonoBehaviour
    {
        [SerializeField] private UIKitOptions options = new UIKitOptions();

        private UIDocument _document;
        private UIKitInstallation _installation;
        private VisualElement _installedRoot;

        /// <summary>What the host installs; change it before the first install (an inactive object) or call <see cref="UIKitInstallation.Refresh"/> afterwards.</summary>
        public UIKitOptions Options => options;

        /// <summary>The live installation on the document root, or null while the document has no tree.</summary>
        public UIKitInstallation Installation => _installation;

        /// <summary>The theme being worn; assign to swap (null = base tokens).</summary>
        public ThemeData Theme
        {
            get => options.Theme;
            set
            {
                options.Theme = value;
                _installation?.SetTheme(value);
            }
        }

        private void OnEnable()
        {
            _document = GetComponent<UIDocument>();
            TryInstall();
        }

        // UIDocument builds its tree in its own OnEnable; component order is not ours to pick, and a
        // document can rebuild its tree later (new root), so also re-check every frame until installed.
        private void Update()
        {
            if (_document == null) return;
            var root = _document.rootVisualElement;
            if (root != null && root != _installedRoot) TryInstall();
        }

        private void TryInstall()
        {
            var root = _document != null ? _document.rootVisualElement : null;
            if (root == null) return;

            _installation?.Dispose();
            _installation = UIKitInstaller.Install(root, options);
            _installedRoot = root;
        }

        private void OnDisable()
        {
            _installation?.Dispose();
            _installation = null;
            _installedRoot = null;
        }
    }
}
