using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FactoryBuilderTemplate
{
    /// <summary>
    /// The wrapper class used to display information about InventorySlot bound to it.
    /// Useful to conveniently display inventories (see InventoryView.cs)
    /// </summary>
    public class ItemView : MonoBehaviour
    {
        public Image ItemIcon;
        public Text ItemName;
        public Text InStack;
        public Image BackgroundImage;

        [HideInInspector]
        public InventorySlot Slot;
        
        /// <summary>
        /// Update info showed by ItemView
        /// </summary>
        void Update()
        {
            if(Slot == null || Slot.IsEmpty())
            {
                ItemName.text = "Empty";
                InStack.text = "";
                ItemIcon.sprite = null;

                BackgroundImage.enabled = false;
                ItemIcon.enabled = false;
                ItemName.enabled = false;
                InStack.enabled = false;

                return;
            }
            else if(Slot != null)
            {
                BackgroundImage.enabled = true;
                ItemIcon.enabled = true;
                ItemName.enabled = true;
                InStack.enabled = true;

                ItemName.text = "" + Slot.GetItemDefinition().ItemName;
                InStack.text = "" + Slot.InStack();
                ItemIcon.sprite = Slot.GetItemDefinition().ItemIcon;
            }
        }
    }
}
