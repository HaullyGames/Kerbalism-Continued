﻿namespace KERBALISM 
{
  public sealed class EmitterDevice : Device
  {
    public EmitterDevice(Emitter emitter)
    {
      this.emitter = emitter;
    }

    public override string Name()
    {
      return "emitter";
    }

    public override uint Part()
    {
      return emitter.part.flightID;
    }

    public override string Info()
    {
      return emitter.running ? "<color=cyan>active</color>" : "<color=red>disabled</color>";
    }

    public override void Ctrl(bool value)
    {
      if (emitter.running != value) emitter.Toggle();
    }

    public override void Toggle()
    {
      Ctrl(!emitter.running);
    }

    Emitter emitter;
  }

  public sealed class ProtoEmitterDevice : Device
  {
    public ProtoEmitterDevice(ProtoPartModuleSnapshot emitter, uint part_id)
    {
      this.emitter = emitter;
      this.part_id = part_id;
    }

    public override string Name()
    {
      return "emitter";
    }

    public override uint Part()
    {
      return part_id;
    }

    public override string Info()
    {
      return Lib.Proto.GetBool(emitter, "running") ? "<color=cyan>active</color>" : "<color=red>disabled</color>";
    }

    public override void Ctrl(bool value)
    {
      Lib.Proto.Set(emitter, "running", value);
    }

    public override void Toggle()
    {
      Ctrl(!Lib.Proto.GetBool(emitter, "running"));
    }

    ProtoPartModuleSnapshot emitter;
    uint part_id;
  }
}