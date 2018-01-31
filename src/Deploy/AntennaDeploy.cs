﻿namespace KERBALISM
{
  // This class will support two signal system (Kerbalism Signal & CommNet)
  public class AntennaDeploy : DeployBase
  {
    [KSPField(isPersistant = true)]
    double rightDistValue;              // Needs to support CommNet

    // This module will always exist in part that has Antenna or ModuleDataTransmitter module
    Antenna antenna;
    ModuleAnimationGroup customAnim;

    ModuleDataTransmitter transmitter;
    ModuleDeployableAntenna stockAnim;

    bool isTransmitting;                // Extra condition to IsConsuming
    bool isAnimation;                   // isAnimation (Extending/Retracting)

    public override void OnStart(StartState state)
    {
      // don't break tutorial scenarios
      if (Lib.DisableScenario(this)) return;

      // do nothing in the editors and when compiling parts
      if (!Lib.IsFlight()) return;

      // Kerbalism modules
      antenna = part.FindModuleImplementing<Antenna>();
      customAnim = part.FindModuleImplementing<ModuleAnimationGroup>();
      // KSP modules
      // I'm using this.dist to save transmitter.antennaPower.
      //  CommNet - transmitter.canComm() = (isdeploy || moduleisActive)
      //    When the transmitter has no deploy(is fixed), isdeploy= True, 
      //    Then the only way to disable the connection for this transmitter type is setting distance to 0 when no EC, forcing CommNet lost connection.
      //    When need enable back, take the information from this.dist
      transmitter = part.FindModuleImplementing<ModuleDataTransmitter>();
      stockAnim = part.FindModuleImplementing<ModuleDeployableAntenna>();

      if (Features.Signal)
      {
        if (customAnim != null) pModule = customAnim;
      }
      else if (Features.KCommNet)
      {
        if (stockAnim != null) pModule = stockAnim;

        // Show transmissiter rate
        if (transmitter.antennaType != AntennaType.INTERNAL)
        {
          Fields["actualECCost"].guiActive = true;
        }
      }
      base.OnStart(state);
    }

    public override void Update()
    {
      if (Lib.IsFlight() && Features.AdvancedEC)
      {
        // Check if it is transmitting
        if (!Features.Science && Features.Signal)
        {
          if (antenna != null) isTransmitting = antenna.stream.Transmitting();
        }
        else if(Features.Science)
        {
          // get info from the cache
          Vessel_Info vi = Cache.VesselInfo(vessel);
          // consume ec if data is transmitted or relayed
          if (vi == null) isTransmitting = false;
          else isTransmitting = (vi.transmitting.Length > 0 || vi.relaying.Length > 0);
        }
        base.Update();
        if (isTransmitting && isConsuming && !isAnimation)
        {
          if (Features.Signal) actualECCost = antenna.cost;
          //else if (Features.KCommNet)
          //{
          //  NetworkAdaptor adap = part.FindModuleImplementing<NetworkAdaptor>();
          //  if (adap != null)
          //  {
          //    actualECCost = adap.ecCost;
          //  }
          //  else actualECCost = 0;
          //}
          // Kerbalism already has logic to consume EC when it is transmitting
          isConsuming = false;
        }
      }
    }

    public override bool GetIsConsuming
    {
      get
      {
        isAnimation = false;

        if (Features.Signal)
        {
          if (hasEC)
          {
            if (customAnim != null)
            {
              ToggleActions(customAnim, hasEC);
              // Add cost to Extending/Retracting
              isAnimation = customAnim.DeployAnimation.isPlaying;
              if (isAnimation)
              {
                actualECCost = ecDeploy;
                return true;
              }
              else if (customAnim.isDeployed || !Settings.ExtendedAntenna)
              {
                customAnim.Events["RetractModule"].active = true;
                customAnim.Events["DeployModule"].active = false;
                customAnim.OnUpdate();

                // Makes antenna valid to AntennaInfo
                antenna.extended = true;
                actualECCost = ecCost;
                return true;
              }
              else
              {
                customAnim.Events["RetractModule"].active = false;
                customAnim.Events["DeployModule"].active = true;
                return false;
              }
            }
            else
            {
              // this means that antenna is fixed
              // Makes antenna valid to AntennaInfo
              antenna.extended = true;
              actualECCost = ecCost;
              return true;
            }
          }
          else
          {
            if (customAnim != null)
            {
              ToggleActions(customAnim, hasEC);
              // Don't allow extending/retracting when has no ec
              customAnim.Events["RetractModule"].active = false;
              customAnim.Events["DeployModule"].active = false;
            }
            // Makes antennaModule invalid to AntennaInfo
            antenna.extended = false;
            return false;
          }
        }
        else if (Features.KCommNet)
        {
          // Save antennaPower
          rightDistValue = (rightDistValue != transmitter.antennaPower && transmitter.antennaPower > 0 ? transmitter.antennaPower : rightDistValue);

          if (hasEC)
          {
            if (stockAnim != null)
            {
              ToggleActions(stockAnim, hasEC);
              // Add cost to Extending/Retracting
              if (stockAnim.deployState == ModuleDeployablePart.DeployState.RETRACTING || stockAnim.deployState == ModuleDeployablePart.DeployState.EXTENDING)
              {
                actualECCost = ecDeploy;
                isAnimation = true;
                return true;
              }
              else if (stockAnim.deployState == ModuleDeployablePart.DeployState.EXTENDED || !Settings.ExtendedAntenna)
              {
                stockAnim.Events["Retract"].active = true;
                stockAnim.Events["Extend"].active = false;

                // Recover antennaPower only if antenna is Extended
                transmitter.antennaPower = rightDistValue;

                actualECCost = ecCost;
                return true;
              }
              else
              {
                // antenna is retract
                stockAnim.Events["Retract"].active = false;
                stockAnim.Events["Extend"].active = true;
                if (Settings.ExtendedAntenna) transmitter.antennaPower = 0;
                return false;
              }
            }
            else
            {
              // Recover antennaPower for fixed antenna
              transmitter.antennaPower = rightDistValue;
              actualECCost = ecCost;
              return true;
            }
          }
          else
          {
            if (stockAnim != null)
            {
              ToggleActions(stockAnim, hasEC);
              // Don't allow extending/retracting when has no ec
              stockAnim.Events["Retract"].active = false;
              stockAnim.Events["Extend"].active = false;
            }
            // Change the range to 0, causing CommNet to lose the signal
            transmitter.antennaPower = 0;
            return false;
          }
        }
        else return false;
      }
    }

    public static void BackgroundUpdate(Vessel v, ProtoPartSnapshot p, ProtoPartModuleSnapshot antenna, Vessel_Info vi, Resource_Info ec, double elapsed_s)
    {
      if (Features.AdvancedEC)
      {
        bool isDeploy;
        bool has_ec = ec.amount > double.Epsilon;

        ProtoPartModuleSnapshot deployModule = p.FindModule("AntennaDeploy");
        ProtoPartModuleSnapshot anim;

        if(deployModule == null)
        {
          Lib.Debug("AntennaDeploy is null. Load vessel to try fix it");
          return;
        }

        // if it is transmitting, leave with Kerbalism
        if (Features.Science && (vi.transmitting.Length > 0 || vi.relaying.Length > 0)) return;

        if (has_ec)
        {
          if (Features.Signal)
          {
            anim = p.FindModule("ModuleAnimationGroup");
            if (anim != null) isDeploy = Lib.Proto.GetBool(anim, "isDeployed");
            else isDeploy = true;

            if (!Settings.ExtendedAntenna || isDeploy)
            {
              Lib.Proto.Set(antenna, "extended", true);
              ec.Consume(Lib.Proto.GetDouble(deployModule, "ecCost") * elapsed_s);
            }
          }
          else if (Features.KCommNet)
          {
            anim = p.FindModule("ModuleDeployableAntenna");
            if (anim != null) isDeploy = Lib.Proto.GetString(anim, "deployState") == "EXTENDED";
            else isDeploy = true;

            if (isDeploy)
            {
              Lib.Proto.Set(antenna, "canComm", true);
              ec.Consume(Lib.Proto.GetDouble(deployModule, "ecCost") * elapsed_s);
            }
          }
        }
        else
        {
          if (Features.Signal)
          {
            Lib.Proto.Set(antenna, "extended", false);
          }
          else if (Features.KCommNet)
          {
            Lib.Proto.Set(antenna, "canComm", false);
          }
        }
      }
    }
  }
}
