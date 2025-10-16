using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FactoryBuilderTemplate
{
    /// <summary>
    /// Crafting Recipe scriptable object class
    /// Used by crafting machines to craft new items
    /// </summary>
    [CreateAssetMenu(fileName = "CraftingRecipe1", menuName = "Factory Builder/New Crafting Recipe", order = 1)]
    public class CraftingRecipe : ScriptableObject
    {
        [System.Serializable]
        public class Ingredient
        {
            public ItemDefinition IngredientDefinition;
            public int IngredientAmount = 1;
        }

        [Header("Crafting machine type")]
        public Machine.MachineType Type = Machine.MachineType.Crafting;

        [Header("Ingredients")]
        public List<Ingredient> Ingredients;

        [Header("Crafting result")]
        public List<Ingredient> CraftingResults;

        [Header("Crafting parameters")]
        [Tooltip("Crafting time in seconds")]
        public float CraftingTime = 1;
        [Tooltip("Name used in menu where user selects what recipe machine uses. Have to be unique!")]
        public string CraftingRecipeName = "CraftingRecipe0";
          
    }
}
