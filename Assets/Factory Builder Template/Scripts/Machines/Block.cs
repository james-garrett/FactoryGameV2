using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FactoryBuilderTemplate
{
    /// <summary>
    /// Ore “machine” used by miners to extract ores
    /// </summary>
    public class Block : Machine, ISaveable
    {
        [Header("Ore spawning options")]
        public ItemDefinition ItemDef;
        public Item Item;

        public float moveSpeed;
        public Vector3 moveDirection;

        [Tooltip("Is ore infinite?")]
        public bool IsInfinite = false;

        [Tooltip("Used only if isInifinite equals false")]
        public int Amount = 10000;

        [Header("Events")]
        [Tooltip("Event called when ore is saturated, you can f.e. destroy it, change its material to indicate its empty.")]
        public UnityEvent OnOreEnd;

        //public Miner ConnectedMiner;

        private void Start()
        {
            Item = new Item(ItemDef);
            IsInitializated = true;
        }

        private void Update()
        {
            //disconnect miner from ore if it got destroyed or deactivated
            //if(ConnectedMiner != null && !ConnectedMiner.gameObject.activeInHierarchy)
            //{
            //    ConnectedMiner.ConnectedOre = null;
            //    ConnectedMiner = null;
            //} 
        }

        /// <summary>
        /// Mine one piece of ore from this source if possible
        /// </summary>
        public void MineOre()
        {
            if (!IsInfinite)
                Amount--;

            if (Amount == 0 && !IsInfinite)
            {
                FactoryBuilderMaster.RunOnMainThread.Enqueue(() =>
                {
                    OnOreEnd.Invoke();
                });
            }
        }

        /// <summary>
        /// Checks if anything can be mined 
        /// </summary>
        /// <returns>True if there is something to mine</returns>
        public bool CanMine()
        {
            return Amount > 0 || IsInfinite;
        }

        public string Save()
        {
            OreData data = new OreData();
            data.Amount = Amount;

            return JsonUtility.ToJson(data);
        }

        public bool Load(string data)
        {
            OreData oreData = JsonUtility.FromJson<OreData>(data);
            Amount = oreData.Amount;

            if (Amount <= 0 && !IsInfinite)
                OnOreEnd.Invoke();

            return true;
        }

        /// <summary>
        /// Class used to store this machine data used for serialization/deserialization
        /// </summary> 
        [System.Serializable]
        private class OreData
        {
            public int Amount;
        }

        public void OnCollisionEnter(Collision collision)
        {
            ConveyorBelt beltObj = collision.gameObject.GetComponent<ConveyorBelt>();
            if (beltObj != null)
            {
                moveSpeed = beltObj.speed;
                moveDirection = beltObj.direction;
                gameObject.GetComponent<Rigidbody>().velocity = moveSpeed * moveDirection * Time.deltaTime;
            }
        }
    }
}
