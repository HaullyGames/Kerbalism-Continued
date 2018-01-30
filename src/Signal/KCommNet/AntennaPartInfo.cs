using System;

namespace KERBALISM
{
  // Information for each antenna
  public class AntennaPartInfo
  {
    // NetworkAdaptor info
    public short frequency;
    public double ecCost;
    public double rate;
    public string name;
    public double antennaPower;
    public double antennaCombinableExponent;
    public bool antennaCombinable;
    public AntennaType antennaType;
    public ModuleDeployablePart.DeployState deployState;
    public bool canComm;
    public Guid Target;
  }
}