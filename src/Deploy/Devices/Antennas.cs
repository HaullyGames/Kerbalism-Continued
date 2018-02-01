using System.Collections.Generic;

namespace KERBALISM
{
  public class AntennaEC : ECDevice
  {
    public AntennaEC(Antenna antenna, double extra_Cost, double extra_Deploy)
    {
      this.antenna = antenna;
      this.extra_Cost = extra_Cost;
      this.extra_Deploy = extra_Deploy;
      animator = antenna.part.FindModuleImplementing<ModuleAnimationGroup>();
    }

    public AntennaEC(ModuleDataTransmitter antenna, double extra_Cost, double extra_Deploy)
    {
      transmitter = antenna;
      this.extra_Cost = extra_Cost;
      this.extra_Deploy = extra_Deploy;
      stockAnim = antenna.part.FindModuleImplementing<ModuleDeployableAntenna>();
    }

    public override KeyValuePair<bool, double> GetConsume()
    {
      return new KeyValuePair<bool, double>(IsConsuming, actualCost);
    }

    public override bool IsConsuming
    {
      get
      {
        if (Features.Signal)
        {
          if (animator != null)
          {
            if (animator.DeployAnimation.isPlaying)
            {
              actualCost = extra_Deploy;
              return true;
            }
            else if (animator.isDeployed || (Settings.UnlinkedControl == UnlinkedCtrl.none))
            {
              actualCost = extra_Cost;
              return true;
            }
          }
          else
          {
            // this means that antenna is fixed
            actualCost = extra_Cost;
            return true;
          }
        }
        else if (Features.KCommNet)
        {
          if (stockAnim != null)
          {
            if (stockAnim.deployState == ModuleDeployablePart.DeployState.RETRACTING || stockAnim.deployState == ModuleDeployablePart.DeployState.EXTENDING)
            {
              actualCost = extra_Deploy;
              return true;
            }
            else if (stockAnim.deployState == ModuleDeployablePart.DeployState.EXTENDED || (Settings.UnlinkedControl == UnlinkedCtrl.none))
            {
              actualCost = extra_Cost;
              return true;
            }
          }
          else
          {
            actualCost = extra_Cost;
            return true;
          }
        }
        actualCost = 0;
        return false;
      }
    }

    // Kerbalism Antenna
    Antenna antenna;
    ModuleAnimationGroup animator;

    // CommNet Antenna
    ModuleDataTransmitter transmitter;
    ModuleDeployableAntenna stockAnim;

    // Logical
    double extra_Cost;
    double extra_Deploy;

    // Return
    double actualCost;
  }
}
