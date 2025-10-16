using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FactoryBuilderTemplate
{ 
    /// <summary>
    /// Base class for all components in this template. Any game object that can receive/send/produce items is a machine it's a base abstract class for framework.
    /// </summary>
    [System.Serializable]
    public class Machine : MonoBehaviour
    {
        /// <summary>
        /// List containing all machines in the current scene
        /// Used in FactoryBuilderMaster to perform updates
        /// </summary>
        public static List<Machine> AllMachines = new List<Machine>();

        //[HideInInspector]
        //public List<MachineInput> Inputs;
        //[HideInInspector]
        //public List<MachineOutput> Outputs;

        public IOHub InputOutputHub = new IOHub();

        public enum MachineType
        {
            Crafting, Smelting, Mining, Transport
        }

        [Header("Basic machine parameters")]
        public MachineType Type;

        /// <summary>
        /// Name of this machine original prefab, used in save load system
        /// </summary>
        public string PrefabName;

        protected void Awake()
        {
            AllMachines.Add(this);
        }

        protected void OnDestroy()
        {
            AllMachines.Remove(this);
        }

        /// <summary>
        /// Method used to send items between two machines.
        /// Called when other machines are trying to send item to input.
        /// Machine can accept or decline based on return value.
        /// </summary>
        /// <param name="item">The item machine can receive.</param>
        /// <param name="input">To which input item is send.</param>
        /// <returns>Return true when machine accepts items false otherwise.</returns>
        public virtual bool ReceiveItem(Item item, MachineInput input) { return false; }

        /// <summary>
        /// Method used to update all machines, when writing code here be aware that it can run on separate thread so make code thread safe. 
        /// If you don't want to run this method on separate thread but on main thread instead uncheck option called Run On Separate Thread in FactoryBuilderMaster
        /// </summary>
        public virtual void MachineUpdate() { }

        /// <summary>
        /// MachineUpdate() will be only called if machine return true in this function, machines set this flag at the end of their Start method.
        /// </summary>
        public bool IsInitializated { get; protected set; }

        /// <summary>
        /// ID of machine that is supposed to be unique and persistent if a machine doesn’t change its transform, 
        /// ID is based on object position and rotation used in save load system 
        /// </summary>
        /// <returns>Machine ID that is supposed to be unique and persistent as long as machine transform doesn't change</returns>
        public int GetMachineID()
        {
            int signAdd = (int) (Mathf.Sign(transform.position.x) * 31 + Mathf.Sign(transform.position.y) * 9 + Mathf.Sign(transform.position.z) * 2);
            return (int) ((transform.position.sqrMagnitude * 1000) + (transform.rotation.x + transform.rotation.y + transform.rotation.z + transform.rotation.w)) + signAdd;
        }

        /// <summary>
        /// Helper function used by save load system to find to which input currently loading machine is connected to
        /// </summary>
        /// <param name="machineID">ID of machine to which input belongs to</param>
        /// <param name="inputName">name of the input game object</param>
        /// <returns>Machine input that belongs to given machineID and its game object name is inputName</returns>
        protected static MachineInput TryToFindMachineInput(int machineID, string inputName)
        {
            //connect machine output
            Machine connectedTo = FactoryBuilderMaster.Instance.GetMachineByID(machineID);
            if(connectedTo != null)
            {
                //try to find proper machine input
                foreach(MachineInput input in connectedTo.InputOutputHub.Inputs)
                {
                    if(input.name == inputName)
                    {
                        return input;
                    }
                }
            }
            return null;
        }
    }
}
