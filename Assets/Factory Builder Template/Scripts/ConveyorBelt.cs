using FactoryBuilderTemplate;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ConveyorBelt : Machine, ISaveable
{
    public float speed;
    public Vector3 direction;
    public List<Tuple<float, GameObject>> onBelt;
    public float validTimeMarginForDespawn;

    [Header("Machine IO")]
    public MachineInput Input;
    public MachineOutput Output;

    // Update is called once per frame
    void Update()
    {
        if (direction == Vector3.zero &&
            Output.ConnectedTo != null &&
            Output.ConnectedTo.Parent != null &&
            Output.ConnectedTo.Parent.transform.position != null)
        {
            CalibrateBeltDirection();
        }

        //if(onBelt != null && onBelt.Count > 0)
        //{
        //    for (int i = 0; i <= onBelt.Count - 1; i++)
        //    {
        //        Tuple<float, GameObject> currentBeltItem = onBelt[i];
        //        if (currentBeltItem.Item2 == null)
        //        {
        //            onBelt.Remove(currentBeltItem);
        //        } else 
        //        {
        //            currentBeltItem.Item2.GetComponent<Rigidbody>().velocity = speed * direction * Time.deltaTime;
        //        }
        //    }
        //}
    }

    private void CalibrateBeltDirection()
    {
        direction = (Output.ConnectedTo.Parent.transform.position - transform.position).normalized;
        Debug.DrawLine(transform.position, transform.position + direction * 10, Color.red, Mathf.Infinity);
    }

    // When something collides with the belt
    //public void OnCollisionEnter(Collision collision)
    //{
    //    onBelt.Add(new Tuple<float, GameObject>(Time.deltaTime, collision.gameObject));
    //}

    //public void OnCollisionExit(Collision collision)
    //{
    //    GameObject collisionObject = collision.gameObject;
    //    Tuple<float, GameObject> objectOnBelt = onBelt.Find(x => x.Item2.GetInstanceID() == collisionObject.GetInstanceID());
    //    if(isObjectValidForDespawning(objectOnBelt))
    //    {
    //        onBelt.Remove(objectOnBelt);
    //    }
    //}

    public bool isObjectValidForDespawning(Tuple<float, GameObject> objectOnBelt)
    {
        float timeSinceContactOccured = Time.deltaTime - objectOnBelt.Item1;
        return timeSinceContactOccured > validTimeMarginForDespawn;
    }

    [Header("Belt parameters")]
    public float ItemsSpeed = 10;

    //line renderer representing conveyor belt
    private LineRenderer lineRenderer;

    [System.Serializable]
    private class ItemContainer
    {
        public Item item;
        public float distanceTraveled = 0; //between 0-1

        /// <summary>
        /// Matrix used in instanced rendering
        /// </summary>
        public Matrix4x4 travellingItemMatrix;

        /// <summary>
        /// GameObject used in game object based rendering mode
        /// </summary>
        public GameObject representationOfItemTravelling;
        public bool despawnGO, spawnGO;

        /// <summary>
        /// Despawn item on belt
        /// </summary>
        /// <param name="belt">Belt instance</param>
        public void DespawnItemRepresentation(ConveyorBelt belt)
        {
            //if (item.ItemDefinition.RenderInstanced)
            //{
                //belt.DespawnItemRepresentationForInstancedRendering(this);
            //}
            //else
            //{
            //    despawnGO = true;
            //}
        }
    }

    //lists used to synchronize itemsOnBelt modifications on different thread
    private List<ItemContainer> itemsOnBelt;

    //instanced rendering variables
    private Dictionary<ItemDefinition, List<ItemContainer>> itemsMatrices;
    private Matrix4x4[] tempMatrices = new Matrix4x4[1024];

    //game object based rendering
    private Dictionary<ItemDefinition, List<GameObject>> itemsPool;

    //conveyor belt points in world space
    private Vector3[] pointsWorldspace;

    // Start is called before the first frame update
    new void Awake()
    {
        base.Awake();

        //grab references
        lineRenderer = GetComponent<LineRenderer>();

        onBelt = new List<Tuple<float, GameObject>>();

        //create lists
        itemsOnBelt = new List<ItemContainer>();
        itemsMatrices = new Dictionary<ItemDefinition, List<ItemContainer>>();

        itemsPool = new Dictionary<ItemDefinition, List<GameObject>>();

        //register machine IO
        InputOutputHub.Inputs = new List<MachineInput>();
        InputOutputHub.Outputs = new List<MachineOutput>();

        InputOutputHub.Inputs.Add(Input);
        InputOutputHub.Outputs.Add(Output);

    }

    void Start()
    {
        IsInitializated = true;
    }

    public void SetOutput(MachineInput input)
    {
        input.ConnectedTo = this.Output;
        this.Output.ConnectedTo = input;
    }
    public void SetInput(MachineOutput output)
    {
        output.ConnectedTo = this.Input;
        this.Input.ConnectedTo = output;
    }

    /// <summary>
    /// Method used by PlayerTools and save load system when conveyor belt is placed to setup its input, output, collider and items path
    /// </summary>
    /// <param name="points">Set of points in world space defining items path</param>
    /// <param name="input">input to which this conveyor belt is connected to</param>
    /// <param name="output">output to which this conveyor belt is connected to</param>
    public void SetupBelt(Vector3[] points, MachineInput input, MachineOutput output)
    {
        pointsWorldspace = points;

        //calculate pointsLocalspace and shift belt pivot to its center

        //Change pivot and transform positions from world space to local space to make collider working 
        Vector3[] pointsLocalspace = new Vector3[points.Length];
        for (int i = 0; i < points.Length; i++)
            pointsLocalspace[i] = new Vector3(points[i].x, points[i].y, points[i].z);

        //calculate new pivot point
        Vector3 totalPosition = Vector3.zero;
        foreach (Vector3 pos in pointsLocalspace)
            totalPosition += pos;

        Vector3 center = totalPosition / pointsLocalspace.Length; //world space

        //offset all positions by new pivot and go from world space to local space
        for (int i = 0; i < pointsLocalspace.Length; i++)
        {
            pointsLocalspace[i] = pointsLocalspace[i] - center;
            pointsLocalspace[i] = lineRenderer.transform.InverseTransformPoint(pointsLocalspace[i]);
        }

        //Move the transform to where the center of the line was so it looks like the line didn't move
        transform.position = center;

        //have to move from world space to local space to make mesh baking work properly for some reason (otherwise rotation breaks orientation of collider)
        lineRenderer.useWorldSpace = false;
        lineRenderer.positionCount = pointsLocalspace.Length;
        lineRenderer.SetPositions(pointsLocalspace);

        //CreateItemsPath();

        CreateMeshCollider();

        //connect belt and machines inputs & outputs 
        if (input != null)
            SetOutput(input);

        if (output != null)
            SetInput(output);
    }

    public string Save()
    {
        ConveyorBeltData data = new ConveyorBeltData();
        data.ItemsSpeed = ItemsSpeed;
        data.BeltPointsWorldspace = pointsWorldspace;

        //save where output is connected to
        if (InputOutputHub.Outputs[0].ConnectedTo != null)
        {
            data.OutputConnectedToMachineInputID = InputOutputHub.Outputs[0].ConnectedTo.Parent.GetMachineID();
            data.OutputConnectedToInputGameObjectName = InputOutputHub.Outputs[0].ConnectedTo.name;
        }

        data.ItemContainersList = itemsOnBelt;

        return JsonUtility.ToJson(data);
    }

    public bool Load(string data)
    {
        transform.position = Vector3.zero;

        ConveyorBeltData beltData = JsonUtility.FromJson<ConveyorBeltData>(data);
        pointsWorldspace = beltData.BeltPointsWorldspace;
        ItemsSpeed = beltData.ItemsSpeed;

        //remove old items on belt
        foreach (ItemContainer container in itemsOnBelt)
        {
            //if (container.item.ItemDefinition.RenderInstanced)
            //{
                //DespawnItemRepresentationForInstancedRendering(container);
            //}
            //else
            //{
                //DespawnItemRepresentationForGORendering(container);
            //}
        }
        itemsOnBelt.Clear();

        //restore items on belt list from save 
        itemsOnBelt = beltData.ItemContainersList;

        foreach (ItemContainer container in itemsOnBelt)
        {
            //if (container.item.ItemDefinition.RenderInstanced)
            //{
                //SpawnItemRepresentationForInstancedRendering(container);
            //}
            //else
            //{
                //container.representationOfItemTravelling = SpawnItemRepresentationForGORendering(container.item);
            //}
        }

        //find where belt is connected 
        MachineInput machineInput = TryToFindMachineInput(beltData.OutputConnectedToMachineInputID, beltData.OutputConnectedToInputGameObjectName);

        //setup line renderer, input and collider based on loaded data
        SetupBelt(pointsWorldspace, machineInput, null);
        //update containers positions
        //foreach (ItemContainer container in itemsOnBelt)
        //    UpdateContainerTransform(container);

        return true;
    }

    /// <summary>
    /// Create mesh collider for line renderer
    /// </summary>
    private void CreateMeshCollider()
    {
        MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>();
        if(meshCollider == null)
            meshCollider = gameObject.AddComponent<MeshCollider>();

        Mesh mesh = new Mesh();
        lineRenderer.BakeMesh(mesh, false);
        meshCollider.sharedMesh = mesh;
    }

    /// <summary>
    /// Class used to store this machine data used for serialization/deserialization
    /// </summary> 
    [System.Serializable]
    private class ConveyorBeltData
    {
        public float ItemsSpeed;
        public Vector3[] BeltPointsWorldspace;

        public int OutputConnectedToMachineInputID;
        public string OutputConnectedToInputGameObjectName;

        public List<ItemContainer> ItemContainersList;
    }
}
