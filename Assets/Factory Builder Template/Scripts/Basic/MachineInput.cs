using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FactoryBuilderTemplate
{
    /// <summary>
    /// Machine input class, used to be able to connect other machine output with this machine input
    /// </summary>
    public class MachineInput : MonoBehaviour
    {
        [Header("Machine to which this input belongs to")]
        [Tooltip("Please set parent of this input here, remember to update this value when copying existing inputs/outputs to new machine.")]
        public Machine Parent;

        public ItemDefinition defaultBlockItem;

        public MachineOutput ConnectedTo;

        private void Start()
        {
            //try to find parent
            if(Parent == null)
            {
                Parent = GetComponentInParent<Machine>();

                Debug.LogWarning("Please assign Parent field of machine input manually in inspector! Machine can not receive items otherwise.");
            }
        }

        public void OnTriggerEnter(Collider other)
        {
            Block block = other.gameObject.GetComponent<Block>();
            Item item = block.Item;
            if (item == null) item = new Item(defaultBlockItem);
            if(Parent.ReceiveItem(item, this))
            {
                Destroy(other.gameObject);
            }
        }
    }
}
