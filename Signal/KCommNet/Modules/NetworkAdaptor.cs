using System;

namespace KERBALISM
{
  // Original code by TaxiService from https://github.com/KSP-TaxiService/CommNetConstellation
  public class NetworkAdaptor : PartModule
  {
    [KSPField(isPersistant = true)] public short frequency = 0;
    [KSPField] public double ecCost;                            // cost of transmission in EC/s
    [KSPField] public double rate;                              // transmission rate at zero distance in Mb/s
    public Guid Target = Guid.Empty;                            //TODO: implement targeting

    // This module is always existing in part that has ModuleDataTransmitter
    ModuleDataTransmitter transmitter;

    [KSPField(guiName = "Antenna Power", guiUnits = "", guiActive = false, guiFormat = "")]
    string power = "";

    [KSPField(guiName = "Bandwidth", guiUnits = "", guiActive = false, guiFormat = "")]
    string Rate;

    public override void OnStart(StartState state)
    {
      if (state == StartState.Editor || state == StartState.None || state == StartState.PreLaunch) return;

      transmitter = part.FindModuleImplementing<ModuleDataTransmitter>();

      if (transmitter != null)
      {
        // add event to update cache when antenna be extended or retracted
        ModuleDeployableAntenna deployMod = part.FindModuleImplementing<ModuleDeployableAntenna>();
        if (deployMod != null)
        {
          deployMod.OnStop.Add(OnAntennaDeployment);
        }

        // I hide it because it don't show the right information (should be antenna_power * rangeModifier)
        transmitter.Fields["powerText"].guiActive = false;
        transmitter.Fields["statusText"].guiActive = false;
        transmitter.Events["StartTransmission"].active = false;
        transmitter.Events["TransmitIncompleteToggle"].active = false;

        // Show transmissiter rate
        if (transmitter.antennaType != AntennaType.INTERNAL)
        {
          Rate = Lib.HumanReadableDataRate(rate);
          power = KSPUtil.PrintSI(transmitter.antennaPower * Antenna_Info.rangeModifier, string.Empty, 3, false);
          Fields["Rate"].guiActive = true;
          Fields["power"].guiActive = true;
        }
      }
      base.OnStart(state);
    }

    private void OnAntennaDeployment(float data)
    {
      Lib.Debug("Antenna in active CommNet Vessel '{0}' is extended/retracted. Rebuilding the freq list ...", vessel.vesselName);
      // update antenna cache
      Cache.AntennaInfo(vessel).isTimeToUpdate = true;
    }

  }
}