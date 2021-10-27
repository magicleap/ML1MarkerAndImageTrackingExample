using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;
using MagicLeap.Core;

public class ArUcoTrackingManager : MonoBehaviour
{
    //Add Magic Leap's ArUcoTracker Settings to the inspector so we can select our markers.
    public MLArucoTracker.Settings trackerSettings = MLArucoTracker.Settings.Create();

    //Create a variable to hold the markerIds the tracker tracks.
    private HashSet<int> _arucoMarkerIds = new HashSet<int>();

    //Add a prefab that will display when we've detected a marker
    public GameObject MLArucoMarkerPrefab;

    void Start()
    {
        //Update the tracker settings and add the callback that will trigger on marker detection
        MLArucoTracker.UpdateSettings(trackerSettings);
        MLArucoTracker.OnMarkerStatusChange += OnMarkerStatusChange;
    }

    void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            DisableAruco();
        }
        else
        {
            if (MLPrivileges.RequestPrivilege(MLPrivileges.Id.CameraCapture).Result == MLResult.Code.PrivilegeGranted)
            {
                EnableAruco();
            }
        }
    }

    void OnDestroy()
    {
        if (MLArucoTracker.IsStarted)
        {
            MLArucoTracker.OnMarkerStatusChange -= OnMarkerStatusChange;
        }
    }

    private void DisableAruco()
    {
        trackerSettings.Enabled = false;
        MLArucoTracker.UpdateSettings(trackerSettings);
    }

    private void EnableAruco()
    {
        trackerSettings.Enabled = true;
        MLArucoTracker.UpdateSettings(trackerSettings);
    }

    //This is where the magic happens, anything we want to happen when markers are triggered goes here
    private void OnMarkerStatusChange(MLArucoTracker.Marker marker, MLArucoTracker.Marker.TrackingStatus status)
    {
        if (status == MLArucoTracker.Marker.TrackingStatus.Tracked)
        {
            if (_arucoMarkerIds.Contains(marker.Id))
            {
                //This ensures we don't add the marker and prefab if we're already tracking it
                return;
            }

            //Instantiate the prefab that will follow that marker -- note: the TrackerBehavior component will handle position and rotation.
            GameObject arucoMarker = Instantiate(MLArucoMarkerPrefab);
            //Adjust the properties of the TrackerBehavior component to add the markerID and the dictionary we're comparing the marker to.
            MLArucoTrackerBehavior arucoBehavior = arucoMarker.GetComponent<MLArucoTrackerBehavior>();
            arucoBehavior.MarkerId = marker.Id;
            arucoBehavior.MarkerDictionary = MLArucoTracker.TrackerSettings.Dictionary;
            //Add the markerId so we don't do this again
            _arucoMarkerIds.Add(marker.Id);
        }
        else if (_arucoMarkerIds.Contains(marker.Id))
        {
            //if the marker's status indicates it's no longer tracked, remove it from the list so if it comes back we'll detect it.
            _arucoMarkerIds.Remove(marker.Id);
        }
    }
}
