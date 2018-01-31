namespace KERBALISM
{
  public abstract class ModuleECBase
  {
    [KSPField(isPersistant = true)] public double ecCost = 0;                 // ecCost to keep the part active
    [KSPField(isPersistant = true)] public double ecDeploy = 0;               // ecCost to do a deploy(animation)
  }

  public abstract class ECDevice
  {
    public double actualECCost = 0;

    public abstract string Name();

    public abstract uint Part();

    public abstract void Update();

    public abstract bool IsConsuming { get; }

    public void ToggleActions(PartModule partModule, bool value)
    {
      foreach (BaseAction ac in partModule.Actions)
      {
        ac.active = value;
      }
    }

    // generate unique id for the module
    // - multiple same-type components in the same part will have the same id
    public uint Id() { return Part() + (uint)Name().GetHashCode(); }
  }
}
