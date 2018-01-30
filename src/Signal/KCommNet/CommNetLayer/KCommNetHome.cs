using System;
using System.Collections.Generic;
using UnityEngine;
using CommNet;
using KSP.Localization;

// Original code by TaxiService from https://github.com/KSP-TaxiService/CommNetConstellation
namespace KERBALISM
{
  public class KCommNetHome : CommNetHome, IComparable<KCommNetHome>
  {
    static readonly Texture markTexture = Icons.home;
    static GUIStyle groundStationHeadline;
    bool loadCompleted = false;

    //to be saved to persistent.sfs
    [Persistent] public string ID;
    [Persistent] public Color Color = Color.red;
    [Persistent(collectionIndex = "Frequency")] public List<short> Frequencies = new List<short>(new short[] { 0 });

    public double GetAltitude { get { return alt; } }
    public double GetLatitude { get { return lat; } }
    public double GetLongitude { get { return lon; } }
    public CommNode commNode { get { return comm; } }

    public void CopyOf(CommNetHome stockHome)
    {
      Lib.Verbose("CommNet Home '{0}' added", stockHome.nodeName);

      ID = stockHome.nodeName;
      nodeName = stockHome.nodeName;
      displaynodeName = Localizer.Format(stockHome.displaynodeName);
      nodeTransform = stockHome.nodeTransform;
      isKSC = stockHome.isKSC;
      body = stockHome.GetComponentInParent<CelestialBody>();

      groundStationHeadline = new GUIStyle(HighLogic.Skin.label)
      {
        fontSize = 12,
        normal = { textColor = Color.yellow }
      };

      loadCompleted = true;
    }

    // Apply the changes from persistent.sfs
    public void ApplySavedChanges(KCommNetHome stationSnapshot)
    {
      Color = stationSnapshot.Color;
      Frequencies = stationSnapshot.Frequencies;
    }

    // Replace one specific frequency with new frequency
    public void ReplaceFrequency(short oldFrequency, short newFrequency)
    {
      Frequencies.Remove(oldFrequency);
      Frequencies.Add(newFrequency);
      Frequencies.Sort();
    }

    // Drop the specific frequency from the list
    public bool DeleteFrequency(short frequency)
    {
      return Frequencies.Remove(frequency);
    }

    // Draw graphic components on screen like RemoteTech's ground-station marks
    public void OnGUI()
    {
      if (HighLogic.CurrentGame == null || !loadCompleted || Lib.IsPaused()) return;

      if (!(HighLogic.LoadedScene == GameScenes.FLIGHT || HighLogic.LoadedScene == GameScenes.TRACKSTATION)) return;

      if ((!HighLogic.CurrentGame.Parameters.CustomParams<CommNetParams>().enableGroundStations && !isKSC) || !MapView.MapIsEnabled || MapView.MapCamera == null) return;

      Vector3d worldPos = ScaledSpace.LocalToScaledSpace(nodeTransform.transform.position);

      if (MapView.MapCamera.transform.InverseTransformPoint(worldPos).z < 0f) return;

      Vector3 position = PlanetariumCamera.Camera.WorldToScreenPoint(worldPos);
      Rect groundStationRect = new Rect((position.x - 8), (Screen.height - position.y) - 8, 16, 16);

      if (IsOccluded(nodeTransform.transform.position, body)) return;

      if (!IsOccluded(nodeTransform.transform.position, body) && IsCamDistanceToWide(nodeTransform.transform.position)) return;

      //draw the dot
      Color previousColor = GUI.color;
      GUI.color = Color;
      GUI.DrawTexture(groundStationRect, markTexture, ScaleMode.ScaleToFit, true);
      GUI.color = previousColor;

      //draw the headline above and below the dot
      if (Lib.KCOMMNET.ContainsMouse(groundStationRect))
      {
        Rect headlineRect = groundStationRect;

        //Name
        Vector2 nameDim = groundStationHeadline.CalcSize(new GUIContent(displaynodeName));
        headlineRect.x -= nameDim.x / 2 - 5;
        headlineRect.y -= nameDim.y + 5;
        headlineRect.width = nameDim.x;
        headlineRect.height = nameDim.y;
        GUI.Label(headlineRect, displaynodeName, groundStationHeadline);

        //frequency list
        string freqStr = "No frequency assigned";

        if (Frequencies.Count > 0)
        {
          freqStr = "Broadcasting in";
          for (int i = 0; i < Frequencies.Count; i++)
            freqStr += "\n~ frequency " + Frequencies[i];
        }

        headlineRect = groundStationRect;
        Vector2 freqDim = groundStationHeadline.CalcSize(new GUIContent(freqStr));
        headlineRect.x -= freqDim.x / 2 - 5;
        headlineRect.y += groundStationRect.height + 5;
        headlineRect.width = freqDim.x;
        headlineRect.height = freqDim.y;
        GUI.Label(headlineRect, freqStr, groundStationHeadline);
      }
    }

    // Check whether this vector3 location is behind the body
    // Original code by regex from https://github.com/NathanKell/RealSolarSystem/blob/master/Source/KSCSwitcher.cs
    private bool IsOccluded(Vector3d position, CelestialBody body)
    {
      Vector3d camPos = ScaledSpace.ScaledToLocalSpace(PlanetariumCamera.Camera.transform.position);

      if (Vector3d.Angle(camPos - position, body.position - position) > 90)
        return false;
      return true;
    }

    // Calculate the distance between the camera position and the ground station, and
    // return true if the distance is >= DistanceToHideGroundStations from the settings file.
    private bool IsCamDistanceToWide(Vector3d loc)
    {
      Vector3d camPos = ScaledSpace.ScaledToLocalSpace(PlanetariumCamera.Camera.transform.position);
      float distance = Vector3.Distance(camPos, loc);

      if (distance >= 8000000)
        return true;
      return false;
    }

    // Allow to be sorted easily
    public int CompareTo(KCommNetHome other)
    {
      return displaynodeName.CompareTo(other.displaynodeName);
    }
  }
}
