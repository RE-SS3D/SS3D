using GameKit.Utilities.Types;
using GameKit.Utilities.Types.CanvasContainers;
using System.Collections.Generic;


namespace GameKit.Utilities.Types.OptionMenuButtons
{

    public class FloatingOptions : CanvasGroupFader
    {
        #region Protected.
        /// <summary>
        /// Current buttons.
        /// </summary>
        protected List<ButtonData> Buttons = new List<ButtonData>();
        #endregion

        /// <summary>
        /// Adds buttons.
        /// </summary>
        /// <param name="clearExisting">True to clear existing buttons first.</param>
        /// <param name="buttonDatas">Buttons to add.</param>
        protected virtual void AddButtons(bool clearExisting, IEnumerable<ButtonData> buttonDatas)
        {
            if (clearExisting)
                RemoveButtons();
            foreach (ButtonData item in buttonDatas)
                Buttons.Add(item);
        }

        /// <summary>
        /// Removes all buttons.
        /// </summary>
        protected virtual void RemoveButtons()
        {
            foreach (ButtonData item in Buttons)
                GameKit.Utilities.ResettableObjectCaches<ButtonData>.Store(item);
            Buttons.Clear();
        }


    }


}