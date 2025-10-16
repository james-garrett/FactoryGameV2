using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FactoryBuilderTemplate
{
    /// <summary>
    /// Mining machines have to be connected to some ore to start the mining process. 
    /// Miner is storing ores in attached inventory and tries to push items to its output if possible.
    /// </summary>
    [RequireComponent(typeof(Inventory))]
    public class Miner : Machine, ISaveable
    {
        [Header("Machine output")]
        public MachineOutput Output;

        [Header("Ore settings")]
        public Ore ConnectedOre;

        [Space]
        [Tooltip("Miner will try to find ore nearby and automatically connect to it if current Connected Ore is empty")]
        public bool AutomaticallyTryToConnectToNearbyOre = true;
        [Tooltip("Maximum distance from miner to ore, used only when AutomaticallyTryToConnectToNearbyOre is set to true.")]
        public float MaxOreDistance = 10;
        [Tooltip("Helper showing radius of ore searching process when Miner is selected in inspector in Scene View")]
        public bool DrawRadiusSphereWhenSelected = true;

        [Header("Mining speed")]
        public int ItemsPerSecond = 10;

        [Header("Events")]
        public UnityEvent MiningStarted;
        public UnityEvent MiningStopped;

        [Header("Events invoking parameters")]
        [Tooltip("Miner has to be idle for at least this amount of time in second to MiningStopped event can be called. Used to prevent calling stop mining events a lot.")]
        public float StopMiningThreshold = 2f;

        private bool isMining = false; //used for calling events
        private bool lastIsMining;

        private Inventory inventory;
        private float time, spawnTimer, lastItemSpawnTime;
        private Item toSend;

        // Start is called before the first frame update
        void Start()
        {
            //register machine IO
            InputOutputHub.Outputs().Add(Output);

            //grab inventory reference
            inventory = GetComponent<Inventory>();

            //assign miner to ore if miner already connected to ore
            if(ConnectedOre != null)
                ConnectedOre.ConnectedMiner = this;

            //try to connect miner to nearby ore if possible
            TryToFindOre();

            lastIsMining = isMining;

            //set machine initialized flag
            IsInitializated = true;             
        }

        /// <summary>
        /// Method trying to find ore nearby miner and connect to it
        /// </summary>
        public virtual void TryToFindOre()
        {
            if(ConnectedOre == null && AutomaticallyTryToConnectToNearbyOre)
            {
                Collider[] hitColliders = Physics.OverlapSphere(transform.position, MaxOreDistance);
                float dst = float.MaxValue;
                foreach(Collider hitCollider in hitColliders)
                {
                    Ore potentialOre = hitCollider.gameObject.GetComponent<Ore>();
                    if(potentialOre != null && potentialOre.ConnectedMiner == null)
                    {
                        float dstToPotentialOre = Vector3.Distance(potentialOre.transform.position, transform.position);
                        if(ConnectedOre != null)
                        {
                            if(dstToPotentialOre < dst)
                            {
                                dst = dstToPotentialOre;
                                ConnectedOre = potentialOre;
                            }
                        } 
                        else
                        {
                            ConnectedOre = potentialOre;
                            dst = dstToPotentialOre;
                        }
                    }
                }

                if(ConnectedOre != null)
                { 
                    ConnectedOre.ConnectedMiner = this;
                }
            }
        }

        /// <summary>
        /// Draw radius of ore searching process
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if(DrawRadiusSphereWhenSelected)
                Gizmos.DrawWireSphere(transform.position, MaxOreDistance);
        }

        // Update is called once per frame
        void Update()
        {
            time = Time.time;

            if(ConnectedOre == null)
                TryToFindOre();
        }

        /// <summary>
        /// Calculate period of mining process
        /// </summary>
        /// <returns></returns>
        private float CalculatePeriod()
        {
            return 1f / ItemsPerSecond;
        }

        public override void MachineUpdate()
        {
            //try to mine
            if(time - spawnTimer > CalculatePeriod())
            {
                if(ConnectedOre != null && ConnectedOre.CanMine())
                {
                    if(inventory.Add(new Item(ConnectedOre.OreItem)))
                    {
                        //if item was added to internal inventory it means miner is mining
                        isMining = true;
                        lastItemSpawnTime = time;

                        ConnectedOre.MineOre();
                    }
                }

                spawnTimer = time;
            }

            //try to push items out 
            if(toSend == null)
                toSend = inventory.GetLastAndRemove();

            if(Output != null && 
                Output.TryToSendItem(toSend))
            {
                toSend = null;
            }

            //try to call events
            if(isMining && time - lastItemSpawnTime > StopMiningThreshold)
            {
                isMining = false;

                FactoryBuilderMaster.RunOnMainThread.Enqueue(() => {
                    MiningStopped.Invoke();
                });
            }

            if(isMining && !lastIsMining)
            {
                FactoryBuilderMaster.RunOnMainThread.Enqueue(() => {
                    MiningStarted.Invoke();
                });
            }

            lastIsMining = isMining;
        }

        new void OnDestroy()
        {
            if(ConnectedOre != null)
                ConnectedOre.ConnectedMiner = null;
             
            base.OnDestroy();
        }

        public string Save()
        {
            MinerData data = new MinerData();

            //save ore to which miner is connected
            if(ConnectedOre != null)
                data.ConnectedToOreMachineID = ConnectedOre.GetMachineID();

            //save miner internal inventory
            data.InventoryData = inventory.Save();

            //save output connection
            if(InputOutputHub.Outputs()[0].ConnectedTo != null)
            {
                data.OutputConnectedToMachineInputID = InputOutputHub.Outputs()[0].ConnectedTo.Parent.GetMachineID();
                data.OutputConnectedToInputGameObjectName = InputOutputHub.Outputs()[0].ConnectedTo.name;
            }

            return JsonUtility.ToJson(data);
        }

        public bool Load(string data)
        {
            MinerData minerData = JsonUtility.FromJson<MinerData>(data);

            //restore connection between miner and ore
            if(minerData.ConnectedToOreMachineID != -1)
            {
                ConnectedOre = (Ore) FactoryBuilderMaster.Instance.GetMachineByID(minerData.ConnectedToOreMachineID);
                if(ConnectedOre != null)
                    ConnectedOre.ConnectedMiner = this;
            }

            //load internal inventory
            inventory.Load(minerData.InventoryData);

            //connect miner output
            Output.ConnectedTo = TryToFindMachineInput(minerData.OutputConnectedToMachineInputID, minerData.OutputConnectedToInputGameObjectName);

            return true;
        }

        /// <summary>
        /// Class used to store this machine data used for serialization/deserialization
        /// </summary> 
        [System.Serializable]
        private class MinerData
        {
            public int OutputConnectedToMachineInputID;
            public string OutputConnectedToInputGameObjectName;

            public int ConnectedToOreMachineID = -1;
            public string InventoryData;
        }

    }
}
