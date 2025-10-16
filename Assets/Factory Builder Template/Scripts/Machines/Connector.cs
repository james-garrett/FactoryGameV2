using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FactoryBuilderTemplate
{
    /// <summary>
    /// Machine used to connect couple conveyor belts into one.
    /// </summary>
    public class Connector : Machine, ISaveable
    {
        //[Header("Machine IO")]
        public MachineInput input1;
        public MachineInput input2;
        public MachineInput input3;
        public MachineOutput output;

        private Item item;
        private List<int> waitingRequests; //int = id of input
         
        void Start()
        {
            var newInputs = IOHub.CreateManyInput(3, this);
            input1 = newInputs[0];
            input2 = newInputs[1];
            input3 = newInputs[2];
            //create inputs and outputs lists

            //register machine IO
            InputOutputHub.Inputs.Add(input1);
            InputOutputHub.Inputs.Add(input2);
            InputOutputHub.Inputs.Add(input3);
            InputOutputHub.Outputs.Add(output);

            //list used to push items to output from all 3 inputs evenly, to prevent connector pushing items to output only from first input when all input belts are clogged
            waitingRequests = new List<int>();
            
            //set machine initialized flag
            IsInitializated = true;
        }

        public override bool ReceiveItem(Item item, MachineInput input)
        {
            int currentInput = InputOutputHub.Inputs.IndexOf(input);
              
            //receive item only if last received item is already send to output
            bool canTakeItem = (this.item == null) && (waitingRequests.Count == 0 || waitingRequests[0] == currentInput);
            if(canTakeItem)
            {
                this.item = item;
                waitingRequests.Remove(currentInput);
                return true;
            }

            if(!waitingRequests.Contains(currentInput))
                waitingRequests.Add(currentInput);

            return false; 
        }

        public override void MachineUpdate()
        {
            if(item != null)
            {
                //try to send stored item into output if possible
                if(output.TryToSendItem(item))
                {
                    item = null;
                }
            }
        }
        
        public string Save()
        {
            ConnectorData data = new ConnectorData();

            if(InputOutputHub.Outputs[0].ConnectedTo != null)
            {
                data.OutputConnectedToMachineInputID = InputOutputHub.Outputs[0].ConnectedTo.Parent.GetMachineID();
                data.OutputConnectedToInputGameObjectName = InputOutputHub.Outputs[0].ConnectedTo.name;
            }

            return JsonUtility.ToJson(data);
        }

        public bool Load(string data)
        {
            ConnectorData connectorData = JsonUtility.FromJson<ConnectorData>(data);

            //connect machine output 
            output.ConnectedTo = TryToFindMachineInput(connectorData.OutputConnectedToMachineInputID, connectorData.OutputConnectedToInputGameObjectName);

            return true;
        }

        /// <summary>
        /// Class used to store this machine data used for serialization/deserialization
        /// </summary>  
        [System.Serializable]
        private class ConnectorData
        {
            public int OutputConnectedToMachineInputID;
            public string OutputConnectedToInputGameObjectName;
        }
    }
}
