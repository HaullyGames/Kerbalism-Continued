using System.Collections.Generic;

namespace KERBALISM
{
  public class AdvancedEC
  {
    // return set of devices on a vessel
    // - the list is only valid for a single simulation step
    public static Dictionary<uint, Device> Boot(Vessel v)
    {
      // store all devices
      var devices = new Dictionary<uint, Device>();

      // store device being added
      Device dev;

      // loaded vessel
      if (v.loaded)
      {
        foreach (PartModule m in Lib.FindModules<PartModule>(v))
        {
          switch (m.moduleName)
          {
            case "ProcessController": dev = new ProcessDevice(m as ProcessController); break;
            case "Greenhouse": dev = new GreenhouseDevice(m as Greenhouse); break;
            case "GravityRing": dev = new RingDevice(m as GravityRing); break;
            case "Emitter": dev = new EmitterDevice(m as Emitter); break;
            case "Harvester": dev = new HarvesterDevice(m as Harvester); break;
            case "Laboratory": dev = new LaboratoryDevice(m as Laboratory); break;
            case "Antenna": dev = new AntennaDevice(m as Antenna); break;
            case "ModuleDataTransmitter": dev = new AntennaDevice(m as ModuleDataTransmitter); break;
            case "Experiment": dev = new ExperimentDevice(m as Experiment); break;
            case "ModuleDeployableSolarPanel": dev = new PanelDevice(m as ModuleDeployableSolarPanel); break;
            case "ModuleGenerator": dev = new GeneratorDevice(m as ModuleGenerator); break;
            case "ModuleResourceConverter": dev = new ConverterDevice(m as ModuleResourceConverter); break;
            case "ModuleKPBSConverter": dev = new ConverterDevice(m as ModuleResourceConverter); break;
            case "FissionReactor": dev = new ConverterDevice(m as ModuleResourceConverter); break;
            case "ModuleResourceHarvester": dev = new DrillDevice(m as ModuleResourceHarvester); break;
            case "ModuleLight": dev = new LightDevice(m as ModuleLight); break;
            case "ModuleColoredLensLight": dev = new LightDevice(m as ModuleLight); break;
            case "ModuleMultiPointSurfaceLight": dev = new LightDevice(m as ModuleLight); break;
            case "SCANsat": dev = new ScannerDevice(m); break;
            case "ModuleSCANresourceScanner": dev = new ScannerDevice(m); break;
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
              case "ProcessController": dev = new ProtoProcessDevice(m, module_prefab as ProcessController, p.flightID); break;
              case "Greenhouse": dev = new ProtoGreenhouseDevice(m, p.flightID); break;
              case "GravityRing": dev = new ProtoRingDevice(m, p.flightID); break;
              case "Emitter": dev = new ProtoEmitterDevice(m, p.flightID); break;
              case "Harvester": dev = new ProtoHarvesterDevice(m, module_prefab as Harvester, p.flightID); break;
              case "Laboratory": dev = new ProtoLaboratoryDevice(m, p.flightID); break;
              case "Antenna": dev = new ProtoAntennaDevice(m, p.flightID, v); break;
              case "ModuleDataTransmitter": dev = new ProtoAntennaDevice(m, p.flightID, v); break;
              case "Experiment": dev = new ProtoExperimentDevice(m, module_prefab as Experiment, p.flightID); break;
              case "ModuleDeployableSolarPanel": dev = new ProtoPanelDevice(m, module_prefab as ModuleDeployableSolarPanel, p.flightID); break;
              case "ModuleGenerator": dev = new ProtoGeneratorDevice(m, module_prefab as ModuleGenerator, p.flightID); break;
              case "ModuleResourceConverter": dev = new ProtoConverterDevice(m, module_prefab as ModuleResourceConverter, p.flightID); break;
              case "ModuleKPBSConverter": dev = new ProtoConverterDevice(m, module_prefab as ModuleResourceConverter, p.flightID); break;
              case "FissionReactor": dev = new ProtoConverterDevice(m, module_prefab as ModuleResourceConverter, p.flightID); break;
              case "ModuleResourceHarvester": dev = new ProtoDrillDevice(m, module_prefab as ModuleResourceHarvester, p.flightID); break;
              case "ModuleLight": dev = new ProtoLightDevice(m, p.flightID); break;
              case "ModuleColoredLensLight": dev = new ProtoLightDevice(m, p.flightID); break;
              case "ModuleMultiPointSurfaceLight": dev = new ProtoLightDevice(m, p.flightID); break;
              case "SCANsat": dev = new ProtoScannerDevice(m, part_prefab, v, p.flightID); break;
              case "ModuleSCANresourceScanner": dev = new ProtoScannerDevice(m, part_prefab, v, p.flightID); break;
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
