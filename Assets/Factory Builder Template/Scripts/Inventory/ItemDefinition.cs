using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FactoryBuilderTemplate
{
    /// <summary>
    /// Item definition/type scriptable class, here parameters of items are stored and used later when creating Item objects
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDefinition1", menuName = "Factory Builder/New Item Definition", order = 1)]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Item parameters")]
        [Tooltip("The name of the item")]
        public string ItemName = "not_set";

        [Tooltip("Is this item stackable")]
        public bool Stackable = false;
        [Tooltip("How many items in the stack, used only when Stackable is true")]
        public int MaxInStack = 16;

        [Header("Inventory View")]
        [Tooltip("Sprite used in UI when rendering item preview f.e. in inventory view")]
        public Sprite ItemIcon;

        [Header("On Belt Rendering")]

        [Tooltip("When using instanced rendering only mesh, material, rotation, and scale will be used to render items on the belt. Items on belts are not individual GameObjects so any scripts attached to the item prefab will not execute when the item is on the conveyor belt unless Instanced Rendering is turned off")]
        public GameObject VisualItemPrefab;
        [Tooltip("Does item on conveyor belt cast shadows")]
        public bool CastShadowOnBelt = true;
        [Tooltip("Does item on the conveyor belt receive shadows")]
        public bool ReceiveShadowOnBelt = true;

        [Tooltip("Vector used to offset item position on the belt to correct its pivot")]
        public Vector3 PivotOffset;

        [Tooltip("The minimal distance between the pivot of this item and the next one on the belt")]
        public float SpaceNeededInFrontOfItemOnBelt = 0.5f;

        [Tooltip("Please use instanced rendering when possible, using conventional game objects to render stuff on belts is very ineffective even with pooling in use, and with bigger setups you will probably encounter lag spikes")]
        public bool RenderInstanced = true;

    }

    /// <summary>
    /// Item class which parameters are defined by ItemDefinition inside it
    /// </summary>
    [System.Serializable]
    public class Item : ISerializationCallbackReceiver
    {
        /// <summary>
        /// Scriptable object defining this item instance
        /// </summary>
        [System.NonSerialized]
        public ItemDefinition ItemDefinition;

        /// <summary>
        /// Index in an array stored in FactoryBuilderMaster which contains all possible items, 
        /// it has to be serialized this way because unity serialization doesn’t serialize references \
        /// to scriptable objects 
        /// </summary>
        public int ItemDefinitionID = -1;

        public Item(ItemDefinition definition)
        {
            ItemDefinition = definition;
        }

        public void OnAfterDeserialize()
        {
            //get item definition reference based on saved index
            //if(ItemDefinitionID != -1 && FactoryBuilderMaster.Instance != null)
            //    ItemDefinition = FactoryBuilderMaster.Instance.AllItemDefinitions[ItemDefinitionID];
        }

        public void OnBeforeSerialize()
        {
            //remember index of item definition
            if(FactoryBuilderMaster.Instance != null)
            { 
                for(int i = 0; i < FactoryBuilderMaster.Instance.AllItemDefinitions.Length; i++)
                {
                    if(ItemDefinition == FactoryBuilderMaster.Instance.AllItemDefinitions[i])
                    {
                        ItemDefinitionID = i;
                        break;
                    }
                }
            }
        }
    }
}
