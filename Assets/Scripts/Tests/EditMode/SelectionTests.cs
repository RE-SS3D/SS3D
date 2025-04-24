using NUnit.Framework;
using SS3D.Systems.Selection;
using SS3D.Systems.Tile;
using SS3D.Tests;
using System.Collections;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace EditorTests
{

    public class SelectionTests: EditModeTest
    {
        const string ParentName = "Parent Game Object";
        const string ChildName = "Child Game Object";

        #region Tests
        /// <summary>
        /// This test confirms that selectables are allocated different
        /// colours from previously allocated.
        /// </summary>
        [Test]
        public void RegisterSelectableAllocatesDistinctColors()
        {
            // Create Selection system, and basic hierarchy (parent/child gameobjects) to test.
            SelectionSubSystem _system = new SelectionSubSystem();
            Selectable _parentSelectable;
            Selectable _childSelectable;
            CreateSelectableHierarchy(_system, out _parentSelectable, out _childSelectable);

            // Confirm what colours they have been allocated.
            Color32 parentColor = _parentSelectable.SelectionColor;
            Color32 childColor = _childSelectable.SelectionColor;

            // The colours should not be the same.
            Assert.IsFalse(
                parentColor.r == childColor.r &&
                parentColor.g == childColor.g &&
                parentColor.b == childColor.b,
                $"Parent color {parentColor.ToString()} should not match Child color {childColor.ToString()}");
        }

        /// <summary>
        /// This test confirms that selectables are correctly returned by the Selection System
        /// when they have the component that is being queried. I.e. If the Selectable's selection
        /// colour matches the colour returned by the camera, and the Selectable has component X,
        /// then GetCurrentSelectable<X>() should return that Selectable.
        /// </summary>
        [Test]
        public void GetCurrentSelectableReturnsImmediateObjectWhenItHasCorrectComponent()
        {
            // Create Selection system, and basic hierarchy (parent/child gameobjects) to test.
            SelectionSubSystem _system = new SelectionSubSystem();
            Selectable _parentSelectable;
            Selectable _childSelectable;
            CreateSelectableHierarchy(_system, out _parentSelectable, out _childSelectable);

            // Confirm what colours they have been allocated.
            Color32 parentColor = _parentSelectable.SelectionColor;
            Color32 childColor = _childSelectable.SelectionColor;

            // Both parent and child game objects have a Type A selectable. They should be returned
            // if their specific colour is provided by the camera.
            SelectableTypeA returned;
            
            // Check the parent object.
            _system.UpdateColourFromCamera(parentColor);
            returned = _system.GetCurrentSelectable<SelectableTypeA>();
            Assert.IsTrue(returned?.gameObject.name == ParentName);

            // Check the child object
            _system.UpdateColourFromCamera(childColor);
            returned = _system.GetCurrentSelectable<SelectableTypeA>();
            Assert.IsTrue(returned?.gameObject.name == ChildName);
        }

        /// <summary>
        /// This test confirms that the appropriate ancestor Selectable is returned by the Selection 
        /// System, when the original Selectable does not have the component that is being queried.
        /// I.e. If the Selectable's selection colour matches the colour returned by the camera, but
        /// the Selectable does not have component X, then GetCurrentSelectable<X>() should return 
        /// the nearest ancestor of that Selectable that does have component X.
        /// </summary>
        [Test]
        public void GetCurrentSelectableReturnsAncestorWhenOnlyAncestorHasCorrectComponent()
        {
            // Create Selection system, and basic hierarchy (parent/child gameobjects) to test.
            SelectionSubSystem _system = new SelectionSubSystem();
            Selectable _parentSelectable;
            Selectable _childSelectable;
            CreateSelectableHierarchy(_system, out _parentSelectable, out _childSelectable);

            // Confirm what colours they have been allocated.
            Color32 parentColor = _parentSelectable.SelectionColor;
            Color32 childColor = _childSelectable.SelectionColor;

            // Only the parent game object has a Type B selectable. The parent should be returned
            // even though it is the child game object's selection colour being passed from the camera.
            _system.UpdateColourFromCamera(childColor);
            SelectableTypeB returned = _system.GetCurrentSelectable<SelectableTypeB>();
            Assert.IsTrue(returned?.gameObject.name == ParentName);
        }

        /// <summary>
        /// This test confirms that when neither the target Selectable nor any ancestors have the
        /// particular component being queried, the Selection System returns null.
        /// </summary>
        [Test]
        public void GetCurrentSelectableReturnsNullWhenNoAncestorsHaveCorrectComponent()
        {
            // Create Selection system, and basic hierarchy (parent/child gameobjects) to test.
            SelectionSubSystem _system = new SelectionSubSystem();
            Selectable _parentSelectable;
            Selectable _childSelectable;
            CreateSelectableHierarchy(_system, out _parentSelectable, out _childSelectable);

            // Confirm what colours they have been allocated.
            Color32 parentColor = _parentSelectable.SelectionColor;
            Color32 childColor = _childSelectable.SelectionColor;

            // Neither the parent game object nor child game object has a Type C selectable.
            _system.UpdateColourFromCamera(childColor);
            SelectableTypeC returned = _system.GetCurrentSelectable<SelectableTypeC>();
            Assert.IsNull(returned);
        }
        #endregion

        #region Helper functions
        /// <summary>
        /// Creates a basic hierarchy to test against, including 'mock' selectable types:
        /// 
        /// > "Parent Game Object" (contains Type A and Type B Selectables)
        ///   > "Child Game Object" (contains Type A Selectable only)
        /// 
        /// Neither the Parent or the Child Game Objects have a Type C Selectable.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="child"></param>
        private void CreateSelectableHierarchy(SelectionSubSystem system, out Selectable parent, out Selectable child)
        {
            GameObject _parentGo;
            GameObject _childGo;

            CreateGameObject<Selectable>(out _parentGo, out parent);
            CreateGameObject<Selectable>(out _childGo, out child);
            _childGo.transform.SetParent(_parentGo.transform);

            // Set GameObject names so we can identify which is being returned.
            _parentGo.name = ParentName;
            _childGo.name = ChildName;

            // Add the relevant test doubles to the GameObjects.
            _parentGo.AddComponent<SelectableTypeA>();
            _parentGo.AddComponent<SelectableTypeB>();
            _childGo.AddComponent<SelectableTypeA>();

            // Manually register Selectables with the Selection System.
            // (This would normally occur in OnStart method to the SelectionSystemController)
            parent.SelectionColor = system.RegisterSelectable(parent);
            child.SelectionColor = system.RegisterSelectable(child);
        }
        #endregion

        #region Test Double Components
        // These classes are simply test doubles to represent other systems
        // that use the Selection system. We could have used real classes
        // or interfaces such as IExaminable, IInteractionTarget etc, but that
        // would have made the logic less clear.
        private class SelectableTypeA : MonoBehaviour { }
        private class SelectableTypeB : MonoBehaviour { }
        private class SelectableTypeC : MonoBehaviour { }
        #endregion


    }


}
