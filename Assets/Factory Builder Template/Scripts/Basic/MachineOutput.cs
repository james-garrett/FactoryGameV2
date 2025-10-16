using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FactoryBuilderTemplate
{
    /// <summary>
    /// Machine output class is used by the machine to send items to another machine input.
    /// </summary>
    public class MachineOutput : MonoBehaviour
    {
        [Header("Output to which this output belongs to")]
        public Machine Parent;

        [Header("Connected Input")]
        [Tooltip("Some machine input to which this output is connected to.")]
        public MachineInput ConnectedTo;

        public GameObject SpawnPositionPrefab;

        private Vector3 SpawnPosition;

        [Header("SpawnZone")]
        public BoxCollider spawnBufferZone;

        public bool spawnZoneFree;
        private float spawnClearTimer;
        public float spawnIntervalClearTimer;

        public void Start()
        {
            //try to find parent automatically
            if (Parent == null)
            {
                Parent = GetComponentInParent<Machine>();

                Debug.LogWarning("Please assign Parent field of machine output manually in inspector!");
            }
        }

        public void AssignSpawnPosition()
        {
            if ((SpawnPositionPrefab != null) && 
                (SpawnPositionPrefab.transform.position != null))
            {
                SpawnPosition = SpawnPositionPrefab.transform.position;
                //SpawnPosition = gameObject.transform.position;
            }
        }

        /// <summary>
        /// The method used to try push item through output into connected to it input
        /// </summary>
        /// <param name="item">Item machine is trying to push out</param>
        /// <returns>True when item was accepted by connected input false when connected input is null or item was declined by the input.</returns>
        public bool TryToSendItem(Item item)
        {
            if(SpawnPosition == Vector3.zero)
            {
                AssignSpawnPosition();
            }
            //if (spawnZoneFree && (Math.Abs(spawnClearTimer - Time.time) > spawnIntervalClearTimer) || spawnClearTimer == 0)
            //{
                if (ConnectedTo != null && item != null)
                {
                    //if(ConnectedTo.ReceiveItem(item))
                    Machine newOutputItem = SpawnItem(item);
                    if (newOutputItem != null) Machine.AllMachines.Add(newOutputItem);
                    return true;
                }
            //}
            return false;
        }

        public Machine SpawnItem(Item item)
        {
            Machine newObj = null;
            if (item.ItemDefinition.VisualItemPrefab != null)
            {
                newObj = Instantiate(item.ItemDefinition.VisualItemPrefab, SpawnPosition, Quaternion.identity).GetComponent<Machine>();
            }
            return newObj;
        }

        //public void OnCollisionEnter(Collision collision)
        //{
        //    spawnZoneFree = false;
        //    spawnClearTimer = Time.time;
        //}

        //public void OnCollisionExit(Collision collision)
        //{
        //    spawnZoneFree = true;
        //}
    }
}