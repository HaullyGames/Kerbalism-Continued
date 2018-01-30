using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KERBALISM
{
  public static class NetInfo
  {
    public static void NetMan(this Panel p, Vessel v)
    {
      // avoid corner-case when this is called in a lambda after scene changes
      v = FlightGlobals.FindVessel(v.id);

      // if vessel doesn't exist anymore, leave the panel empty
      if (v == null) return;

      // get info from the cache
      Vessel_Info vi = Cache.VesselInfo(v);

      // if not a valid vessel, leave the panel empty
      if (!vi.is_valid) return;

      // set metadata
      p.Title(Lib.BuildString(Lib.Ellipsis(v.vesselName, 20), " <color=#cccccc>NETWORK INFO</color>"));

      // time-out simulation
#if !DEBUG
      if (p.Timeout(vi)) return;
#endif

      p.SetSection("ADAPTORS");
      p.Set_IsFreqSelector(true);

      // store all devices
      var devices = new Dictionary<uint, NetDevice>();

      // store device being added
      NetDevice adap;

      // loaded vessel
      if (v.loaded)
      {
        foreach (NetworkAdaptor m in Lib.FindModules<NetworkAdaptor>(v))
        {
          adap = new NetAdaptorDevice(m);

          // add the device
          // - multiple same-type components in the same part will have the same id, and are ignored
          if (!devices.ContainsKey(adap.Id()))
          {
            devices.Add(adap.Id(), adap);
          }
        }
      }
      else
      {
        // store data required to support multiple modules of same type in a part
        var PD = new Dictionary<string, Lib.module_prefab_data>();

        // for each part
        foreach (ProtoPartSnapshot proto in v.protoVessel.protoPartSnapshots)
        {
          // get part prefab (required for module properties)
          Part part_prefab = PartLoader.getPartInfoByName(proto.partName).partPrefab;

          // get all module prefabs
          var module_prefabs = part_prefab.FindModulesImplementing<PartModule>();

          // clear module indexes
          PD.Clear();

          // for each module
          foreach (ProtoPartModuleSnapshot m in proto.modules)
          {
            // get the module prefab
            // if the prefab doesn't contain this module, skip it
            PartModule module_prefab = Lib.ModulePrefab(module_prefabs, m.moduleName, PD);
            if (!module_prefab) continue;

            // if the module is disabled, skip it
            // note: this must be done after ModulePrefab is called, so that indexes are right
            if (!Lib.Proto.GetBool(m, "isEnabled")) continue;

            if (m.moduleName == "NetworkAdaptor")
            {
              adap = new ProtoNetAdaptorDevice(m, proto.flightID, v);

              // add the device
              // - multiple same-type components in the same part will have the same id, and are ignored
              if (!devices.ContainsKey(adap.Id()))
              {
                devices.Add(adap.Id(), adap);
              }
            }
          }
        }
      }

      // dict order by device name
      // for each device
      foreach (var pair in devices.OrderBy(x => x.Value.Name()))
      {
        // render device entry
        NetDevice dev = pair.Value;
        // Get how many antennas share the same freq
        AntennasByFrequency x = null;
        if (vi.antenna.antennasByFreq.ContainsKey(dev.InfoFreq()))
        {
          x = vi.antenna.antennasByFreq[dev.InfoFreq()];
        }

        p.SetContent(dev.Name(), dev.InfoRate(), string.Empty, null, () => Highlighter.Set(dev.Part(), Color.cyan), dev.InfoFreq());
        p.SetIcon(Icons.left_freq, "Decrease", () =>
        {
          if (dev.InfoFreq() > 0) // && x != null
          {
            //if (x.antCount == 1 && x.countConnections > 0)
            //{
            //  Lib.Popup(
            //    "Warning!",
            //    Lib.BuildString("This is the last antenna on '", dev.InfoFreq().ToString(),
            //                    "' frequency.\nYou will lost connection in this frequency.\nDo you really want to remove this frequency from this vessel?"),
            //    new DialogGUIButton("Remove", () => dev.ChangeFreq(-1)),
            //    new DialogGUIButton("Keep it", () => { }));
            //}
            //else 
            dev.ChangeFreq(-1);
          }
        });
        p.SetIcon(Icons.right_freq, "Increase", () =>
        {
          if (dev.InfoFreq() < 99) // && x != null
          {
            //if (x.antCount == 1 && x.countConnections > 0)
            //{
            //  Lib.Popup(
            //    "Warning!",
            //    Lib.BuildString("This is the last antenna on '", dev.InfoFreq().ToString(),
            //                    "' frequency.\nYou will lost connection in this frequency.\nDo you really want to remove this frequency from this vessel?"),
            //    new DialogGUIButton("Remove", () => dev.ChangeFreq(+1)),
            //    new DialogGUIButton("Keep it", () => { }));
            //}
            //else 
            dev.ChangeFreq(+1);
          }
        });
      }

      p.SetSection("FREQUENCY(S) DETAIL");
      foreach (short key in vi.antenna.antennasByFreq.Keys)
      {
        double range = vi.antenna.antennasByFreq[key].antennaPower;
        double rate = vi.antenna.antennasByFreq[key].antennaRate;

        Render_ConnectionDetail(p, range, rate, key);
      }
    }

    static void Render_ConnectionDetail(Panel p, double range, double rate, short freq)
    {
      // render frequency
      string detail = Lib.BuildString("<size=10>", Lib.HumanReadableRange(range),"</size>");
      p.SetContent(Lib.BuildString("Frequency: <b>", freq.ToString(), "</b>"), detail, Lib.BuildString("<b>", Lib.HumanReadableDataRate(rate), "</b>"));
    }
  }
}