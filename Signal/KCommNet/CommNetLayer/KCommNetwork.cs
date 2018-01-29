using System;
using System.Collections.Generic;
using CommNet;

// Original code by TaxiService from https://github.com/KSP-TaxiService/CommNetConstellation
namespace KERBALISM
{
  public class KCommNetwork : CommNetwork
  {
    // Check if two CommNodes are the exact same object
    public static bool AreSame(CommNode a, CommNode b)
    {
      if (a == null || b == null)
      {
        return false;
      }
      return a.precisePosition == b.precisePosition;
    }

    // TODO: Create rule to disconnect vessels without EC
    protected virtual bool TryConnectFreq(CommNode a, CommNode b, double distance, bool aCanRelay, bool bCanRelay, bool bothRelay, short freq = 0)
    {
      bool flag = false;
      double num1 = a.GetSignalStrengthMultiplier(b) * b.GetSignalStrengthMultiplier(a);
      double num2;

      AntennaValues a_Antennas = Cache.GetNodeAntennaCache(a, freq);
      AntennaValues b_Antennas = Cache.GetNodeAntennaCache(b, freq);

      if (bothRelay)
      {
        double normalizedRange = CommNetScenario.RangeModel.GetNormalizedRange(a_Antennas.relayPower, b_Antennas.relayPower, distance);
        if (normalizedRange > 0.0)
        {
          num2 = Math.Sqrt(a.antennaRelay.rangeCurve.Evaluate(normalizedRange) * b.antennaRelay.rangeCurve.Evaluate(normalizedRange)) * num1;
          if (num2 > 0.0)
            flag = true;
        }
        else
        {
          bothRelay = false;
          num2 = 0.0;
        }
      }
      else
        num2 = 0.0;
      double num3;
      if (aCanRelay)
      {
        double normalizedRange = CommNetScenario.RangeModel.GetNormalizedRange(a_Antennas.relayPower, b_Antennas.antennaPower, distance);
        if (normalizedRange > 0.0)
        {
          num3 = Math.Sqrt(a.antennaRelay.rangeCurve.Evaluate(normalizedRange) * b.antennaTransmit.rangeCurve.Evaluate(normalizedRange)) * num1;
          if (num3 > 0.0)
            flag = true;
        }
        else
        {
          aCanRelay = false;
          num3 = 0.0;
        }
      }
      else
      {
        aCanRelay = false;
        num3 = 0.0;
      }
      double num4;
      if (bCanRelay)
      {
        double normalizedRange = CommNetScenario.RangeModel.GetNormalizedRange(a_Antennas.antennaPower, b_Antennas.relayPower, distance);
        if (normalizedRange > 0.0)
        {
          num4 = Math.Sqrt(b.antennaRelay.rangeCurve.Evaluate(normalizedRange) * a.antennaTransmit.rangeCurve.Evaluate(normalizedRange)) * num1;
          if (num4 > 0.0)
            flag = true;
        }
        else
        {
          bCanRelay = false;
          num4 = 0.0;
        }
      }
      else
      {
        bCanRelay = false;
        num4 = 0.0;
      }
      if (flag)
      {
        CommLink commLink = Connect(a, b, distance);
        commLink.strengthRR = num2;
        commLink.strengthAR = num3;
        commLink.strengthBR = num4;
        commLink.aCanRelay = aCanRelay;
        commLink.bCanRelay = bCanRelay;
        commLink.bothRelay = bothRelay;

        // flag frequency as connected
        Cache.GetNodeAntennaCache(a, freq).countConnections += 1;
        Cache.GetNodeAntennaCache(b, freq).countConnections += 1;
        return true;
      }

      Disconnect(a, b, true);
      if (Cache.GetNodeAntennaCache(a, freq).countConnections > 0) Cache.GetNodeAntennaCache(a, freq).countConnections -= 1;
      if (Cache.GetNodeAntennaCache(b, freq).countConnections > 0) Cache.GetNodeAntennaCache(b, freq).countConnections -= 1;
      return false;
    }

    //Edit the connectivity between two potential nodes
    protected override bool SetNodeConnection(CommNode a, CommNode b)
    {
      //stop links between ground stations
      if (a.isHome && b.isHome)
      {
        Disconnect(a, b, true);
        return false;
      }

      List<short> aFreqs, bFreqs;

      //each CommNode has at least some frequencies?
      try
      {
        aFreqs = Cache.GetFrequencies(a);
        bFreqs = Cache.GetFrequencies(b);
      }
      catch (NullReferenceException e) // either CommNode could be a kerbal on EVA
      {
        Lib.Debug("Connection issue between '{0}' and '{1}'", a.name, b.name);
        Disconnect(a, b, true);
        return false;
      }

      //share same frequency?
      for (int i = 0; i < aFreqs.Count; i++)
      {
        if (bFreqs.Contains(aFreqs[i]))
        {
          AntennaValues a_Antennas = Cache.GetNodeAntennaCache(a, aFreqs[i]);
          AntennaValues b_Antennas = Cache.GetNodeAntennaCache(b, aFreqs[i]);

          if (a_Antennas.antennaPower + a_Antennas.relayPower == 0.0 || b_Antennas.antennaPower + b_Antennas.relayPower == 0.0)
          {
            Disconnect(a, b, true);
            return false;
          }
          Vector3d precisePosition1 = a.precisePosition;
          Vector3d precisePosition2 = b.precisePosition;

          double num = (precisePosition2 - precisePosition1).sqrMagnitude;
          double distance = a.distanceOffset + b.distanceOffset;
          if (distance != 0.0)
          {
            distance = Math.Sqrt(num) + distance;
            num = distance <= 0.0 ? (distance = 0.0) : distance * distance;
          }
          bool bothRelay = CommNetScenario.RangeModel.InRange(a_Antennas.relayPower, b_Antennas.relayPower, num);
          bool aCanRelay = bothRelay;
          bool bCanRelay = bothRelay;
          if (!bothRelay)
          {
            aCanRelay = CommNetScenario.RangeModel.InRange(a_Antennas.relayPower, b_Antennas.antennaPower, num);
            bCanRelay = CommNetScenario.RangeModel.InRange(a_Antennas.antennaPower, b_Antennas.relayPower, num);
          }
          if (!aCanRelay && !bCanRelay)
          {
            Disconnect(a, b, true);
            return false;
          }
          if (num == 0.0 && (bothRelay || aCanRelay || bCanRelay))
            return TryConnectFreq(a, b, 1E-07, aCanRelay, bCanRelay, bothRelay, aFreqs[i]);
          if (distance == 0.0)
            distance = Math.Sqrt(num);
          if (TestOcclusion(precisePosition1, a.occluder, precisePosition2, b.occluder, distance))
            return TryConnectFreq(a, b, distance, aCanRelay, bCanRelay, bothRelay, aFreqs[i]);

          Disconnect(a, b, true);
          return false;
        }
      }
      
      Disconnect(a, b, true);
      return false;
    }
  }
}
