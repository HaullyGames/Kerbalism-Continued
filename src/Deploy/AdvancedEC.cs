using System.Collections.Generic;
using UnityEngine;

// This class is a Reliability modified.
namespace KERBALISM
{
  public sealed class AdvancedEC : PartModule
  {
    [KSPField(isPersistant = true)] public string type;     // component name
    [KSPField] public double extra_Cost = 0;                // extra energy cost to keep the part active
    [KSPField] public double extra_Deploy = 0;              // extra eergy cost to do a deploy(animation)

    [KSPField(guiName = "EC Usage", guiUnits = "/sec", guiActive = false, guiFormat = "F3")]
    public double actualCost = 0;                           // Show Energy Consume
    List<PartModule> modules;                               // components cache

    bool hasEnergy;                                         // Check if vessel has energy, otherwise will disable animations and functions
    bool isConsuming;
    Resource_Info resources;                                // Vessel resources

    public override void OnStart(StartState state)
    {
      // don't break tutorial scenarios
      if (Lib.DisableScenario(this)) return;

      // do nothing in the editors and when compiling part or when advanced EC is not enabled
      if (!Lib.IsFlight() || !Features.AdvancedEC) return;

      // cache list of modules
      modules = part.FindModulesImplementing<PartModule>().FindAll(k => k.moduleName == type);

      // setup UI
      Fields["actualCost"].guiActive = true;

      // get energy from cache
      resources = ResourceCache.Info(vessel, "ElectricCharge");
      hasEnergy = resources.amount > double.Epsilon;

      // sync monobehaviour state with module state
      // - required as the monobehaviour state is not serialized
      if (!hasEnergy)
      {
        foreach (PartModule m in modules)
        {
          m.enabled = false;
        }
      }

      // type-specific hacks
      if (!hasEnergy) Apply(true);
    }

    public void Update()
    {
      if (Lib.IsFlight() && Features.AdvancedEC)
      {
        // get energy from cache
        resources = ResourceCache.Info(vessel, "ElectricCharge");
        hasEnergy = resources.amount > double.Epsilon;

        // enforce state
        // - required as things like Configure or AnimationGroup can re-enable broken modules
        foreach (PartModule m in modules)
        {
          m.enabled = hasEnergy;
          m.isEnabled = hasEnergy;
        }
        if (!hasEnergy)
        {
          actualCost = 0;
          isConsuming = false;
          Apply(true);
        }
        else
        {
          isConsuming = GetIsConsuming(true);
        }
        
        // TODO: Implement update Interface for each module
        // update ui
      }
    }

    public void FixedUpdate()
    {
      // do nothing in the editor
      if (Lib.IsEditor()) return;

      // If has energym and isConsuming
      if (isConsuming)
      {
        if (resources != null) resources.Consume(actualCost * Kerbalism.elapsed_s);
      }
    }

    public bool GetIsConsuming(bool b)
    {
      KeyValuePair<bool, double> modReturn;
      if (b)
      {
        switch (type)
        {
          case "Antenna":
            ModuleDataTransmitter x = part.FindModuleImplementing<ModuleDataTransmitter>();
            modReturn = new AntennaEC(x, extra_Cost, extra_Deploy).GetConsume();
            actualCost = modReturn.Value;
            return modReturn.Key;
        }
      }
      actualCost = extra_Deploy;
      return true;
    }

    // apply type-specific hacks to enable/disable the module
    void Apply(bool b)
    {
      switch (type)
      {
        case "ModuleLight":
          if (b)
          {
            foreach (PartModule m in modules)
            {
              ModuleLight light = m as ModuleLight;
              if (light.animationName.Length > 0)
              {
                new Animator(part, light.animationName).Still(0.0f);
              }
              else
              {
                part.FindModelComponents<Light>().ForEach(k => k.enabled = false);
              }
            }
          }
          break;
      }
    }
  }
}
