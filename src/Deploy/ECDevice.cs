using System.Collections.Generic;

namespace KERBALISM
{
  public abstract class ECDevice
  {
    public abstract KeyValuePair<bool, double> GetConsume();

    public abstract bool IsConsuming { get; }
  }
}
