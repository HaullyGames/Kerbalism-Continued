using System;
using System.Collections.Generic;
using CommNet;

// Original code by TaxiService from https://github.com/KSP-TaxiService/CommNetConstellation
namespace KERBALISM
{
  public class KCommNetVessel : CommNetVessel
  {
    // Player will be able to define what is the best connection ( Strongest_Signal Or Highest_Rate )
    protected short strongestFreq = -1;
    protected short highestRateFreq = -1;
    protected bool stageActivated = false;
    public List<short> freqList = new List<short>() { 0 };

    protected override void OnNetworkInitialized()
    {
      base.OnNetworkInitialized();
      try
      {
        GameEvents.onStageActivate.Add(StageActivate);
        GameEvents.onVesselWasModified.Add(VesselModified);
      }
      catch (Exception e)
      {
        Lib.Error("Vessel '{0}' doesn't have any CommNet capability, likely a mislabelled junk or a kerbin on EVA", Vessel.GetName());
        Lib.Error("'{0}'", e.Message);
      }
    }

    protected override void OnDestroy()
    {
      base.OnDestroy();

      if (HighLogic.CurrentGame == null)
        return;

      GameEvents.onStageActivate.Remove(StageActivate);
      GameEvents.onVesselWasModified.Remove(VesselModified);
      
      Lib.Debug("Vessel '{0}' is destroyed.", Vessel.vesselName);
    }

    // GameEvent of staging a vessel
    private void StageActivate(int stageIndex)
    {
      if (Vessel.isActiveVessel)
      {
        stageActivated = true;
      }
    }

    // GameEvent of vessel being modified
    private void VesselModified(Vessel thisVessel)
    {
      if (Vessel.isActiveVessel && stageActivated) // decouple event
      {
        Lib.Debug("Active CommNet Vessel '{0}' is staged. Updating antenna cache...", thisVessel.vesselName);

        //force-update antenna cache
        Cache.AntennaInfo(thisVessel);

        stageActivated = false;
      }
    }
  }
}
