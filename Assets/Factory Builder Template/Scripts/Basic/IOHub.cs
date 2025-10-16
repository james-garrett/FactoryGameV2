using FactoryBuilderTemplate;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IOHub:MonoBehaviour

    //In the future this might be the interior container for buildings
{
    public List<MachineInput> Inputs = new List<MachineInput>();
    public List<MachineOutput> Outputs = new List<MachineOutput>();

    public MachineInput inputTemplate;
    public MachineOutput outputTemplate;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //Todo - Create method which uses template IO to create inputs, outputs and return a reference to them
    public static MachineInput CreateInput()
    {
        MachineInput newInput = new MachineInput();
        return newInput;
    }

    //Creates new Input or Output based on T declaration
    public static List<MachineInput> CreateManyInput(int count, Machine parent)
    {
        List<MachineInput> newIOList = new List<MachineInput>();
        for (int i = 0; i < count; i++)
        {
            MachineInput newIO = new MachineInput();
            newIO.Parent = parent;
            newIOList.Add(newIO);
        }
        return newIOList;
    }

    public static List<MachineOutput> CreateManyOutput(int count, Machine parent)
    {
        List<MachineOutput> newIOList = new List<MachineOutput>();
        for (int i = 0; i < count; i++)
        {
            MachineOutput newIO = new MachineOutput();
            newIO.Parent = parent;
            newIOList.Add(newIO);
        }
        return newIOList;
    }
}
