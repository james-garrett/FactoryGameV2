using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FactoryBuilderTemplate
{
    /// <summary>
    /// Crafting machine, crafts items based on supplied Crafting Recipe and attached inventory
    /// </summary>
    [RequireComponent(typeof(Inventory))]
    public class CraftingMachine : Machine, ISaveable
    {
        //[Header("Crafting machine inputs and outputs")]
        //public List<MachineInput> InputOutputHub.Inputs;
        //public List<MachineOutput> InputOutputHub.Outputs;

        [Header("Recipe")]
        public CraftingRecipe Recipe;

        [Header("Events")]
        public UnityEvent CraftingStarted;
        public UnityEvent CraftingEnded;
        public UnityEvent OnCraftingProgressChange;

        public float CraftingProgress { get; private set; }

        private Inventory inventory;

        private float currentTime, craftingTimeStart;
        private bool isCrafting, lastIsCrafting;
        private float lastCraftingProgress;

        // Start is called before the first frame update
        void Start()
        {
            //register machine IO

            //IOHub.Inputs.AddRange(InputOutputHub.Inputs);
            //IOHub.Outputs.AddRange(InputOutputHub.Outputs);
            
            //check if supplied recipe is proper for this machine
            ValidateRecipe(Recipe);

            //grab inventory reference
            inventory = GetComponent<Inventory>();
            inventory.ChangeSlotsAmount(InputOutputHub.Inputs.Count + InputOutputHub.Outputs.Count);

            //set machine initialized flag
            IsInitializated = true;
        }

        /// <summary>
        /// Change crafting recipe
        /// </summary>
        /// <param name="recipe">New recipe</param>
        /// <returns>True if recipe was changed false when recipe is not proper for this machine.</returns>
        public bool ChangeRecipe(CraftingRecipe recipe)
        {
            if(ValidateRecipe(recipe) || recipe == null)
            {
                Recipe = recipe;

                //stop crafting process
                isCrafting = false;

                //clear inputs crafting inventory, keep outputs
                for(int i = 0; i < InputOutputHub.Inputs.Count; i++)
                    inventory.Slots[i].Clear();

                //send end crafting event
                FactoryBuilderMaster.RunOnMainThread.Enqueue(() => {
                    CraftingEnded.Invoke();
                });

                return true;
            }
            return false;
        }

        /// <summary>
        /// Method checking if supplied recipe is proper for this crafting machine
        /// </summary>
        /// <param name="recipe">Recipe to check</param>
        /// <returns>True of recipe is proper for this machine</returns>
        public bool ValidateRecipe(CraftingRecipe recipe)
        {
            if(recipe == null)
            {
                return false;
            }

            //check if inputs/outputs amount is enough for given recipe
            if(!(recipe.Ingredients.Count <= InputOutputHub.Inputs.Count && recipe.CraftingResults.Count <= InputOutputHub.Outputs.Count))
            {
                Debug.LogError("Crafting recipe supplied to this crafting machine is invalid! Amount of inputs or outputs is not enough for this recipe!");
                return false;
            }

            //check recipe required machine type
            if(recipe.Type != Type)
            {
                Debug.LogError("Recipe requires different type of crafting machine (Required: " + recipe.Type + " this machine type: " + Type + ") it probably shouldn't be there!");
                return false;
            }

            //check if amount of crafting results in recipe is correct
            foreach(CraftingRecipe.Ingredient ingredient in recipe.CraftingResults)
            {
                if(!ingredient.IngredientDefinition.Stackable && ingredient.IngredientAmount > 1)
                {
                    Debug.LogError("If output item is not stackable OuputAmount have to be 1! Can/will cause inventory bugs!");
                    return false;
                }
            }

            return true;
        }

        // Update is called once per frame
        void Update()
        {
            //update crafting progress event
            if(isCrafting)
            {
                CraftingProgress = (Time.time - craftingTimeStart) / Recipe.CraftingTime;
                OnCraftingProgressChange.Invoke();
            } else
            {
                CraftingProgress = 0f;

                if(lastCraftingProgress != CraftingProgress)
                    OnCraftingProgressChange.Invoke();
            }

            //cache current time because it can be accessed only on main thread
            currentTime = Time.time;
            lastCraftingProgress = CraftingProgress;
        }

        /// <summary>
        /// Check if amount of ingredients is enough to start crafting process
        /// </summary>
        /// <returns>True if machine have enough ingredients in inventory and space in output slot and can start crafting process.</returns>
        private bool CheckIfCanCraft()
        {
            //cant craft if there is no recipe
            if(Recipe == null)
                return false;

            //check if inventory contains every needed item to start crafting process
            foreach(CraftingRecipe.Ingredient ingredient in Recipe.Ingredients)
            {
                bool ingredientNeedMet = false;

                for(int i = 0; i < InputOutputHub.Inputs.Count; i++)
                {
                    InventorySlot slot = inventory.Slots[i];
                    if(ingredient.IngredientDefinition == slot.GetItemDefinition())
                    {
                        if(ingredient.IngredientAmount <= slot.InStack())
                        {
                            ingredientNeedMet = true;
                            break;
                        }
                    }
                }

                //machine lacking some ingredient cant craft
                if(!ingredientNeedMet)
                {
                    return false;
                }
            }

            //check if output inventory have enough space to crafting anything 
            for(int i = 0; i < Recipe.CraftingResults.Count; i++)
            {
                CraftingRecipe.Ingredient result = Recipe.CraftingResults[i];
                if(!inventory.Slots[InputOutputHub.Inputs.Count + i].HaveSpaceForItems(result.IngredientDefinition, result.IngredientAmount))
                    return false;
            }

            return true;
        }
         
        public override bool ReceiveItem(Item item, MachineInput input)
        {
            //check if this machine input is known
            if(!InputOutputHub.Inputs.Contains(input))
            {
                Debug.LogError("Machine was trying to receive item from unknown machine input, make sure machine inputs are added to machine Inputs list in inspector!");
                return false;
            }

            //cant receive items if current recipe is null
            if(Recipe == null)
                return false;

            //check if item is on list of ingredients
            bool onIngredientsList = false;
            for(int i = 0; i < Recipe.Ingredients.Count; i++)
            {
                CraftingRecipe.Ingredient ingredient = Recipe.Ingredients[i];
                if(item.ItemDefinition == ingredient.IngredientDefinition)
                {
                    onIngredientsList = true;
                    break;
                }
            }
            if(!onIngredientsList)
                return false;

            //if item is on list of ingredients try to add it to inventory slot corresponding to machine input index
            InventorySlot slot = inventory.Slots[InputOutputHub.Inputs.IndexOf(input)];
            return slot.Add(item);
        }
          
        public override void MachineUpdate()
        {
            //check if machine can start crafting
            if(!isCrafting && CheckIfCanCraft())
            {
                isCrafting = true;
                craftingTimeStart = currentTime;

                FactoryBuilderMaster.RunOnMainThread.Enqueue(() => {
                    CraftingStarted.Invoke();
                });
            }

            //check if crafting ended
            if(isCrafting && currentTime - craftingTimeStart >= Recipe.CraftingTime)
            {
                //remove ingredients needed to craft items
                for(int i = 0; i < InputOutputHub.Inputs.Count; i++)
                {
                    InventorySlot slot = inventory.Slots[i];
                    foreach(CraftingRecipe.Ingredient ingredient in Recipe.Ingredients)
                    {
                        if(ingredient.IngredientDefinition == slot.GetItemDefinition())
                        {
                            for(int j = 0; j < ingredient.IngredientAmount; j++)
                                slot.GetLastAndRemove();

                            break;
                        }
                    }
                }

                //send crafting results to inventory slots 
                for(int i = 0; i < Recipe.CraftingResults.Count; i++)
                {
                    CraftingRecipe.Ingredient result = Recipe.CraftingResults[i];

                    for(int j = 0; j < result.IngredientAmount; j++)
                        inventory.Slots[InputOutputHub.Inputs.Count + i].Add(new Item(result.IngredientDefinition));
                }

                isCrafting = false;

                if(!CheckIfCanCraft())
                {
                    FactoryBuilderMaster.RunOnMainThread.Enqueue(() => {
                        CraftingEnded.Invoke();
                    });
                }
            }

            //try to push items from output inventory to connected inputs
            for(int i = 0; i < InputOutputHub.Outputs.Count; i++)
            {
                if(!inventory.Slots[InputOutputHub.Inputs.Count + i].IsEmpty() && InputOutputHub.Outputs[i].TryToSendItem(inventory.Slots[InputOutputHub.Inputs.Count + i].GetLast()))
                {
                    inventory.Slots[InputOutputHub.Inputs.Count + i].GetLastAndRemove();
                }
            }
        }

        public string Save()
        {
            CraftingMachineData data = new CraftingMachineData(); 
            data.InventoryData = inventory.Save();

            //save current recipe index
            for(int i = 0; i < FactoryBuilderMaster.Instance.AllRecipes.Length; i++)
            {
                if(Recipe != null && Recipe == FactoryBuilderMaster.Instance.AllRecipes[i])
                {
                    data.RecipeIndex = i;
                    break;
                }
            }

            //save outputs connections
            data.Outputs = new List<MachineOutputData>();
            for(int i = 0; i < InputOutputHub.Outputs.Count; i++)
            {
                MachineOutput output = InputOutputHub.Outputs[i];
                if(output.ConnectedTo != null) {
                    MachineOutputData outputData = new MachineOutputData();
                    outputData.OutputConnectedToMachineInputID = output.ConnectedTo.Parent.GetMachineID();
                    outputData.OutputConnectedToInputGameObjectName = output.ConnectedTo.name;
                    outputData.index = i;

                    data.Outputs.Add(outputData);
                }
            }

            return JsonUtility.ToJson(data);
        }

        public bool Load(string data)
        {
            CraftingMachineData machineData = JsonUtility.FromJson<CraftingMachineData>(data);
            inventory.Load(machineData.InventoryData);

            //restore current recipe
            if(machineData.RecipeIndex >= 0) 
                Recipe = FactoryBuilderMaster.Instance.AllRecipes[machineData.RecipeIndex];

            //restore where outputs where connected
            for(int i = 0; i < InputOutputHub.Outputs.Count; i++)
            {
                foreach(MachineOutputData outputData in machineData.Outputs)
                {
                    if(outputData.index == i)
                    {
                        InputOutputHub.Outputs[i].ConnectedTo = TryToFindMachineInput(outputData.OutputConnectedToMachineInputID, outputData.OutputConnectedToInputGameObjectName);
                        break;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Classes used to store this machine data used for serialization/deserialization
        /// </summary> 

        [System.Serializable]
        private class CraftingMachineData
        {
            public int RecipeIndex = -1; //-1 equals selected recipe was null
            public string InventoryData;

            public List<MachineOutputData> Outputs;
        }
        [System.Serializable]
        private class MachineOutputData
        {
            public int index;
            public int OutputConnectedToMachineInputID;
            public string OutputConnectedToInputGameObjectName;
        }
    }
}
