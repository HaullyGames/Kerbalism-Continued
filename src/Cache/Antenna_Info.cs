using System;
using System.Linq;
using System.Collections.Generic;
using CommNet;

namespace KERBALISM
{
  public class AntennaValues
  {
    public short antCount = 0;              // Used to warning user: That is the only one antenna on that freq
    public int countConnections = 0;        // Used to warning user: Is frequency is connected
    public double antennaPower = 0;
    public double relayPower = 0;
    public double antennaRate = 0;          //  Kbps
    public double relayRate = 0;            //  Kbps
  }

  public class AntennasByFrequency
  {
    public double antennaPower;
    public double relayPower;
    public double antennaRate;                  //  Kbps
    public double relayRate;                    //  Kbps
    public List<AntennaPartInfo> AntennasList;
  }

  public class AntennaFreqCompare
  {
    public bool allAntennasEqual;
    public bool allRelaysEqual;
    public List<AntennaPartInfo> AntennasList;
  }

  public sealed class Antenna_Info
  {
    public Antenna_Info(Vessel v)
    {
      List<AntennaPartInfo> adaptorsInfoList = new List<AntennaPartInfo>();

      int numParts = (!v.loaded) ? v.protoVessel.protoPartSnapshots.Count : v.Parts.Count;

      for(int partIndex = 0; partIndex < numParts; partIndex++)
      {
        Part part;
        ProtoPartSnapshot protoPart = null;

        if (v.loaded) part = v.Parts[partIndex];
        else
        {
          protoPart = v.protoVessel.protoPartSnapshots[partIndex];
          part = PartLoader.getPartInfoByName(protoPart.partName).partPrefab;
        }

        bool hasInfo = false;
        AntennaPartInfo antennaPartInfo = new AntennaPartInfo();
        ProtoPartModuleSnapshot protoPartModule = null;

        // Inspect each module of the part
        for (int moduleIndex = 0; moduleIndex < part.Modules.Count; moduleIndex++)
        {
          NetworkAdaptor antennaMod = new NetworkAdaptor();
          PartModule pModule = part.Modules[moduleIndex];
          if(pModule is NetworkAdaptor)
          {
            if (v.loaded)
            {
              antennaMod = (NetworkAdaptor)pModule;
              antennaPartInfo.frequency = antennaMod.frequency;
              antennaPartInfo.ecCost = antennaMod.ecCost;
              antennaPartInfo.rate = antennaMod.rate;
              antennaPartInfo.name = part.partInfo.title;
            }
            else
            {
              ProtoPartModuleSnapshot netAdaptor = FlightGlobals.FindProtoPartByID(protoPart.flightID).FindModule("NetworkAdaptor");
              antennaPartInfo.frequency = Lib.Proto.GetShort(netAdaptor, "frequency");
              antennaPartInfo.ecCost = Lib.Proto.GetDouble(netAdaptor, "ecCost");
              antennaPartInfo.rate = Lib.Proto.GetDouble(netAdaptor, "rate");
              antennaPartInfo.name = protoPart.partInfo.title;
            }

            hasInfo = true;
          }
          else if (pModule is ICommAntenna)
          {
            ICommAntenna antenna = pModule as ICommAntenna;

            // This method only works when v.Loaded, otherwise this brings a wrong deployState
            ModuleDeployableAntenna anim = part.FindModuleImplementing<ModuleDeployableAntenna>();

            // Assume extended if there is no animator
            if (anim != null)
            {
              if (!v.loaded)
              {
                // This method works to !v.Loaded
                ProtoPartModuleSnapshot deployState = FlightGlobals.FindProtoPartByID(protoPart.flightID).FindModule("ModuleDeployableAntenna");
                antennaPartInfo.deployState = Lib.KCOMMNET.String_to_DeployState(Lib.Proto.GetString(deployState, "deployState"));
              }
              else antennaPartInfo.deployState = anim.deployState;
            }
            else antennaPartInfo.deployState = ModuleDeployablePart.DeployState.EXTENDED;

            if (!v.loaded) protoPartModule = protoPart.FindModule(pModule, moduleIndex);

            antennaPartInfo.antennaPower = (!v.loaded) ? antenna.CommPowerUnloaded(protoPartModule) : antenna.CommPower;
            antennaPartInfo.antennaCombinable = antenna.CommCombinable;
            antennaPartInfo.antennaCombinableExponent = antenna.CommCombinableExponent;
            antennaPartInfo.antennaType = antenna.CommType;
            //antennaPartInfo.partReference = part;
            //antennaPartInfo.partSnapshotReference = protoPart;
            antennaPartInfo.canComm = (!v.loaded) ? antenna.CanCommUnloaded(protoPartModule) : antenna.CanComm();

            hasInfo = true;
          }
        }

        if (hasInfo) adaptorsInfoList.Add(antennaPartInfo);
      }

      freqSortedList = adaptorsInfoList.OrderBy(a => a.frequency).ToList();   // order by frequency
      adaptorsSortedList = adaptorsInfoList.OrderBy(a => a.name).ToList();    // order by device name

      AntennaCalc(freqSortedList);
    }

    private void AntennaCalc(List<AntennaPartInfo> antennas)
    {
      Dictionary<short,AntennaFreqCompare> freqAntennaInfo = new Dictionary<short, AntennaFreqCompare>();
      List<AntennaPartInfo> antennasList;

      //freqAdaptorsDict = new Dictionary<short, AntennaValues>();
      antennasByFreq = new Dictionary<short, AntennasByFrequency>();

      string tAntennaName = null;
      string tRelayName = null;

      // Separate antennas in freq groups
      for (short index = 0; index < antennas.Count; index++)
      {
        // Create a new freq Key per freq
        if (!freqAntennaInfo.ContainsKey(antennas[index].frequency))
        {
          // Use to verify if all antennas are the same.
          // Every new freq, tAntennaName needs be define to identify if list has all equal antennas
          tAntennaName = antennas[index].name;
          if (antennas[index].antennaType == AntennaType.RELAY) tRelayName = tAntennaName;
          
          // Initial values
          AntennaFreqCompare newValues = new AntennaFreqCompare
          {
            allAntennasEqual = true,
            allRelaysEqual = true,
            AntennasList = new List<AntennaPartInfo> { antennas[index] }
          };

          freqAntennaInfo.Add(antennas[index].frequency, newValues);
        }
        // Update values for existing freq Key adding new antenna
        else
        {
          // Verify if new antenna\relay is equal previous antenna
          freqAntennaInfo[antennas[index].frequency].allAntennasEqual &= tAntennaName == antennas[index].name;
          if (antennas[index].antennaType == AntennaType.RELAY)
          {
            if(tRelayName == null) tRelayName = antennas[index].name;
            freqAntennaInfo[antennas[index].frequency].allRelaysEqual &= tRelayName == antennas[index].name;
          }
          freqAntennaInfo[antennas[index].frequency].AntennasList.Add(antennas[index]);
        }
      }

      // Calc the range for each fraq group
      foreach(short freq in freqAntennaInfo.Keys)
      {
        antennasList = new List<AntennaPartInfo>();

        double strongestAntenna = 0;                                                  // Strongest Antenna
        double antennaSum = 0;                                                        // Sum of all Antennas
        double antennaComb = 0;                                                       // Sum of all Antennas Combinability
        double antennaCombExponent = 0;                                               // Average Weighted Combinability Exponent
        double antennaRateSum = 0;                                                    // Sum of rate for freq antenna group

        double strongestRelay = 0;                                                    // Strongest Relay
        double relaySum = 0;                                                          // Sum of all Relay
        double relayComb = 0;                                                         // Sum of all Relay Combinability
        double relayCombExponent = 0;                                                 // Average Weighted Combinability Exponent
        double relayRateSum = 0;                                                      // Sum of rate for freq antenna group

        bool allAntennaEqual = freqAntennaInfo[freq].allAntennasEqual;                // If all antennas is Equal, antennaCombinableExponent = 1
        bool allRelayEqual = freqAntennaInfo[freq].allRelaysEqual;                    // If all relay is Equal, antennaCombinableExponent = 1

        double AntennaXModified;                                                      // (AntennaPower || RelayPower) * rangeModifier

        foreach (AntennaPartInfo info in freqAntennaInfo[freq].AntennasList)
        {
          //  Skip invalid antennas
          if (info.deployState != ModuleDeployablePart.DeployState.EXTENDED) continue;

          antennasList.Add(info);

          //  Sum rate for the group
          antennaRateSum += info.rate;

          //  Antenna Power X CommNet rangeModifier
          AntennaXModified = Math.Round(info.antennaPower * rangeModifier, 1);

          //  Strongest antenna
          strongestAntenna = (strongestAntenna < AntennaXModified ? AntennaXModified : strongestAntenna);

          //  Total Antenna Power
          antennaSum += AntennaXModified;

          //  Average Weighted Combinability
          //  If all antennas are Equal, CombinableExponent = 1, I guess this is the right because this way I can have the exactly the same result to signalStrength
          if (info.antennaCombinable) antennaComb += Math.Round(AntennaXModified * ((allAntennaEqual) ? 1 : info.antennaCombinableExponent), 1);

          //  If connected through Relay, accept only AntennaType = Relay to calc
          if (info.antennaType == AntennaType.RELAY)
          {
            //  Sum relay rate for the group
            relayRateSum += info.rate;

            //  Strongest antenna
            strongestRelay = (strongestRelay < AntennaXModified ? AntennaXModified : strongestRelay);

            //  Total Antenna Power
            relaySum += AntennaXModified;

            //  Average Weighted Combinability
            if (info.antennaCombinable) relayComb += Math.Round(AntennaXModified * ((allRelayEqual) ? 1 : info.antennaCombinableExponent), 1);
          }
        }
        //  Average Weighted Combinability Exponent 
        antennaCombExponent = antennaComb / antennaSum;
        relayCombExponent = relayComb / relaySum;

        //  Formula source = https://wiki.kerbalspaceprogram.com/wiki/CommNet
        //  Vessel Antenna Power = Strongest Antenna Power * ( Sum of Antenna's Powers / Strongest Antenna Power ) ^ ( Average Weighted Combinability Exponent for Vessel )
        //  Vessel Antenna Power = mainAntenna * (AntennaComb/AntennaSum) ^ AntennaCombExponent;
        //AntennaValues netAntennaValues = new AntennaValues
        //{
        //  antennaPower = Math.Ceiling(strongestAntenna * Math.Pow((antennaSum / strongestAntenna), antennaCombExponent)),
        //  antennaRate = antennaRateSum,
        //  relayPower = Math.Ceiling(strongestRelay * Math.Pow((relaySum / strongestRelay), relayCombExponent)),
        //  relayRate = relayRateSum
        //};

        double aPower = Math.Ceiling(strongestAntenna * Math.Pow((antennaSum / strongestAntenna), antennaCombExponent));
        double rPower = Math.Ceiling(strongestRelay * Math.Pow((relaySum / strongestRelay), relayCombExponent));

        AntennasByFrequency byFrequency = new AntennasByFrequency
        {
          antennaPower = aPower,
          relayPower = rPower,
          antennaRate = antennaRateSum,
          relayRate = relayRateSum,
          AntennasList = antennasList
        };

        //freqAdaptorsDict.Add(freq, netAntennaValues);
        antennasByFreq.Add(freq, byFrequency);
      }
    }

    public List<AntennaPartInfo> freqSortedList;                  // list order by frequency
    public List<AntennaPartInfo> adaptorsSortedList;              // list order by Adaptor name

    //public Dictionary<short, AntennaValues> freqAdaptorsDict;     // Dict to be used by CommNet System, dict is order by frequency
    public Dictionary<short, AntennasByFrequency> antennasByFreq; // Dict to be used by CommNet System, dict is order by frequency

    public bool isTimeToUpdate;                                   // used to define when the antennas needs be updated
    public bool hasECChanged;                                     // TODO
    public UInt64 inc;                                            // unique incremental id for the entry

    //  CommNet Range Modifier
    public static float rangeModifier = HighLogic.fetch.currentGame.Parameters.CustomParams<CommNetParams>().rangeModifier;
  }
}