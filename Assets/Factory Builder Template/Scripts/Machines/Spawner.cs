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
    public class Spawner : Machine, ISaveable
    {
        [Header("Machine output")]
        public MachineOutput Output;

        [Header("Ore settings")]
        public Ore ResourceToMine;

        [Space]
        [Tooltip("Miner will try to find ore nearby and automatically connect to it if current Connected Ore is empty")]
        public bool AutomaticallyTryToConnectToNearbyOre = true;
        [Tooltip("Maximum distance from miner to ore, used only when AutomaticallyTryToConnectToNearbyOre is set to true.")]
        public float MaxOreDistance = 10;
        [Tooltip("Helper showing radius of ore searching process when Miner is selected in inspector in Scene View")]
        public bool DrawRadiusSphereWhenSelected = true;

        [Header("Mining speed")]
        public int ItemsPerSecond = 1;

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

        public float fertilityScore;
        public float MinRequiredFertilityScoreForBaby;
        public bool CanProduceResource { get; private set; }

        void Start()
        {
            //register machine IO
            //InputOutputHub.Outputs().Add(Output);

            InputOutputHub = new IOHub();

            //grab inventory reference
            inventory = GetComponent<Inventory>();

            //assign miner to ore if miner already connected to ore
            //if (ResourceToMine != null)
            //    ResourceToMine.ConnectedMiner = this;

            //try to connect miner to nearby ore if possible

            lastIsMining = isMining;

            //set machine initialized flag
            IsInitializated = true;
        }

        void Update()
        {
            time = Time.time;

            //if (ResourceToMine == null)
            //    TryToFindOre();
            CheckFertilityScore();
        }

        //Rewritten to check if fertility score is high enough to generate a baby
        public void CheckFertilityScore()
        {
            bool fertilityScoreMeetsMinRequirement = fertilityScore > MinRequiredFertilityScoreForBaby;
            CanProduceResource = fertilityScoreMeetsMinRequirement;
        }

        public override void MachineUpdate()
        {
            //try to mine
            if(Output.ConnectedTo != null)
            {
                if (time - spawnTimer > CalculatePeriod())
                {
                    if (CanProduceResource)
                    {
                        if (inventory.Add(new Item(ResourceToMine.OreItem)))
                        {
                            //if item was added to internal inventory it means miner is mining
                            isMining = true;
                            lastItemSpawnTime = time;

                            ResourceToMine.MineOre();
                        }
                    }

                spawnTimer = time;
                }
                //try to push items out 
                if (toSend == null)
                    toSend = inventory.GetLastAndRemove();

                if (Output.TryToSendItem(toSend))
                {
                    toSend = null;
                }
            }


            //try to call events
            if (isMining && time - lastItemSpawnTime > StopMiningThreshold)
            {
                isMining = false;

                FactoryBuilderMaster.RunOnMainThread.Enqueue(() => {
                    MiningStopped.Invoke();
                });
            }

            if (isMining && !lastIsMining)
            {
                FactoryBuilderMaster.RunOnMainThread.Enqueue(() => {
                    MiningStarted.Invoke();
                });
            }

            lastIsMining = isMining;
        }

        private float CalculatePeriod()
        {
            return 1f / ItemsPerSecond;
        }

        public bool Load(string data)
        {
            MinerData minerData = JsonUtility.FromJson<MinerData>(data);

            //restore connection between miner and ore
            if (minerData.ConnectedToOreMachineID != -1)
            {
                ResourceToMine = (Ore)FactoryBuilderMaster.Instance.GetMachineByID(minerData.ConnectedToOreMachineID);
                //if (ResourceToMine != null)
                //    ResourceToMine.ConnectedMiner = this;
            }

            //load internal inventory
            inventory.Load(minerData.InventoryData);

            //connect miner output
            Output.ConnectedTo = TryToFindMachineInput(minerData.OutputConnectedToMachineInputID, minerData.OutputConnectedToInputGameObjectName);

            return true;
        }

        public string Save()
        {
            MinerData data = new MinerData();

            //save ore to which miner is connected
            if (ResourceToMine != null)
                data.ConnectedToOreMachineID = ResourceToMine.GetMachineID();

            //save miner internal inventory
            data.InventoryData = inventory.Save();

            //save output connection
            if (InputOutputHub.Outputs()[0].ConnectedTo != null)
            {
                data.OutputConnectedToMachineInputID = InputOutputHub.Outputs()[0].ConnectedTo.Parent.GetMachineID();
                data.OutputConnectedToInputGameObjectName = InputOutputHub.Outputs()[0].ConnectedTo.name;
            }

            return JsonUtility.ToJson(data);
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
