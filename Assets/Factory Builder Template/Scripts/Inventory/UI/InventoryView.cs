using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FactoryBuilderTemplate
{
    /// <summary>
    /// Example class of how to display UI for specific machines.
    /// This class doesn’t have to be updated as long as UI containers this class uses remain the same(look in inspector how UI hierarchy 
    /// f.e.ingredients list is just scrollable panel and its contents are getting generated 
    /// here based on currently selected recipe) so stuff like position, sprites can be changed without any code modifications.
    /// </summary>
    public class InventoryView : MonoBehaviour
    {
        public static InventoryView Instance;

        public Transform ItemViewPrefab;

        [Header("General Inventory View")]
        [Tooltip("Parent object of all objects responsible for showing UI of basic inventory")]
        public Transform GeneralInventoryViewPanel;
        [Tooltip("panel where all items in inventory will be displayed as a set of ItemViewPrefabs")]
        public Transform GeneralContentPanel;

        [Header("Crafting Machine View")]
        [Tooltip("Parent object of all objects responsible for showing UI of crafting machine inventory and its current recipe ")]
        public Transform CraftingMachineViewPanel;
        [Tooltip("Panel which contains all ingredients of current crafting machine recipe")]
        public Transform CraftingMachineIngredientsContent;
        [Tooltip("panel which contains all results of current crafting machine recipe")]
        public Transform CraftingMachineResultsContent;
        [Tooltip("Panel which contains all currently stored items of crafting machine inventory")]
        public Transform CraftingMachineStoredItemsContent;
        [Tooltip("Dropdown used to display all possible recipes for the current crafting machine")]
        public Dropdown CraftingMachineRecipesDropdown;

        [Header("Miners View")]
        [Tooltip("Parent object of all objects responsible for showing UI of a miner and whats it currently mining")]
        public Transform MinerViewPanel;
        [Tooltip("Panel which contains all currently stored items in miner internal inventory")]
        public Transform MinerStoredItemsContent;
        [Tooltip("Panel which contains what miner is currently mining and how much it mines per second")]
        public Transform MinerMiningItemsContent;

        private Inventory inventory;
        private Machine machine;
         
        public InventoryView()
        {
            Instance = this;
        }
         
        /// <summary>
        /// Show inventory view of given machine or just inventory
        /// </summary>
        /// <param name="machine">Machine to which inventory belongs to</param>
        /// <param name="inventory">Inventory containing items</param>
        public void Show(Machine machine, Inventory inventory)
        {
            transform.gameObject.SetActive(true);
            this.inventory = inventory;
            this.machine = machine;

            if(machine != null && machine is CraftingMachine && (machine.Type == Machine.MachineType.Crafting || machine.Type == Machine.MachineType.Smelting))
            {
                ShowCraftingMachineView();
            } 
            else if(machine != null && machine is Miner && machine.Type == Machine.MachineType.Mining)
            {
                ShowMinerView();
            } 
            else
            {
                ShowGeneralInventoryView();
            }
        }

        private void ShowCraftingMachineView()
        {
            CraftingMachine craftingMachine = (CraftingMachine) machine;
            CraftingRecipe currentRecipe = craftingMachine.Recipe;

            //hide/show parts of UI
            GeneralInventoryViewPanel.gameObject.SetActive(false);
            MinerViewPanel.gameObject.SetActive(false);
            CraftingMachineViewPanel.gameObject.SetActive(true);
            CraftingMachineRecipesDropdown.onValueChanged.RemoveAllListeners();

            //find all possible recipes and show them 
            CraftingMachineRecipesDropdown.ClearOptions();
            List<Dropdown.OptionData> optionsList = new List<Dropdown.OptionData>();
            optionsList.Add(new Dropdown.OptionData("None"));

            List<CraftingRecipe> possibleRecipes = FactoryBuilderMaster.Instance.GetAllPossibleRecipesForGivenCraftingMachine(craftingMachine.Type, craftingMachine.InputOutputHub.Inputs().Count, craftingMachine.InputOutputHub.Outputs().Count);
            foreach(CraftingRecipe recipe in possibleRecipes)
                optionsList.Add(new Dropdown.OptionData(recipe.CraftingRecipeName));

            CraftingMachineRecipesDropdown.AddOptions(optionsList);

            //select current recipe in dropdown
            if(currentRecipe != null)
                CraftingMachineRecipesDropdown.value = CraftingMachineRecipesDropdown.options.FindIndex(option => option.text == currentRecipe.CraftingRecipeName);

            //update ingredients list
            CraftingMachineViewUpdateIngredientsLists(currentRecipe);

            //listen for recipe selection
            CraftingMachineRecipesDropdown.onValueChanged.AddListener(delegate {
                //change machine recipe if user changed it 
                string newCraftingRecipeName = CraftingMachineRecipesDropdown.options[CraftingMachineRecipesDropdown.value].text;

                if(CraftingMachineRecipesDropdown.value == 0) //user selected none
                {
                    craftingMachine.ChangeRecipe(null);
                    CraftingMachineViewUpdateIngredientsLists(null);
                } 
                else
                {
                    foreach(CraftingRecipe recipe in possibleRecipes)
                    {
                        if(recipe != null && recipe.CraftingRecipeName == newCraftingRecipeName)
                        {
                            craftingMachine.ChangeRecipe(recipe);
                            CraftingMachineViewUpdateIngredientsLists(recipe);
                            break;
                        }
                    }
                }
            });

            //show machine stored items 
            foreach(Transform child in CraftingMachineStoredItemsContent)
            {
                GameObject.Destroy(child.gameObject);
            }

            for(int i = 0; i < inventory.SlotsAmount; i++)
            {
                Transform newItemView = Instantiate(ItemViewPrefab, CraftingMachineStoredItemsContent);
                ItemView view = newItemView.GetComponent<ItemView>();

                InventorySlot slot = (i >= inventory.Slots.Count) ? null : inventory.Slots[i];
                view.Slot = slot;
            }
        }

        private void CraftingMachineViewUpdateIngredientsLists(CraftingRecipe recipe)
        {
            //clear old ingredients
            foreach(Transform child in CraftingMachineIngredientsContent)
                GameObject.Destroy(child.gameObject);

            foreach(Transform child in CraftingMachineResultsContent)
                GameObject.Destroy(child.gameObject);

            if(recipe == null)
                return;

            //show ingredients for current recipe
            foreach(CraftingRecipe.Ingredient ingredient in recipe.Ingredients)
            {
                Transform newItemView = Instantiate(ItemViewPrefab, CraftingMachineIngredientsContent);
                ItemView view = newItemView.GetComponent<ItemView>();

                InventorySlot slot = new InventorySlot();
                for(int i = 0; i < ingredient.IngredientAmount; i++)
                    slot.Add(new Item(ingredient.IngredientDefinition));

                view.Slot = slot;
            }

            //show crafting result for current recipe 
            foreach(CraftingRecipe.Ingredient ingredient in recipe.CraftingResults)
            {
                Transform newItemView = Instantiate(ItemViewPrefab, CraftingMachineResultsContent);
                ItemView view = newItemView.GetComponent<ItemView>();

                InventorySlot slot = new InventorySlot();
                for(int i = 0; i < ingredient.IngredientAmount; i++)
                    slot.Add(new Item(ingredient.IngredientDefinition));

                view.Slot = slot;
            }
        }

        private void ShowGeneralInventoryView()
        {
            //hide/show parts of UI
            GeneralInventoryViewPanel.gameObject.SetActive(true);
            CraftingMachineViewPanel.gameObject.SetActive(false);
            MinerViewPanel.gameObject.SetActive(false);

            //destroy old item views
            foreach(Transform child in GeneralContentPanel)
            {
                GameObject.Destroy(child.gameObject);
            }

            for(int i = 0; i < inventory.SlotsAmount; i++)
            {
                Transform newItemView = Instantiate(ItemViewPrefab, GeneralContentPanel);
                ItemView view = newItemView.GetComponent<ItemView>();

                InventorySlot slot = (i >= inventory.Slots.Count) ? null : inventory.Slots[i];
                view.Slot = slot;
            }
        }

        private void ShowMinerView()
        {
            Miner miner = (Miner) machine;

            //hide/show parts of UI
            MinerViewPanel.gameObject.SetActive(true);
            GeneralInventoryViewPanel.gameObject.SetActive(false);
            CraftingMachineViewPanel.gameObject.SetActive(false);

            //show miner stored items
            foreach(Transform child in MinerStoredItemsContent)
            {
                GameObject.Destroy(child.gameObject);
            }

            for(int i = 0; i < inventory.SlotsAmount; i++)
            {
                Transform newItemView = Instantiate(ItemViewPrefab, MinerStoredItemsContent);
                ItemView view = newItemView.GetComponent<ItemView>();

                InventorySlot slot = (i >= inventory.Slots.Count) ? null : inventory.Slots[i];
                view.Slot = slot;
            }

            //show whats miner digging currently
            foreach(Transform child in MinerMiningItemsContent)
            {
                GameObject.Destroy(child.gameObject);
            }

            Transform miningItemView = Instantiate(ItemViewPrefab, MinerMiningItemsContent);
            if(miner.ConnectedOre != null)
            {
                ItemView view = miningItemView.GetComponent<ItemView>();
                InventorySlot slot = new InventorySlot();

                for(int i = 0; i < miner.ItemsPerSecond; i++)
                    slot.Add(new Item(miner.ConnectedOre.OreItem));

                view.Slot = slot;
            }
        }

        /// <summary>
        /// Hide inventory UI
        /// </summary>
        public void Hide()
        {
            transform.gameObject.SetActive(false);

            GeneralInventoryViewPanel.gameObject.SetActive(false);
            CraftingMachineViewPanel.gameObject.SetActive(false);
            MinerViewPanel.gameObject.SetActive(false);
        }
    }
}
