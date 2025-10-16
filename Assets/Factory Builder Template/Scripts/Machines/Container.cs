using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FactoryBuilderTemplate
{
    /// <summary>
    /// Machine used to store items in attached to its inventory
    /// </summary>
    [RequireComponent(typeof(Inventory))]
    public class Container : Machine, ISaveable
    {
        [Header("Machine IO")]
        public MachineInput input;
        public MachineOutput output;

        [Header("Text to show occupied slots amount")]
        public Text text;

        [Header("Debug")]
        [Tooltip("Useful to create storage full of given item to test if for example crafting machine works properly, set it to null if not used")]
        public ItemDefinition fillWith;

        private Inventory inventory;
         
        void Start()
        {
            //register machine IO
            //InputOutputHub.Inputs() = new List<MachineInput>();
            //InputOutputHub.Outputs() = new List<MachineOutput>();
            //InputOutputHub.Inputs().Add(input);
            //InputOutputHub.Outputs().Add(output);

            //grab inventory reference
            inventory = GetComponent<Inventory>();

            //fill storage with debug item if its not null
            if(fillWith)
            {
                for(int i = 0; i < inventory.SlotsAmount; i++)
                    for(int j = 0; j < fillWith.MaxInStack; j++)
                        inventory.Add(new Item(fillWith));
            }

            //mark machine initialized flag
            IsInitializated = true;
        }

        public override bool ReceiveItem(Item item, MachineInput input)
        {
            return inventory.Add(item);
        }

        public void Update()
        {
            if(text && inventory)
                text.text = "" + inventory.GetOccupiedSlots() + "/" + inventory.SlotsAmount;
        }

        public override void MachineUpdate()
        {
            //try to push last item in inventory to output if possible
            Item last = inventory.GetLast();
            if(last != null)
            {
                if(output.TryToSendItem(last))
                {
                    inventory.Remove(last);
                }
            }
        }

        public string Save()
        {
            ContainerData data = new ContainerData();
            data.InventoryData = inventory.Save();

            if(InputOutputHub.Outputs()[0].ConnectedTo != null)
            {
                data.OutputConnectedToMachineInputID = InputOutputHub.Outputs()[0].ConnectedTo.Parent.GetMachineID();
                data.OutputConnectedToInputGameObjectName = InputOutputHub.Outputs()[0].ConnectedTo.name;
            }

            return JsonUtility.ToJson(data);
        }

        public bool Load(string data)
        {
            ContainerData containerData = JsonUtility.FromJson<ContainerData>(data);
            inventory.Load(containerData.InventoryData);

            //connect machine output
            output.ConnectedTo = TryToFindMachineInput(containerData.OutputConnectedToMachineInputID, containerData.OutputConnectedToInputGameObjectName);

            return true;
        }

        /// <summary>
        /// Class used to store this machine data used for serialization/deserialization
        /// </summary> 
        [System.Serializable]
        private class ContainerData
        {
            public int OutputConnectedToMachineInputID;
            public string OutputConnectedToInputGameObjectName;

            public string InventoryData;
        }
    }
}
