using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FactoryBuilderTemplate
{
    /// <summary>
    /// Splitter machine splits items from input evenly to connected outputs
    /// </summary>
    public class Splitter : Machine, ISaveable
    {
        [Header("Machine IO")]
        public MachineInput Input;
        public MachineOutput Output1, Output2, Output3;

        private Item currentItem; //process 1 item at a time
        private int lastOutput = 0;

        // Start is called before the first frame update
        void Start()
        {
            //register machine IO
            InputOutputHub = new IOHub(new List<MachineInput>(), new List<MachineOutput>());

            InputOutputHub.Inputs().Add(Input);
            InputOutputHub.Outputs().Add(Output1);
            InputOutputHub.Outputs().Add(Output2);
            InputOutputHub.Outputs().Add(Output3);
              
            //set machine initialized flag
            IsInitializated = true;
        }

        public override bool ReceiveItem(Item item, MachineInput input)
        {
            if(currentItem == null)
            {
                currentItem = item;
                return true;
            }
            return false;
        }
        
        public override void MachineUpdate()
        {
            if(currentItem == null)
                return;

            for(int i = 0; i < 3; i++)
            {
                lastOutput++;
                lastOutput %= 3; 

                if(InputOutputHub.Outputs()[lastOutput].TryToSendItem(currentItem))
                {
                    currentItem = null;
                    break;
                }
            }
        }

        public string Save()
        {
            SplitterData data = new SplitterData();

            if(Output1.ConnectedTo != null)
            {
                data.OutputConnectedToMachineInputID1 = Output1.ConnectedTo.Parent.GetMachineID();
                data.OutputConnectedToInputGameObjectName1 = Output1.ConnectedTo.name;
            }
            if(Output2.ConnectedTo != null)
            {
                data.OutputConnectedToMachineInputID2 = Output2.ConnectedTo.Parent.GetMachineID();
                data.OutputConnectedToInputGameObjectName2 = Output2.ConnectedTo.name;
            }
            if(Output3.ConnectedTo != null)
            {
                data.OutputConnectedToMachineInputID3 = Output3.ConnectedTo.Parent.GetMachineID();
                data.OutputConnectedToInputGameObjectName3 = Output3.ConnectedTo.name;
            }

            return JsonUtility.ToJson(data);
        }
        
        public bool Load(string data)
        {
            SplitterData splitterData = JsonUtility.FromJson<SplitterData>(data);

            Output1.ConnectedTo = TryToFindMachineInput(splitterData.OutputConnectedToMachineInputID1, splitterData.OutputConnectedToInputGameObjectName1);
            Output2.ConnectedTo = TryToFindMachineInput(splitterData.OutputConnectedToMachineInputID2, splitterData.OutputConnectedToInputGameObjectName2);
            Output3.ConnectedTo = TryToFindMachineInput(splitterData.OutputConnectedToMachineInputID3, splitterData.OutputConnectedToInputGameObjectName3);

            return true;
        }

        /// <summary>
        /// Class used to store this machine data used for serialization/deserialization
        /// </summary> 
        [System.Serializable]
        private class SplitterData
        {
            public int OutputConnectedToMachineInputID1;
            public string OutputConnectedToInputGameObjectName1;

            public int OutputConnectedToMachineInputID2;
            public string OutputConnectedToInputGameObjectName2;

            public int OutputConnectedToMachineInputID3;
            public string OutputConnectedToInputGameObjectName3; 
        }
    }
}
