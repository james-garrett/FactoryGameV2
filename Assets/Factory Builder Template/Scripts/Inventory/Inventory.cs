using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FactoryBuilderTemplate
{
    /// <summary>
    /// Inventory class used by objects that need storage. 
    /// While this could be just a normal C# class I decided to keep this as MonoBehaviour so inventory can be added to any object just by attaching this script 
    /// to the game object then getting a reference to it by using GetComponent<Inventory> in the main object class (see how container or crafting machine use it).
    ///
    /// </summary>
    public class Inventory : MonoBehaviour, ISaveable
    {
        [Header("Inventory Parameters")]
        [Tooltip("How many items slots this inventory has, final items count in the whole inventory depends on types of items stored in it because MaxInStack is different for different item types.")]
        public int SlotsAmount = 10;
        
        /// <summary>
        ///List containing all inventory slots of this inventory 
        /// </summary>
        public List<InventorySlot> Slots;

        private void Awake()
        {
            CreateSlots();
        }

        /// <summary>
        /// Initialize or clear inventory
        /// </summary>
        private void CreateSlots()
        {
            if(Slots == null)
                Slots = new List<InventorySlot>();
            else
                Slots.Clear();

            for(int i = 0; i < SlotsAmount; i++)
            {
                Slots.Add(new InventorySlot());
            }
        }

        /// <summary>
        /// Change amount of slots inventory have
        /// </summary>
        /// <param name="slots">New slots amount</param>
        public void ChangeSlotsAmount(int slots)
        {
            SlotsAmount = slots;
            CreateSlots();
        }

        /// <summary>
        /// Get the last item from the last inventory slot in this inventory
        /// </summary>
        /// <returns>Last item in the inventory</returns>
        public Item GetLast()
        {
            for(int i = 0; i < Slots.Count; i++)
            {
                InventorySlot slot = Slots[Slots.Count - 1 - i];
                if(!slot.IsEmpty())
                {
                    return slot.GetLast();
                }
            }
            return null;
        }

        /// <summary>
        /// Get last item in inventory and remove it
        /// </summary>
        /// <returns></returns>
        public Item GetLastAndRemove()
        {
            Item ret = GetLast();

            if(ret == null)
                return null;

            if(!Remove(ret))
                return null;

            return ret;
        }

        /// <summary>
        /// Add item to inventory
        /// </summary>
        /// <param name="item">Item to be added</param>
        /// <returns>True when item was successfully added to inventory, false when there is not space left.</returns>
        public bool Add(Item item)
        {
            foreach(InventorySlot slot in Slots)
            {
                if(slot.Add(item))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Remove item from inventory
        /// </summary>
        /// <param name="item">Item to be removed</param>
        /// <returns>True when item was removed, false means items is not even stored in this inventory</returns>
        public bool Remove(Item item)
        {
            for(int i = 0; i < Slots.Count; i++)
            {
                InventorySlot slot = Slots[Slots.Count - 1 - i];
                if(slot.SlotItemTypesMatches(item))
                {
                    if(slot.Remove(item))
                    {
                        return true;
                    }
                }
            }
             
            return false;
        }

        /// <summary>
        /// Get occupied slots amount
        /// </summary>
        /// <returns>Amount of slots already occupied by some items.</returns>
        public int GetOccupiedSlots()
        {
            int amount = 0;
            foreach(InventorySlot slot in Slots)
            {
                if(!slot.IsEmpty())
                    amount++;
            }
            return amount;
        }

        public string Save()
        {
            return JsonUtility.ToJson(this);
        }

        public bool Load(string data)
        {
            JsonUtility.FromJsonOverwrite(data, this);
            return true;
        }
    }
}
