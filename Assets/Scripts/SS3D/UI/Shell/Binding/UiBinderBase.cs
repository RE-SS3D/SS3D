using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.Shell.Binding
{
    /// <summary>
    /// Shared query/wire/disconnect plumbing for UI Toolkit panel binders. Concrete binders query
    /// elements through <see cref="Query{T}"/> and register event wiring through <see cref="Wire"/>
    /// instead of hand-writing a matched subscribe/unsubscribe pair — <see cref="Disconnect"/> unwinds
    /// everything that was wired, in reverse, so a binder can't forget to unwire a control it wired.
    /// </summary>
    public abstract class UiBinderBase
    {
        private readonly List<Action> _unwireActions = new();

        protected UiBinderBase(VisualElement root, VisualElement queryRoot = null)
        {
            Root = root;
            QueryRoot = queryRoot ?? root;
        }

        protected VisualElement Root { get; }

        protected VisualElement QueryRoot { get; }

        /// <summary>Queries <see cref="QueryRoot"/> by name, warning once (Editor/dev builds) on a miss.</summary>
        protected T Query<T>(string name)
            where T : VisualElement
        {
            T element = QueryRoot?.Q<T>(name);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (element == null)
            {
                Debug.LogWarning($"{GetType().Name} could not find a {typeof(T).Name} named \"{name}\".");
            }
#endif
            return element;
        }

        /// <summary>Subscribes immediately and queues the matching unsubscribe for <see cref="Disconnect"/>.</summary>
        protected void Wire(Action subscribe, Action unsubscribe)
        {
            subscribe();
            _unwireActions.Add(unsubscribe);
        }

        /// <summary>Runs every queued unsubscribe, in reverse wiring order, then clears them.</summary>
        public virtual void Disconnect()
        {
            for (int i = _unwireActions.Count - 1; i >= 0; i--)
            {
                _unwireActions[i]();
            }

            _unwireActions.Clear();
        }
    }

    /// <summary>Typed <see cref="UiBinderBase"/> that binds a specific view-model type.</summary>
    public abstract class UiBinderBase<TViewModel> : UiBinderBase, IUiBinder<TViewModel>
    {
        protected UiBinderBase(VisualElement root, VisualElement queryRoot = null)
            : base(root, queryRoot)
        {
        }

        public void Bind(TViewModel viewModel)
        {
            BindCore(viewModel);
        }

        protected abstract void BindCore(TViewModel viewModel);
    }
}
