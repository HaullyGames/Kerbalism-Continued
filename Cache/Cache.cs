using CommNet;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KERBALISM
{
  public static class Cache
  {
    public static void Init()
    {
      vessels = new Dictionary<uint, Vessel_Info>();
      antennasCache = new Dictionary<uint, Antenna_Info>();
      next_inc = 0;
    }

    public static void Clear()
    {
      vessels.Clear();
      next_inc = 0;
    }

    public static void Purge(Vessel v)
    {
      uint vID = Lib.VesselID(v);
      vessels.Remove(vID);
      antennasCache.Remove(vID);
      commVessels.Remove(vID);
    }

    public static void Purge(ProtoVessel pv)
    {
      uint vID = Lib.VesselID(pv);
      vessels.Remove(vID);
      antennasCache.Remove(vID);
      commVessels.Remove(vID);
    }

    public static void Update()
    {
      // purge the oldest entry from the vessel cache
      if (vessels.Count > 0)
      {
        UInt64 oldest_inc = UInt64.MaxValue;
        UInt32 oldest_id = 0;
        foreach (KeyValuePair<UInt32, Vessel_Info> pair in vessels)
        {
          if (pair.Value.inc < oldest_inc)
          {
            oldest_inc = pair.Value.inc;
            oldest_id = pair.Key;
          }
        }
        vessels.Remove(oldest_id);
      }
    }

    public static Vessel_Info VesselInfo(Vessel v)
    {
      // get vessel id
      UInt32 id = Lib.VesselID(v);

      // get the info from the cache, if it exist
      if (vessels.TryGetValue(id, out Vessel_Info info)) return info;

      // compute vessel info
      info = new Vessel_Info(v, id, next_inc++);

      // store vessel info in the cache
      vessels.Add(id, info);

      return info;
    }

    #region KCommNet

    #region GroundStation
    public static void InitGroundStation() => groundStations = new List<KCommNetHome>();

    public static void LoadGroundStation(KCommNetHome station) => groundStations.Add(station);

    public static List<KCommNetHome> GetGroundStation() => groundStations;

    public static KCommNetHome FindCorrespondingGroundStation(CommNode commNode)
    {
      return groundStations.Find(x => KCommNetwork.AreSame(x.commNode, commNode));
    }
    #endregion

    #region CacheCommNode
    public static List<KCommNetVessel> GetCommNetVessels(short targetFrequency = -1)
    {
      //List<KCommNetVessel> newList = new List<KCommNetVessel>();

      //// Get List<KCommNetVessel> that has targetFrequency
      //newList = 
      return commVessels.Where(x => x.Value.freqList.Contains(targetFrequency)).Select(x => x.Value).ToList();
    }

    public static Vessel FindCorrespondingVessel(CommNode commNode)
    {
      foreach (var a in commVessels)
      {
        if (KCommNetwork.AreSame(a.Value.Comm, commNode)) return a.Value.Vessel;
      }
      return null;
    }

    public static void InitCommVessels()
    {
      Lib.Debug("CommVessel cache has been initialized");
      commVessels = new Dictionary<uint, KCommNetVessel>();
    }

    public static void CommNodeInfo(Vessel v)
    {
      // get vessel id
      UInt32 id = Lib.VesselID(v);

      // get the info from the cache, if it exist update
      if (commVessels.TryGetValue(id, out KCommNetVessel info))
      {
        // needs to be refresh
        if (refreshCommNode)
        {
          // Update old cache
          Lib.Debug("Triggered: Update existing CommNode cache");

          commVessels[id] = (KCommNetVessel)v.connection;
        }
      }
      else // Create a new entry
      {
        Lib.Debug("New CommNode has been cached");
        info = (KCommNetVessel)v.connection;
        commVessels.Add(id, info);
      }
    }
    #endregion

    public static AntennaValues GetNodeAntennaCache(CommNode node, short freq)
    {
      AntennaValues node_Antennas;

      if (node.isHome)
      {
        node_Antennas = new AntennaValues()
        {
          antennaPower = node.antennaTransmit.power,
          relayPower = node.antennaRelay.power
        };
      }
      else
      {
        Vessel v = FindCorrespondingVessel(node);
        if(AntennaInfo(v).freqAdaptorsDict.Keys.Contains(freq))
        {
          node_Antennas = AntennaInfo(v).freqAdaptorsDict[freq];
        }
        else
        {
          node_Antennas = new AntennaValues() { antCount = 0, antennaPower = 0, antennaRate = 0, countConnections = 0, relayPower = 0, relayRate = 0 };
        }
      }
      return node_Antennas;
    }

    public static List<short> GetFrequencies(CommNode a)
    {
      List<short> aFreqs = new List<short>();

      if (a.isHome && FindCorrespondingGroundStation(a) != null)
      {
        aFreqs.AddRange(FindCorrespondingGroundStation(a).Frequencies);
      }
      else
      {
        aFreqs = AntennaInfo(FindCorrespondingVessel(a)).freqAdaptorsDict.Keys.ToList();
      }

      return aFreqs;
    }

    public static Antenna_Info AntennaInfo(Vessel v)
    {
      // get vessel id
      UInt32 id = Lib.VesselID(v);

      // get the info from the cache, if it exist update
      if (antennasCache.TryGetValue(id, out Antenna_Info info))
      {
        // isTimeToUpdate is altered when Vessel was changed(stage, break), antenna has extended/retracted 
        if (!info.isTimeToUpdate) return info;
      }
      else // Create a new entry
      {
        Lib.Debug("New antenna cache");
        info = new Antenna_Info(v);
        antennasCache.Add(id, info);
      }
      return info;
    }

    public static void AntennaUpdate(Vessel v, bool refresh = false)
    {
      // get vessel id
      UInt32 id = Lib.VesselID(v);

      bool hasEC = ResourceCache.Info(v, "ElectricCharge").amount > double.Epsilon;

      // get the info from the cache, if it exist update
      if (antennasCache.TryGetValue(id, out Antenna_Info info))
      {
        // EC state changed since last update?
        if (info.hasECChanged != hasEC || info.isTimeToUpdate)
        {
          // Update old cache
          Lib.Debug("Triggered: Update existing antenna cache");

          // Update Antenna
          antennasCache[id] = new Antenna_Info(v)
          {
            hasECChanged = hasEC
          };
        }
      }
      else // Create a new entry
      {
        Lib.Debug("New antenna cache");
        info = new Antenna_Info(v) { hasECChanged = hasEC };
        antennasCache.Add(id, info);
      }
    }
    #endregion

    public static bool HasVesselInfo(Vessel v, out Vessel_Info vi) => vessels.TryGetValue(Lib.VesselID(v), out vi);

    static Dictionary<UInt32, KCommNetVessel> commVessels;    // Vessel
    static List<KCommNetHome> groundStations;                 // GroundStation cache
    public static bool refreshCommNode;

    static Dictionary<UInt32, Antenna_Info> antennasCache;    // This will be update only if antenna change
    static Dictionary<UInt32, Vessel_Info> vessels;           // vessel cache
    static UInt64 next_inc;                                   // used to generate unique id
  }
}
