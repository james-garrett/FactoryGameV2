using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FactoryBuilderTemplate
{
    /// <summary>
    /// Struct used to store prefab and it's offset (offset is used when using position snapping while building)
    /// </summary>
    [Serializable]
    public struct PlaceablePrefab
    {
        public GameObject Prefab;
        public Vector3 SnapPositionOffset;
    }

    /// <summary>
    /// Player tools script used to give first person player basic options to place belts and machines in the scene
    /// </summary>
    public class PlayerTools : MonoBehaviour
    {
        //Player movement controller
        public FPController FpController;

        //Machine building variables
        [Header("Building")]
        [Tooltip("List of all possible to place by player machines prefabs.")]
        public List<PlaceablePrefab> Prefabs;

        private int currentPrefabIndex = 0;
        private int lastPrefabIndex = 1;
        private float prefabRotY = 0;
        private float lastPrefabRotY = 0;

        private GameObject prefabPreview;
        private int layerBeforeSpawn;

        //Belt placing variables
        [Header("Belt placing")]
        [Tooltip("A reference to text component used to inform the player what to do next (place belt, select next point, etc) can be null")]
        public Text InfoText;
        [Tooltip("A reference to text component used to inform what is current building mode can be null")]
        public Text InfoText2;
        [Tooltip("Prefab of the conveyor belt machine")]
        public Transform BeltPrefab;
        [Tooltip("A reference to belt renderer used as conveyor belt preview renderer")]
        public LineRenderer BeltPreviewRenderer;

        private List<Vector3> beltPoints;
        private bool beltStartSelected = false;

        private MachineInput beltInput;
        private MachineOutput beltOutput;

        public bool buildingBeltMode = false;
        private RaycastHit rayHit;
        private bool hit;

        private bool inventoryShown = false;

        //strings shown to inform how to use player tools
        private const string BELT_BUILDING_MODE = "Belt building (press C to change mode)";
        private const string BUILDING_MODE = "Building mode (press C to change mode, R to remove object)";

        private const string BUILDING_LEFT_CLICK_TO_CHANGE_MACHINE = " (left mouse click to place, right to change machine, shift snap position)";

        private const string SELECT_BELT_START_TEXT = "Select start of belt (left click on machine output)";
        private const string SELECT_BELT_END = "Select end of belt or add segment. (left click on ground or machine input, shift snap pos, ESC to cancel)";
        private const string SELECT_BELT_MACHINE_NOT_FOUND = "Machine not found! Machine not setup properly?";

        //Building modes
        enum Option
        {
            BeltBuilding, Placing
        }
        private Option currentOption = Option.BeltBuilding;
        private int currentOptionIndex = 0;

        void Start()
        {
            if (InfoText)
                InfoText.text = SELECT_BELT_START_TEXT;

            beltPoints = new List<Vector3>();
            FpController.ShowCursor();
        }

        private void ConstructBelt()
        {
            Transform conveyorBelt = Instantiate(BeltPrefab);
            conveyorBelt.GetComponent<ConveyorBelt>().SetupBelt(beltPoints.ToArray(), beltInput, beltOutput);
        }

        void Update()
        {
            //Update currently selected building mode
            if (InfoText2)
                InfoText2.text = currentOption.ToString();

            if (currentOption == Option.BeltBuilding)
            {
                if (InfoText2)
                    InfoText2.text = BELT_BUILDING_MODE;

                BeltBuildingUpdate();

                if (prefabPreview != null)
                {
                    Destroy(prefabPreview);
                    prefabPreview = null;
                    lastPrefabIndex = -1;
                }
            }
            else if (currentOption == Option.Placing)
            {
                if (InfoText2)
                    InfoText2.text = BUILDING_MODE;

                PlacingUpdate();
            }

            //Check if player want to change current mode
            if (Input.GetKeyDown(KeyCode.C))
            {
                currentOptionIndex++;
                currentOptionIndex %= Enum.GetNames(typeof(Option)).Length;
                currentOption = (Option)currentOptionIndex;

                //update info text after option change
                if (InfoText)
                    InfoText.text = SELECT_BELT_START_TEXT;
            }

            //show/hide inventory of game object player is pointing at if possible
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (!inventoryShown)
                {
                    // var ray = Camera.main.ViewportPointToRay(new Vector3(0.5F, 0.5F, 0));
                    var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out rayHit, 100.0f))
                    {
                        Inventory inventory = rayHit.transform.GetComponent<Inventory>();
                        Machine machine = rayHit.transform.GetComponent<Machine>();

                        if (inventory != null)
                        {
                            InventoryView.Instance.Show(machine, inventory);
                            inventoryShown = true;
                            // FpController.ShowCursor();
                        }
                    }
                }
                else
                {
                    // FpController.HideCursor();
                    InventoryView.Instance.Hide();
                    inventoryShown = false;
                }
            }
        }

        private void PlacingUpdate()
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {

                //rotate machine preview
                if (Input.GetAxis("Mouse ScrollWheel") > 0f)
                {
                    prefabRotY += 45;
                }
                else if (Input.GetAxis("Mouse ScrollWheel") < 0f)
                {
                    prefabRotY -= 45;
                }
            }

            //scroll through possible machines to place using mouse wheel
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                currentPrefabIndex++;
                currentPrefabIndex %= Prefabs.Count;
            }

            //remove objects when pressing R
            if (Input.GetKeyDown(KeyCode.R))
            {
                var ray1 = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray1, out rayHit))
                {
                    Machine machine = rayHit.transform.GetComponent<Machine>();
                    ConveyorBelt belt = rayHit.transform.GetComponent<ConveyorBelt>();
                    if (machine != null || belt != null)
                    {
                        Destroy(rayHit.transform.gameObject);
                    }
                }
            }

            //spawn new prefab preview if needed
            if (currentPrefabIndex != lastPrefabIndex)
            {
                //remove old one
                if (prefabPreview)
                    Destroy(prefabPreview);

                if (InfoText)
                    InfoText.text = Prefabs[currentPrefabIndex].Prefab.name + BUILDING_LEFT_CLICK_TO_CHANGE_MACHINE;

                prefabPreview = Instantiate(Prefabs[currentPrefabIndex].Prefab);

                layerBeforeSpawn = prefabPreview.layer;
                prefabPreview.layer = LayerMask.NameToLayer("Ignore Raycast");

                //disable prefab collisions
                List<Collider> prefabColliders = new List<Collider>();
                prefabColliders.AddRange(prefabPreview.GetComponentsInChildren<Collider>());
                prefabColliders.AddRange(prefabPreview.GetComponents<Collider>());
                foreach (Collider c in prefabColliders)
                    c.enabled = false;
            }
            lastPrefabIndex = currentPrefabIndex;

            //set prefab position to where user is looking
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out rayHit))
            {
                prefabPreview.transform.position = RoundPosition(rayHit.point, Prefabs[currentPrefabIndex].SnapPositionOffset);
                prefabPreview.transform.RotateAround(prefabPreview.transform.position, Vector3.up, lastPrefabRotY - prefabRotY);
            }


            lastPrefabRotY = prefabRotY;

            if (Input.GetMouseButtonDown(0))
            {
                prefabPreview.layer = layerBeforeSpawn;

                //reenable collisions
                List<Collider> prefabColliders = new List<Collider>();
                prefabColliders.AddRange(prefabPreview.GetComponentsInChildren<Collider>());
                prefabColliders.AddRange(prefabPreview.GetComponents<Collider>());
                foreach (Collider c in prefabColliders)
                    c.enabled = true;

                prefabPreview = null;
                lastPrefabIndex = -1; //force to spawn preview again
            }
        }

        /// <summary>
        /// Round position to grid to achieve organized objects placing when player is holding left shift
        /// </summary>
        /// <param name="worldPos">Position to round</param>
        /// <returns>Rounded position if left shift pressed</returns>
        private Vector3 RoundPosition(Vector3 worldPos, Vector3 snapOffset)
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                return new Vector3(Mathf.Round(worldPos.x - snapOffset.x) + snapOffset.x, worldPos.y + snapOffset.y, Mathf.Round(worldPos.z - snapOffset.z) + snapOffset.z);
            }
            return worldPos;
        }

        private Vector3 RoundPositionForBelt(Vector3 worldPos)
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                return new Vector3(((int)(worldPos.x / 0.5f)) * 0.5f, worldPos.y + 0.5f, ((int)(worldPos.z / 0.5f)) * 0.5f);
            }
            return worldPos + new Vector3(0, 0.5f, 0);
        }

        private void BeltBuildingUpdate()
        {
            if (buildingBeltMode)
            {
                //exit building belt mode if requested
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    buildingBeltMode = false;
                    beltPoints.Clear();
                    beltStartSelected = false;
                    BeltPreviewRenderer.enabled = false;

                    if (InfoText)
                        InfoText.text = SELECT_BELT_START_TEXT;

                    return;
                }

                //raycast all the time and render preview
                // var ray = Camera.main.ViewportPointToRay(new Vector3(0.5F, 0.5F, 0));
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                hit = Physics.Raycast(ray, out rayHit);

                if (hit)
                {
                    //draw placing belt preview based on current pointing position and previous points
                    if (beltPoints.Count > 0)
                    {
                        BeltPreviewRenderer.enabled = true;
                        Vector3 lastPoint = beltPoints[beltPoints.Count - 1];

                        Vector3 currPoint = RoundPositionForBelt(rayHit.point);

                        //snap preview if hit obj is input node
                        if (rayHit.transform.GetComponent<MachineInput>() != null)
                            currPoint = rayHit.collider.gameObject.transform.position;

                        beltPoints.Add(currPoint);
                        BeltPreviewRenderer.positionCount = beltPoints.Count;
                        BeltPreviewRenderer.SetPositions(beltPoints.ToArray());
                        beltPoints.Remove(currPoint);
                    }
                }
                else
                {
                    BeltPreviewRenderer.enabled = false;
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                //start belt building mode if not started yet
                if (!buildingBeltMode)
                {
                    //raycast all the time and render preview
                    var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    hit = Physics.Raycast(ray, out rayHit);

                    buildingBeltMode = true;
                }

                if (buildingBeltMode)
                {
                    if (hit)
                    {
                        //if hit target was end of belt     
                        if (rayHit.transform.GetComponent<MachineInput>() != null && beltStartSelected)
                        {
                            Machine machine = rayHit.transform.gameObject.GetComponentInParent<Machine>();
                            if (machine)
                            {
                                beltPoints.Add(rayHit.collider.gameObject.transform.position);

                                beltStartSelected = false;
                                beltInput = rayHit.transform.GetComponent<MachineInput>();

                                if (InfoText)
                                    InfoText.text = SELECT_BELT_START_TEXT;

                                ConstructBelt();

                                //exit building mode
                                beltPoints.Clear();
                                buildingBeltMode = false;
                                BeltPreviewRenderer.enabled = false;
                            }
                        }
                        //if hit target was start of belt
                        else if (rayHit.transform.GetComponent<MachineOutput>() != null && !beltStartSelected)
                        {
                            Machine machine = rayHit.transform.gameObject.GetComponentInParent<Machine>();
                            if (machine)
                            {
                                beltPoints.Add(rayHit.collider.gameObject.transform.position);

                                if (InfoText)
                                    InfoText.text = SELECT_BELT_END;

                                beltStartSelected = true;
                                beltOutput = rayHit.transform.GetComponent<MachineOutput>();
                            }
                            else
                            {
                                if (InfoText)
                                    InfoText.text = SELECT_BELT_MACHINE_NOT_FOUND;
                            }
                        }

                        else if ((rayHit.transform.GetComponent<Machine>() != null) &&
                        (rayHit.transform.GetComponent<ConveyorBelt>() == null))
                        {
                            Machine machine = rayHit.transform.gameObject.GetComponentInParent<Machine>();
                            if (machine)
                            {
                                if ((machine.InputOutputHub.Outputs().Count > 0) && !beltStartSelected)
                                {
                                    foreach (MachineOutput output in machine.InputOutputHub.Outputs())
                                    {
                                        if (output.ConnectedTo == null)
                                        {
                                            beltOutput = output;
                                        }
                                    }
                                    if (beltOutput != null)
                                    {
                                        beltPoints.Add(beltOutput.transform.position);
                                        beltStartSelected = true;
                                        if (InfoText)
                                            InfoText.text = SELECT_BELT_END;
                                    }
                                }
                                else if ((machine.InputOutputHub.Inputs().Count > 0))
                                {
                                    //foreach (MachineInput input in machine.Inputs)
                                    //{
                                    //    if (input.ConnectedTo == null)
                                    //    {
                                    //        beltInput = input;
                                    //    }
                                    //}
                                    beltInput = machine.InputOutputHub.Inputs().Find(x => x.name == "Input1");
                                    if (beltInput != null)
                                    {
                                        beltPoints.Add(beltInput.transform.position);
                                        ConstructBelt();

                                        //exit building mode
                                        beltPoints.Clear();
                                        buildingBeltMode = false;
                                        BeltPreviewRenderer.enabled = false;
                                        if (InfoText)
                                            InfoText.text = SELECT_BELT_START_TEXT;
                                    }

                                }
                            }
                            else
                            {
                                if (InfoText)
                                    InfoText.text = SELECT_BELT_MACHINE_NOT_FOUND;
                            }
                        }

                        //if hit target is just another belt segment
                        else if (beltStartSelected)
                        {
                            Vector3 newPoint = RoundPositionForBelt(rayHit.point);
                            beltPoints.Add(newPoint);
                        }
                    }
                }
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    BeltPreviewRenderer.enabled = false;
                    beltPoints.Clear();
                    beltStartSelected = false;
                }
            }
        
        }

        private bool GetAvailableOutlet<T>(Machine machine, T[] AvailableOutlets, out T outlet, RaycastHit rayHit)
        {
            if(AvailableOutlets.Length > 0 )
            {
                T closestOutlet = AvailableOutlets[0];
                if (AvailableOutlets.Length > 1)
                {
                    closestOutlet = GetClosestInputToUserClick(AvailableOutlets, rayHit);

                }
                outlet = closestOutlet;
                return true;
            }
            outlet = default(T);
            return false;
        }

        private T GetClosestInputToUserClick<T>(T[] AvailableOutlets, RaycastHit rayHit)
        {
            T closestOutlet = default(T);
            Vector3 hitPosition = rayHit.transform.position;
            float distanceFromClick = 0;
            foreach (T currentOutlet in AvailableOutlets)
            {
                float currentInputdistanceFromClick = Vector3.Distance((currentOutlet as GameObject).transform.position, hitPosition);
                if (currentInputdistanceFromClick < distanceFromClick)
                { 
                    closestOutlet = currentOutlet;
                }

            }
            return closestOutlet;
        }

        private bool CompareLayers(LayerMask mask, int layer)
        {
            return (mask & 1 << layer) == 1 << layer;
        }
    }
}
