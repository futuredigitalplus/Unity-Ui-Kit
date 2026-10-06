using System;
using System.Collections.Generic;
using UnityEngine;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// Where screens announce themselves: a screen id mapped to a factory, so the router needs no reflection and no central list.
    /// Games with one shell register on <see cref="Default"/> from a static method:
    /// <code>
    /// [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    /// static void Register() { ScreenCatalog.Default.Register("Home", () =&gt; new HomeScreen()); }
    /// </code>
    /// Several shells (or tests) use their own instances: <c>shell.Catalog.Register(...)</c>. A shell asks its own catalog first, then <see cref="Default"/>.
    /// </summary>
    public sealed class ScreenCatalog
    {
        readonly Dictionary<string, Func<UiScreen>> _factories = new Dictionary<string, Func<UiScreen>>(StringComparer.Ordinal);

        /// <summary>The catalog that every shell also consults. Cleared when the editor reloads or play mode starts.</summary>
        public static ScreenCatalog Default { get; } = new ScreenCatalog();

        /// <summary>The registered screen ids.</summary>
        public IReadOnlyCollection<string> Ids
        {
            get { return _factories.Keys; }
        }

        /// <summary>Registers or replaces the factory of <paramref name="id"/>. The id is also the router id and, by convention, the UXML name.</summary>
        public void Register(string id, Func<UiScreen> factory)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("A screen id must not be empty.", nameof(id));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            _factories[id] = factory;
        }

        /// <summary>
        /// Registers a screen that is only a <see cref="ScreenDefinition"/>: with <paramref name="factory"/> null the screen is a plain page that shows the
        /// definition's UXML and has no behaviour (an About or Credits page). With a factory, the screen should return the definition from <see cref="UiScreen.Definition"/>.
        /// </summary>
        public void Register(ScreenDefinition definition, Func<UiScreen> factory = null)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            Register(definition.Id, factory ?? (() => new DefinitionScreen(definition)));
        }

        /// <summary>Finds the factory of <paramref name="id"/>.</summary>
        public bool TryGet(string id, out Func<UiScreen> factory)
        {
            return _factories.TryGetValue(id ?? string.Empty, out factory);
        }

        /// <summary>Removes every registration.</summary>
        public void Clear()
        {
            _factories.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Default.Clear();
        }

        sealed class DefinitionScreen : UiScreen
        {
            readonly ScreenDefinition _definition;

            public DefinitionScreen(ScreenDefinition definition)
            {
                _definition = definition;
            }

            public override string UxmlName
            {
                get { return _definition.Id; }
            }

            public override ScreenDefinition Definition
            {
                get { return _definition; }
            }
        }
    }
}
