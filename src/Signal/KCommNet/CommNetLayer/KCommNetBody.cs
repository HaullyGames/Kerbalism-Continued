using CommNet;

namespace KERBALISM
{
  public class KCommNetBody : CommNetBody
  {
    public void CopyOf(CommNetBody stockBody)
    {
      Lib.Verbose("CommNet Body '{0}' added", stockBody.name);

      body = stockBody.GetComponentInChildren<CelestialBody>();
    }
  }
}
