namespace KERBALISM
{
  public class AntennaEC : ECDevice
  {
    public AntennaEC(Antenna antenna)
    {
      kAntenna = antenna;
    }

    public AntennaEC(ModuleDataTransmitter antenna)
    {

    }

    public override double GetEC()
    {
      throw new System.NotImplementedException();
    }

    // Kerbalism Antenna
    Antenna kAntenna;
    ModuleAnimationGroup kAnimator;

    // CommNet Antenna
    ModuleDataTransmitter transmitter;
    ModuleDeployableAntenna stockAnim;
  }
}
