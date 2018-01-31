using System.Collections.Generic;

namespace KERBALISM
{
  public class AdvancedEC
  {
    // return set of devices on a vessel
    // - the list is only valid for a single simulation step
    public static Dictionary<uint, ECDevice> Boot(Vessel v)
    {
      // get vessel info
      Vessel_Info vi = Cache.VesselInfo(v);

      // get resource handler
      Vessel_Resources resources = ResourceCache.Get(v);
      Resource_Info ec = resources.Info(v, "ElectricCharge");

      // store all devices
      var devices = new Dictionary<uint, ECDevice>();

      // store device being added
      ECDevice dev;

      // loaded vessel
      if (v.loaded)
      {
        foreach (PartModule m in Lib.FindModules<PartModule>(v))
        {
          switch (m.moduleName)
          {
            case "Antenna":                 dev = new AntennaEC(m as Antenna, vi, ec);                  break;
            case "ModuleDataTransmitter":   dev = new AntennaEC(m as ModuleDataTransmitter, vi, ec);    break;
            default: continue;
          }

          // add the device
          // - multiple same-type components in the same part will have the same id, and are ignored
          if (!devices.ContainsKey(dev.Id()))
          {
            devices.Add(dev.Id(), dev);
          }
        }
      }
      // unloaded vessel
      else
      {
        // store data required to support multiple modules of same type in a part
        var PD = new Dictionary<string, Lib.module_prefab_data>();

        // for each part
        foreach (ProtoPartSnapshot p in v.protoVessel.protoPartSnapshots)
        {
          // get part prefab (required for module properties)
          Part part_prefab = PartLoader.getPartInfoByName(p.partName).partPrefab;

          // get all module prefabs
          var module_prefabs = part_prefab.FindModulesImplementing<PartModule>();

          // clear module indexes
          PD.Clear();

          // for each module
          foreach (ProtoPartModuleSnapshot m in p.modules)
          {
            // get the module prefab
            // if the prefab doesn't contain this module, skip it
            PartModule module_prefab = Lib.ModulePrefab(module_prefabs, m.moduleName, PD);
            if (!module_prefab) continue;

            // if the module is disabled, skip it
            // note: this must be done after ModulePrefab is called, so that indexes are right
            if (!Lib.Proto.GetBool(m, "isEnabled")) continue;

            // depending on module name
            switch (m.moduleName)
            {
              case "Antenna":               dev = new ProtoAntennaDevice(m, p.flightID, v); break;
              case "ModuleDataTransmitter": dev = new ProtoAntennaDevice(m, p.flightID, v); break;
              default: continue;
            }

            // add the device
            // - multiple same-type components in the same part will have the same id, and are ignored
            if (!devices.ContainsKey(dev.Id()))
            {
              devices.Add(dev.Id(), dev);
            }
          }
        }
      }

      // return all devices found
      return devices;
    }
  }
}
