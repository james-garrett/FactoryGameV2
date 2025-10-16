using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FactoryBuilderTemplate
{
    /// <summary>
    /// Inventory slot from which inventory consists of. 
    /// Used to store maximum full stack of one item type.
    /// </summary>
    [System.Serializable]
    public class InventorySlot
    {
        /// <summary>
        /// Stored items list
        /// </summary>
        public List<Item> StoredItems;

        /// <summary>
        /// Checks if can add an item to this slot.
        /// </summary>
        /// <param name="item">Item to add.</param>
        /// <returns>True when item can be added to this stack false otherwise.</returns>
        public bool CanAdd(Item item)
        {
            //if true then this slot is empty
            if(IsEmpty())
            {
                return true;
            } 
            else
            {
                ItemDefinition slotItemDef = StoredItems[0].ItemDefinition;

                if(!CompareItemDefinitions(slotItemDef, item.ItemDefinition))
                    return false;

                if(slotItemDef.Stackable)
                {
                    //if item in this is slot is stackable check if we can add new item to stack
                    if(StoredItems.Count < slotItemDef.MaxInStack)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if one item of a given item type can be added to slot.
        /// </summary>
        /// <param name="itemDef">item type</param>
        /// <returns>True when the item was successfully added to slot false otherwise.</returns>
        public bool CanAdd(ItemDefinition itemDef)
        {
            //if true then this slot is empty
            if(IsEmpty())
            {
                return true;
            } else
            {
                ItemDefinition slotItemDef = StoredItems[0].ItemDefinition;

                if(!CompareItemDefinitions(slotItemDef, itemDef))
                    return false;

                if(slotItemDef.Stackable)
                {
                    //if item in this is slot is stackable check if we can add new item to stack
                    if(StoredItems.Count < slotItemDef.MaxInStack)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Add item to a slot.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>True when the item was successfully added to slot false otherwise.</returns>
        public bool Add(Item item)
        {
            if(CanAdd(item))
            {
                if(StoredItems == null)
                {
                    StoredItems = new List<Item>();
                }

                StoredItems.Add(item);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check if a given amount of items can be added to slot
        /// </summary>
        /// <param name="item">item to add</param>
        /// <param name="amount">amount of items to add</param>
        /// <returns></returns>
        public bool HaveSpaceForItems(Item item, int amount)
        {
            return HaveSpaceForItems(item.ItemDefinition, amount);
        }

        /// <summary>
        /// Check if a given amount of items can be added to slot
        /// </summary>
        /// <param name="itemDef">item to add</param>
        /// <param name="amount">amount of items to add</param>
        /// <returns></returns>
        public bool HaveSpaceForItems(ItemDefinition itemDef, int amount)
        {
            if(itemDef == null || amount == 0)
                return false;

            if(IsEmpty())
                return true;

            if(amount == 1)
                return CanAdd(itemDef);

            if(CompareItemDefinitions(GetItemDefinition(), itemDef))
            {
                if(itemDef.Stackable && itemDef.MaxInStack >= InStack() + amount)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Remove item from the slot
        /// </summary>
        /// <param name="item">item to remove</param>
        /// <returns>True when item was removed from slot false can mean item is not even stored here.</returns>
        public bool Remove(Item item)
        {
            if(!IsEmpty() && SlotItemTypesMatches(item))
            {
                StoredItems.RemoveAt(StoredItems.Count - 1);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Clears slot
        /// </summary>
        public void Clear()
        {
            if(StoredItems != null)
                StoredItems.Clear();
        }

        /// <summary>
        /// Returns the last item from the slot, but does not remove it from the stack 
        /// </summary>
        /// <returns>Last item from this slot</returns>
        public Item GetLast()
        {
            if(!IsEmpty())
            {
                return StoredItems[StoredItems.Count - 1];
            }
            return null;
        }

        /// <summary>
        /// Get last item from slot and remove it
        /// </summary>
        /// <returns>Last item in this slot</returns>
        public Item GetLastAndRemove()
        {
            Item ret = GetLast();
            Remove(ret);

            return ret;
        }

        /// <summary>
        /// Check if the type of item matches with what type of items is stored in this slot. 
        /// </summary>
        /// <param name="item">item to check</param>
        /// <returns>True if slot contains items of same type false otherwise</returns>
        public bool SlotItemTypesMatches(Item item)
        {
            ItemDefinition def = GetItemDefinition();
            if(def == null || item == null)
                return false;

            return CompareItemDefinitions(item.ItemDefinition, def);
        }

        /// <summary>
        /// Compare two item types if they match
        /// </summary>
        /// <param name="one">first item type</param>
        /// <param name="two">second item type</param>
        /// <returns>True when types match</returns>
        private bool CompareItemDefinitions(ItemDefinition one, ItemDefinition two)
        {
            return one.ItemName == two.ItemName && one.Stackable == two.Stackable && one.MaxInStack == two.MaxInStack;
        }

        /// <summary>
        /// How many items are in this slot
        /// </summary>
        /// <returns>Amount of items in this slot</returns>
        public int InStack()
        {
            if(StoredItems == null)
                return 0;

            return StoredItems.Count;
        }

        /// <summary>
        /// Get item type this slot stores.
        /// </summary>
        /// <returns>Type of item this slot stores.</returns>
        public ItemDefinition GetItemDefinition()
        {
            return IsEmpty() ? null : StoredItems[0].ItemDefinition;
        }

        /// <summary>
        /// Check if this slot is empty
        /// </summary>
        /// <returns>True when slot is empty</returns>
        public bool IsEmpty()
        {
            return StoredItems == null || StoredItems.Count == 0;
        }

        /// <summary>
        /// Check if slot is full
        /// </summary>
        /// <returns>True when slot contains full stack of items</returns>
        public bool IsFull()
        {
            if(IsEmpty())
                return false;

            int maxInStack = GetItemDefinition().Stackable ? GetItemDefinition().MaxInStack : 1;
            return InStack() >= maxInStack;
        }
    }
}
