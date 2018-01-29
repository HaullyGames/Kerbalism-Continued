using CommNet;
using System.Collections.Generic;

// Original code by TaxiService from https://github.com/KSP-TaxiService/CommNetConstellation
namespace KERBALISM
{
  [KSPScenario(ScenarioCreationOptions.AddToAllGames, new[] { GameScenes.SPACECENTER, GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.TRACKSTATION })]
  public class KCommNetScenario : CommNetScenario
  {
    public static new KCommNetScenario Instance { get; protected set; }

    protected override void Start()
    {
      if (Features.KCommNet)
      {
        Instance = this;
        Cache.InitGroundStation();
        Cache.refreshCommNode = true;

        Lib.Debug("KCommNet Scenario loading ...");
        //Replace the CommNet user interface
        CommNetUI ui = FindObjectOfType<CommNetUI>();
        CustomCommNetUI = gameObject.AddComponent<KCommNetUI>();
        Destroy(ui);

        //Replace the CommNet network
        CommNetNetwork net = FindObjectOfType<CommNetNetwork>();
        CustomCommNetNetwork = gameObject.AddComponent<KCommNetNetwork>();
        Destroy(net);

        //Replace the CommNet ground stations
        CommNetHome[] homes = FindObjectsOfType<CommNetHome>();
        for (int i = 0; i < homes.Length; i++)
        {
          KCommNetHome customHome = homes[i].gameObject.AddComponent(typeof(KCommNetHome)) as KCommNetHome;
          customHome.CopyOf(homes[i]);
          Destroy(homes[i]);
          //DB.Station(customHome.nodeName);
          Cache.LoadGroundStation(customHome);
        }

        //Replace the CommNet celestial bodies
        CommNetBody[] bodies = FindObjectsOfType<CommNetBody>();
        for (int i = 0; i < bodies.Length; i++)
        {
          KCommNetBody customBody = bodies[i].gameObject.AddComponent(typeof(KCommNetBody)) as KCommNetBody;
          customBody.CopyOf(bodies[i]);
          Destroy(bodies[i]);
        }
        Lib.Verbose("CommNet Scenario loading done!");
      }
    }

    public override void OnAwake()
    {
      //override to turn off CommNetScenario's instance check
      GameEvents.onVesselCreate.Add(new EventData<Vessel>.OnEvent(OnVesselCountChanged));
      GameEvents.onVesselDestroy.Add(new EventData<Vessel>.OnEvent(OnVesselCountChanged));
    }

    private void OnDestroy()
    {
      if (CustomCommNetUI != null) Destroy(CustomCommNetUI);
      if (CustomCommNetNetwork != null) Destroy(CustomCommNetNetwork);

      Cache.InitCommVessels();

      GameEvents.onVesselCreate.Remove(new EventData<Vessel>.OnEvent(OnVesselCountChanged));
      GameEvents.onVesselDestroy.Remove(new EventData<Vessel>.OnEvent(OnVesselCountChanged));
    }

    public static void CacheCommNetVessels()
    {
      if (!Cache.refreshCommNode) return;

      Cache.InitCommVessels();

      List<Vessel> allVessels = FlightGlobals.fetch.vessels;
      for (int i = 0; i < allVessels.Count; i++)
      {
        if(allVessels[i].connection != null && Lib.IsVessel(allVessels[i]))
        {
          Lib.Debug("Caching CommNetVessel '{0}'", allVessels[i].vesselName);
          Cache.CommNodeInfo(allVessels[i]);
        }
      }
      Cache.refreshCommNode = false;
    }

    private void OnVesselCountChanged(Vessel v)
    {
      if (Lib.IsVessel(v))
      {
        Lib.Debug("Change in the vessel list detected. Cache refresh required");
        Cache.refreshCommNode = true;
      }
    }

    KCommNetUI CustomCommNetUI = null;
    KCommNetNetwork CustomCommNetNetwork = null;
  }
}
