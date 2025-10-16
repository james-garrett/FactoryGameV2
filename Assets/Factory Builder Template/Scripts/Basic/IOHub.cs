using FactoryBuilderTemplate;
using System.Collections.Generic;

public class IOHub

    //In the future this might be the interior container for buildings
{
    public List<MachineInput> inputs;
    public List<MachineOutput> outputs;

    public MachineInput inputTemplate;
    public MachineOutput outputTemplate;

    public IOHub()
    {
        this.inputs = new List<MachineInput>();
        this.outputs = new List<MachineOutput>();
    }

    public IOHub(List<MachineInput> newInputs, List<MachineOutput> newOutputs)
    {
        this.inputs = newInputs;
        this.outputs = newOutputs;
    }

    public List<MachineInput> Inputs()
    {
        return this.inputs;
    }
    public List<MachineOutput> Outputs()
    {
        return this.outputs;
    }

    public void SetInputs(List<MachineInput> newInputs)
    {
        this.inputs = newInputs;
    }
    public void SetOutputs(List<MachineOutput> newOutputs)
    {
        this.outputs = newOutputs;
    }
}
