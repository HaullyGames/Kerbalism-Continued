namespace KERBALISM
{
  public class AdvancedAntennaEC : ModuleECBase
  {
    [KSPField(isPersistant = true)] 
    public double rightAntennaRange;   // Required to support CommNet
  }

  public class AntennaEC : ECDevice
  {
    public AntennaEC(Antenna antenna, Vessel_Info vi, Resource_Info ec)
    {
      this.ec = ec;
      this.vi = vi;
      this.antenna = antenna;
      animator = antenna.part.FindModuleImplementing<ModuleAnimationGroup>();
      antennaAdvEC = antenna.part.FindModuleImplementing<AdvancedAntennaEC>();
      hasEC = ec.amount > double.Epsilon;
    }

    public AntennaEC(ModuleDataTransmitter antenna, Vessel_Info vi, Resource_Info ec)
    {
      this.ec = ec;
      this.vi = vi;
      transmitter = antenna;
      stockAnim = antenna.part.FindModuleImplementing<ModuleDeployableAntenna>();
      antennaAdvEC = antenna.part.FindModuleImplementing<AdvancedAntennaEC>();
      hasEC = ec.amount > double.Epsilon;
    }

    public override string Name()
    {
      return "antenna";
    }

    public override uint Part()
    {
      if (Features.KCommNet) return transmitter.part.flightID;
      else return antenna.part.flightID;
    }

    public override void Update()
    {
      if (antennaAdvEC == null) return;

      // Check if it is transmitting
      if (!Features.Science && Features.Signal && antenna != null)
      {
        // Basic transmitting mode when Science if off
        isTransmitting = antenna.stream.Transmitting();
      }
      else if (Features.Science)
      {
        // consume ec if data is transmitted or relayed
        isTransmitting = (vi.transmitting.Length > 0 || vi.relaying.Length > 0);
      }
      else isTransmitting = false;

      if (lastECstate != hasEC)
      {
        isConsuming = IsConsuming;
        if (!isConsuming) actualECCost = 0;
      }

      // If is transmitting, Kerbalism will EC in Science class.
      if (isTransmitting && isConsuming && !isPlaying)
      {
        // Just get EC to update display
        if (Features.Signal) actualECCost = antenna.cost;
        else if (Features.KCommNet) actualECCost = antennaAdvEC.ecCost;   // TODO: replace to KCommNet module
        isConsuming = false;
      }
    }

    public override bool IsConsuming
    {
      get
      {
        if (Features.Signal)
        {
          if (hasEC)
          {
            if (animator != null)
            {
              ToggleActions(animator, hasEC);
              // Add cost to Extending/Retracting
              isPlaying = animator.DeployAnimation.isPlaying;
              if (isPlaying)
              {
                actualECCost = antennaAdvEC.ecDeploy;
                return true;
              }
              else if (animator.isDeployed || (Settings.UnlinkedControl == UnlinkedCtrl.none))
              {
                animator.Events["RetractModule"].active = true;
                animator.Events["DeployModule"].active = false;

                // Makes antenna valid to AntennaInfo
                antenna.extended = true;
                actualECCost = antennaAdvEC.ecCost;
                return true;
              }
              else
              {
                animator.Events["RetractModule"].active = false;
                animator.Events["DeployModule"].active = true;
                return false;
              }
            }
            else
            {
              // this means that antenna is fixed
              // Makes antenna valid to AntennaInfo
              antenna.extended = true;
              actualECCost = antennaAdvEC.ecCost;
              return true;
            }
          }
          else
          {
            if (animator != null)
            {
              ToggleActions(animator, hasEC);
              // Don't allow extending/retracting when has no ec
              animator.Events["RetractModule"].active = false;
              animator.Events["DeployModule"].active = false;
            }
            // Makes antennaModule invalid to AntennaInfo
            antenna.extended = false;
            return false;
          }
        }
        else if (Features.KCommNet)
        {
          // Save antennaPower
          antennaAdvEC.rightAntennaRange = (antennaAdvEC.rightAntennaRange != transmitter.antennaPower && transmitter.antennaPower > 0 ? transmitter.antennaPower : antennaAdvEC.rightAntennaRange);

          if (hasEC)
          {
            if (stockAnim != null)
            {
              ToggleActions(stockAnim, hasEC);
              // Add cost to Extending/Retracting
              if (stockAnim.deployState == ModuleDeployablePart.DeployState.RETRACTING || stockAnim.deployState == ModuleDeployablePart.DeployState.EXTENDING)
              {
                actualECCost = antennaAdvEC.ecDeploy;
                isPlaying = true;
                return true;
              }
              else if (stockAnim.deployState == ModuleDeployablePart.DeployState.EXTENDED || (Settings.UnlinkedControl == UnlinkedCtrl.none))
              {
                stockAnim.Events["Retract"].active = true;
                stockAnim.Events["Extend"].active = false;

                // Recover antennaPower only if antenna is Extended
                transmitter.antennaPower = antennaAdvEC.rightAntennaRange;

                actualECCost = antennaAdvEC.ecCost;
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
              transmitter.antennaPower = antennaAdvEC.rightAntennaRange;
              actualECCost = antennaAdvEC.ecCost;
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
            // Change the antenna range to 0, causing CommNet to lose the signal
            transmitter.antennaPower = 0;
            return false;
          }
        }
        else return false;
      }
    }

    // Kerbalism Antenna
    Antenna antenna;
    ModuleAnimationGroup animator;

    // CommNet Antenna
    ModuleDataTransmitter transmitter;
    ModuleDeployableAntenna stockAnim;

    // EC module
    AdvancedAntennaEC antennaAdvEC;

    // Logical
    public bool isConsuming;      // Device is consuming EC
    bool hasEC;                   // Vessel has EC
    bool isTransmitting;          // Antenna is transmitting
    bool isPlaying;               // Animation is playing?
    bool lastECstate;             // Apply the update only if the state has changed

    // Auxiliary
    Vessel_Info vi;
    Resource_Info ec;
  }
}
