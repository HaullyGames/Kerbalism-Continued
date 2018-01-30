namespace KERBALISM
{
  public abstract class NetDevice
  {
    // return device name
    public abstract string Name();

    // return part id
    public abstract uint Part();

    // return short device status string
    public abstract short InfoFreq();

    public abstract string InfoRate();

    // control the device using a value
    public abstract void ChangeFreq(short value);

    // generate unique id for the module
    // - multiple same-type components in the same part will have the same id
    public uint Id() { return Part() + (uint)Name().GetHashCode(); }
  }

  public sealed class NetAdaptorDevice : NetDevice
  {
    public NetAdaptorDevice(NetworkAdaptor networkAdap)
    {
      this.networkAdap = networkAdap;
    }

    public override string Name()
    {
      return networkAdap.part.partInfo.title;
    }

    public override uint Part()
    {
      return networkAdap.part.flightID;
    }

    public override short InfoFreq()
    {
      return networkAdap.frequency;
    }

    public override string InfoRate()
    {
      return Lib.HumanReadableDataRate(networkAdap.rate);
    }

    public override void ChangeFreq(short value)
    {
      networkAdap.frequency += value;
      Cache.AntennaInfo(networkAdap.part.vessel).isTimeToUpdate = true;
    }

    NetworkAdaptor networkAdap;
  }

  public sealed class ProtoNetAdaptorDevice : NetDevice
  {
    public ProtoNetAdaptorDevice(ProtoPartModuleSnapshot networkAdap, uint part_id, Vessel v)
    {
      this.networkAdap = networkAdap;
      this.part_id = part_id;
      this.v = v;
    }

    public override string Name()
    {
      //return v.protoVessel.protoPartSnapshots[(int)part_id].partInfo.title;
      return "protoAntenna";
    }

    public override uint Part()
    {
      return part_id;
    }

    public override short InfoFreq()
    {
      return Lib.Proto.GetShort(networkAdap, "frequency");
    }

    public override string InfoRate()
    {
      return Lib.HumanReadableDataRate(Lib.Proto.GetShort(networkAdap, "rate"));
    }

    public override void ChangeFreq(short value)
    {
      short freq = Lib.Proto.GetShort(networkAdap, "frequency");
      freq += value;
      Lib.Proto.Set(networkAdap, "frequency", freq);
      Cache.AntennaInfo(v).isTimeToUpdate = true;
    }

    ProtoPartModuleSnapshot networkAdap;
    Vessel v;
    uint part_id;
  }
}
