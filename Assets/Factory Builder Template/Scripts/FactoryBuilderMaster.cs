using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic; 
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FactoryBuilderTemplate
{
    /// <summary>
    /// Main Factory Builder Template class.
    /// Here references to all machines and recipes are stored.
    /// Also this class is responsible to update all machines on main or separate thread depends on settings.
    /// </summary> 
    [ExecuteInEditMode]
    public class FactoryBuilderMaster : MonoBehaviour
    {
        public static FactoryBuilderMaster Instance { get; private set; }

        [Header("Settings")]
        [Tooltip("How many MachineUpdate() will be called each second, use value lower than your frame rate to gain performance. If you use really high speed conveyor belts you might want to set it to your game fps.")]
        public int UpdatesPerSecond = 30;

        [Tooltip("Name of folder where all machines prefabs are stored, if this value is wrong then loading will stop working!")]
        public string MachinesPrefabsFolderName = "Machines";

        [Header("References To All Scriptable Objects")]
        [Tooltip("The place where all recipes are stored and can be accessed during runtime used for save load system and useful for making lists of for example all possible recipes etc. Each new created Recipe has to be added here! Changing the order of recipes will break save files")]
        public CraftingRecipe[] AllRecipes;
        [Tooltip("The place where all item definitions are stored. This array is used in inventory serialization, every new item has to be added here! Otherwise, the save load system will not work! Changing the order of items will break save files")]
        public ItemDefinition[] AllItemDefinitions;

        [Header("Read only")]
        public int MachinesAmount = 0;
        [Tooltip("How much time was spend on MachineUpdate() of all machines in the last second in milliseconds. Value 0 means scripts consumed less than 1 ms during an update.")]
        public long MachinesUpdateTimeInMs = 0;

        [Header("Debug")]
        [Tooltip("If set to true then update time consumed by MachineUpdate() of all machines will be measured and value UpdateTimeInMs will be updated every second.")]
        public bool MeasureUpdateTime = false;

        [Header("Experimental (might cause bugs)")]
        [Tooltip("Makes most of the factory-related logic run on a separate thread to give better performance. Do not change this value during runtime! Known bug: do not set UpdatesPerSecond to be higher than the current frame rate because items can start to clone")]
        public bool RunOnSeparateThread = false;

        private Thread thread;
        private bool IsRunning = true;

        private long timeSum;
        private int updateNum = 0;

        public static readonly ConcurrentQueue<Action> RunOnMainThread = new ConcurrentQueue<Action>();

        /**  
         * Snippet to run something on main thread
           FactoryBuilderMaster.RunOnMainThread.Enqueue(() => {
           ...
           });
         */

        private System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

        private void Awake()
        {
            Instance = this; 
        }
        
        void Update()
        {
            if(Application.isEditor && !Application.isPlaying)
                return;

            MachinesAmount = Machine.AllMachines.Count;

            if(!IsRunning && RunOnSeparateThread) //start thread on first update to make sure every machine Start() method was already called
                RunThread();

            if(!RunOnSeparateThread)
            {
                foreach(Machine machine in Machine.AllMachines.ToArray())
                    if(machine.IsInitializated)
                        machine.MachineUpdate();
            }

            //run enqueued tasks on unity main thread
            if(!RunOnMainThread.IsEmpty)
            {
                while(RunOnMainThread.TryDequeue(out var action))
                {
                    action?.Invoke();
                }
            }
        }

        /// <summary>
        /// Run separate thread for machine updates
        /// </summary>
        private void RunThread()
        {
            IsRunning = true;
            ThreadStart updateMethod = new ThreadStart(ThreadedMachineUpdate);
            thread = new Thread(updateMethod);
            thread.Priority = System.Threading.ThreadPriority.Lowest;
            thread.Start();
        }

        /// <summary>
        /// Machine update on separate thread update loop
        /// </summary>
        private void ThreadedMachineUpdate()
        {
            try
            {
                while(IsRunning)
                {
                    if(MeasureUpdateTime)
                    {
                        stopwatch.Reset();
                        stopwatch.Start();
                    }

                    foreach(Machine machine in Machine.AllMachines)
                        if(machine.IsInitializated)
                            machine.MachineUpdate();

                    if(MeasureUpdateTime)
                    {
                        stopwatch.Stop();
                        timeSum += stopwatch.ElapsedMilliseconds; 
                        updateNum++;

                        //second passed
                        if(updateNum % UpdatesPerSecond == 0)
                        {
                            updateNum = 0;
                            timeSum /= UpdatesPerSecond; //get avg update time per sec
                            MachinesUpdateTimeInMs = timeSum;
                            timeSum = 0;
                        }
                    }

                    Thread.Sleep((int) ((1f / UpdatesPerSecond) * 1000f));
                }
            } 
            catch(ThreadAbortException e)
            {
                IsRunning = false;
                Debug.LogException(e);
            }
        }

        private void OnDestroy()
        {
            IsRunning = false;
        }

        private void OnApplicationQuit()
        {
            IsRunning = false;
        }

        /// <summary>
        /// Returns list of all possible recipes that can be used with given crafting machine. Used by InventoryView.cs to get construct a list of selectable recipes when a player is trying to change the current recipe in the machine
        /// </summary>
        /// <param name="type">Type of crafting machine</param>
        /// <param name="inputsCount">Amount of inputs crafting machine has</param>
        /// <param name="outputsCount">Amount of outputs crafting machine has</param>
        /// <returns></returns>
        public List<CraftingRecipe> GetAllPossibleRecipesForGivenCraftingMachine(Machine.MachineType type, int inputsCount, int outputsCount)
        {
            List<CraftingRecipe> recipes = new List<CraftingRecipe>();
            foreach(CraftingRecipe recipe in AllRecipes)
            {
                if(recipe != null 
                    && recipe.Ingredients.Count <= inputsCount
                    && recipe.CraftingResults.Count <= outputsCount
                    && recipe.Type == type)
                {
                    recipes.Add(recipe);
                }
            }
            return recipes;
        }

        /// <summary>
        /// Get Machine instance in the current scene by its ID, used in save load system
        /// </summary>
        /// <param name="id">id of the machine</param>
        /// <returns>Instance of machine with given ID or null</returns>
        public Machine GetMachineByID(int id)
        {
            foreach(Machine machine in Machine.AllMachines)
            {
                if(machine.GetMachineID() == id)
                    return machine;
            }
            return null;
        }

        /// <summary>
        /// Save scene data into text file at user persistentDataPath
        /// </summary>
        /// <param name="saveFileName">name of the save file without extension</param>
        public void SaveToFile(string saveFileName)
        {
            string path = Application.persistentDataPath + "/" + saveFileName + ".json";
            System.IO.File.WriteAllText(path, Save());
            Debug.Log("Saved to: " + path);
        }

        /// <summary>
        /// Save current scene into string containing scene data in JSON format
        /// </summary>
        /// <returns>Scene data in JSON format</returns>
        public string Save()
        {
            var startTime = DateTime.Now;
            SceneData sceneData = new SceneData();
            sceneData.MachinesData = new List<MachineData>();

            foreach(Machine machine in Machine.AllMachines)
            {
                if(machine is ISaveable)
                {
                    if(machine.PrefabName == "")
                    {
                        Debug.LogError("Can't save machine with empty prefab name field because it will not be able to be loaded! " + machine.gameObject.name, machine);
                        continue;
                    }

                    MachineData machineData = new MachineData();
                      
                    machineData.Position = machine.transform.position;
                    machineData.Rotation = machine.transform.rotation;
                    machineData.MachineInternalData = ((ISaveable) machine).Save();
                    machineData.PrefabName = machine.PrefabName;
                    machineData.MachineID = machine.GetMachineID();

                    sceneData.MachinesData.Add(machineData);
                }
            }

            string sceneDataJSON = JsonUtility.ToJson(sceneData);
            Debug.Log("Serializing scene time: " + (DateTime.Now - startTime).Milliseconds + " ms");
            return sceneDataJSON;
        }

        /// <summary>
        /// Load scene from text file stored at user persistentDataPath
        /// </summary>
        /// <param name="saveFileName">Name of the save file without extension</param>
        public void LoadFromFile(string saveFileName)
        {
            string path = Application.persistentDataPath + "/" + saveFileName + ".json";
            string sceneDataJSON = System.IO.File.ReadAllText(path);
            Debug.Log("Loading from: " + path);
            StartCoroutine(Load(sceneDataJSON));
             
        }

        /// <summary>
        /// Load scene from JSON data, it has to be IEnumerator and started as coroutine (see how LoadFromFile works) because code have to wait 1 frame after placing prefabs in scene to make sure Start() methods in all new prefabs have been called
        /// </summary>
        /// <param name="sceneDataJSON">scene data as string, loaded from file, server etc</param>
        /// <param name="destroyMachinesNotInsideSavedData">if true then after loading scene machine that were not stored in loaded file will be destroyed</param>
        /// <returns>IEnumerator for coroutine</returns>
        public IEnumerator Load(string sceneDataJSON, bool destroyMachinesNotInsideSavedData = true)
        {
            var startTime = DateTime.Now;

            HashSet<int> loadedIds = new HashSet<int>();

            SceneData sceneData = JsonUtility.FromJson<SceneData>(sceneDataJSON);

            //Instantiate machines prefabs
            foreach(MachineData data in sceneData.MachinesData)
            {
                //check if machine with id we want to create already exists in the current scene
                Machine machine = GetMachineByID(data.MachineID);

                //if in current scene machine with given ID already exists but type does not match destroy it
                if(machine != null && machine.PrefabName != data.PrefabName)
                {
                    Destroy(machine.gameObject);
                    machine = null;
                }

                //if machine is null spawn new prefab
                if(machine == null)
                {
                    string prefabPath = MachinesPrefabsFolderName + "/" + data.PrefabName;
                    GameObject machinePrefab = (GameObject) Resources.Load(prefabPath, typeof(GameObject));
                    if(machinePrefab == null)
                    {
                        Debug.LogError("Failed to load prefab:" + data.PrefabName + " ! (check if it's located under correct path: " + prefabPath + ")");
                        continue;
                    }

                    GameObject machineInstance = Instantiate(machinePrefab);
                    machine = machineInstance.GetComponent<Machine>();
                }
                 
                //apply transform
                machine.transform.position = data.Position;
                machine.transform.rotation = data.Rotation;

                loadedIds.Add(data.MachineID);

                //check if ids match
                if(data.MachineID != machine.GetMachineID())
                {
                    Debug.LogError("While loading scene something went wrong and machine saved id and current one does not match! (" + data.MachineID + " vs " + machine.GetMachineID() + ") (method for generating ids changed?)");
                }
            }

            //wait for the next frame so Start() method in the newly placed prefabs will be called and they will initialize which is needed to load their internal state
            yield return 0;

            //load machines internal data
            foreach(MachineData data in sceneData.MachinesData)
            {
                Machine machine = GetMachineByID(data.MachineID);

                if(machine == null)
                {
                    Debug.LogError("Not able to load internal machine data because machine with given ID not found: " + data.MachineID);
                    continue;
                }

                //check if loading internal data succeeded
                if(!((ISaveable) machine).Load(data.MachineInternalData))
                {
                    Debug.LogError("Something went wrong while loading machine internal data! " + machine.gameObject.name);
                }
            }

            //remove machines that were not stored inside save file
            if(destroyMachinesNotInsideSavedData)
            {
                foreach(Machine machine in Machine.AllMachines)
                    if(!loadedIds.Contains(machine.GetMachineID()))
                        Destroy(machine.gameObject);
            }

            Debug.Log("De-serializing scene time: " + (DateTime.Now - startTime).Milliseconds + " ms");
        }

        /// <summary>
        /// Classes used for save load system used as data containers
        /// </summary>
        
        [System.Serializable]
        private class MachineData
        {
            public string PrefabName;
            public Vector3 Position;
            public Quaternion Rotation;
            public int MachineID;
            public string MachineInternalData;
        }

        [System.Serializable]
        private class SceneData
        {
            public List<MachineData> MachinesData; 
        }
    }
}
