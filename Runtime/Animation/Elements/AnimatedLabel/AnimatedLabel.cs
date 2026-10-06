// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
// ----------------------------------------------------------------------------
// -- Project : https://github.com/instance-id/ElementAnimationToolkit       --
// -- instance.id 2020 | http://github.com/instance-id | http://instance.id  --
// ----------------------------------------------------------------------------

// FutureDigitalPlus: upstream wrapped this whole file in '#if UNITY_EDITOR' (editor-only toolkit). Guard removed so the code also works at runtime / in player builds.
using System;
using UnityEngine;
using UnityEngine.UIElements;
using FutureDigitalPlus.UIKit.Animation.Extensions;

// ReSharper disable UnusedMember.Global
#pragma warning disable 108,114

namespace FutureDigitalPlus.UIKit.Animation
{
    /// <summary>
    /// Provides an animated Element displaying text.
    /// </summary>
    // FutureDigitalPlus: now 'partial' + [UxmlElement] (legacy UxmlFactory/UxmlTraits are obsolete in Unity 6).
    [UxmlElement]
    public sealed partial class AnimatedLabel : VisualElement
    {
        public Color startColor;
        public Color animatedColor;
        private Label animatedLabel;
        StyleSheet k_StylePath;
        private Type editorType;

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string ussClassName = "animated-label-container";

        public static readonly string ussClassNameExtended = "animated-label";

        /// <summary>
        /// Constructs a label.
        /// </summary>
        private string m_text;

        public string text
        {
            get => m_text;
            set
            {
                m_text = value;
                animatedLabel.text = m_text;
            }
        }

        /// <summary>
        /// Constructs a label.
        /// </summary>
        /// <param name="text">The text to be displayed.</param>
        public AnimatedLabel()
        {
            editorType = GetType();
            styleSheets.Add(StylesheetSetup());
            new Label { text = "" }.Create(out animatedLabel).ToUSS(nameof(animatedLabel));
            animatedLabel.AddToClassList(ussClassNameExtended);
            Add(animatedLabel);

            AddToClassList(ussClassName);
            AddToClassList(this.name);

            this.text = text;
        }

        internal Label GetLabel()
        {
            return animatedLabel;
        }

        // public void AddToClassList()
        // {
        //
        // }

        public void Play() { }

        private StyleSheet StylesheetSetup()
        {
            // FutureDigitalPlus: upstream used AssetDatabase (editor only, via GetStyleSheet). The stylesheet is now loaded from a Resources
            // folder: Elements/AnimatedLabel/Resources/FutureDigitalPlus/UIKit/Animation/Styles/AnimatedLabel.uss -> Resources path "FutureDigitalPlus/UIKit/Animation/Styles/AnimatedLabel".
            if (k_StylePath == null)
                k_StylePath = Resources.Load<StyleSheet>("FutureDigitalPlus/UIKit/Animation/Styles/AnimatedLabel");
            if (k_StylePath is null) Debug.LogError($"{editorType.Name} Stylesheet not found");
            return k_StylePath;
        }

        public void ClearClassList()
        {
            animatedLabel.ClearClassList();
        }

        public void AddToClassList(string className)
        {
            animatedLabel.AddToClassList(className);
        }

        public void RemoveFromClassList(string className)
        {
            animatedLabel.RemoveFromClassList(className);
        }

        /// <summary>
        ///   <para>Toggles between adding and removing the given class name from the class list.</para>
        /// </summary>
        /// <param name="className">The class name to add or remove from the class list.</param>
        public void ToggleInClassList(string className)
        {
            animatedLabel.ToggleInClassList(className);
        }

        /// <summary>
        ///   <para>Enables or disables the class with the given name.</para>
        /// </summary>
        /// <param name="className">The name of the class to enable or disable.</param>
        /// <param name="enable">A boolean flag that adds or removes the class name from the class list. If true, EnableInClassList adds the class name to the class list. If false, EnableInClassList removes the class name from the class list.</param>
        public void EnableInClassList(string className, bool enable)
        {
            animatedLabel.EnableInClassList(className, enable);
        }

        public bool ClassListContains(string cls)
        {
            return animatedLabel.ClassListContains(cls);
        }

        // -- Register callbacks ------------------------------------------
        /// <summary>
        /// Register a callback on the animated label
        /// </summary>
        /// <param name="action"></param>
        /// <typeparam name="TEventType"></typeparam>
        public void RegisterCallback<TEventType>(EventCallback<TEventType> action) where TEventType : EventBase<TEventType>, new()
        {
            animatedLabel.RegisterCallback<TEventType>(action);
        }

        public void RegisterCallback<TEventType, TUserArgsType>(
        EventCallback<TEventType, TUserArgsType> callback,
        TUserArgsType userArgs,
        TrickleDown useTrickleDown = TrickleDown.NoTrickleDown)
            where TEventType : EventBase<TEventType>, new()
        {
            animatedLabel.RegisterCallback<TEventType, TUserArgsType>(callback, userArgs, useTrickleDown);
        }

        public void UnregisterCallback<TEventType>(
        EventCallback<TEventType> callback,
        TrickleDown useTrickleDown = TrickleDown.NoTrickleDown)
            where TEventType : EventBase<TEventType>, new()
        {
            animatedLabel.UnregisterCallback<TEventType>(callback, useTrickleDown);
        }

        public void UnregisterCallback<TEventType, TUserArgsType>(
        EventCallback<TEventType, TUserArgsType> callback,
        TrickleDown useTrickleDown = TrickleDown.NoTrickleDown)
            where TEventType : EventBase<TEventType>, new()
        {
            animatedLabel.UnregisterCallback<TEventType, TUserArgsType>(callback, useTrickleDown);
        }

        public static bool RegisterValueChangedCallback<T>(
        INotifyValueChanged<T> control,
        EventCallback<ChangeEvent<T>> callback)
        {
            if (!(control is CallbackEventHandler callbackEventHandler))
                return false;
            callbackEventHandler.RegisterCallback<ChangeEvent<T>>(callback);
            return true;
        }

        public static bool UnregisterValueChangedCallback<T>(
        INotifyValueChanged<T> control,
        EventCallback<ChangeEvent<T>> callback)
        {
            if (!(control is CallbackEventHandler callbackEventHandler))
                return false;
            callbackEventHandler.UnregisterCallback<ChangeEvent<T>>(callback);
            return true;
        }

        // FutureDigitalPlus: removed the obsolete UxmlFactory / UxmlTraits classes (UXML support is the [UxmlElement] attribute above).
    }
}
