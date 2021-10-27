# ArUco Marker Tracking
### Tutorial and Code Snippets
<br/>
<br/>
<br/>

## Overview
<br/>

**ArUco Marker Tracking** on the Magic Leap allows you to detect two-dimensional planar images from the ArUco Marker dataset and then continuously track the images' locations and orientations as you or the images move through the environment. You can also place digital content based on the presence and transform of a physical image.

Magic Leap devices track ArUco markers, also known as fiducial markers or augmented reality markers, using a tracking system separate from the image tracking system. You can only have one active tracker at a time. ArUco trackers can track one size in one marker dictionary at a time. You can track up to sixteen markers at a time.

This tutorial shows how to implement ArUco marker tracking on the Magic Leap by building a Unity project from scratch using the **Magic Leap Unity SDK v0.26**.
<br/>
<br/>

---

<br/>

### Objectives
<br/>

 After completing this tutorial, you will be able to:
 - Recognize and track ArUco markers with a Magic Leap Unity project
 - Place digital content relative to the tracked image
 - Know some best practices when using AruCo marker tracking features
<br/>
<br/>


---

<br/>

## Tutorial Steps
<br/>

1. Set up the Unity Project.
2. Create a scene and add the marker tracking components. 
3. Create the marker tracking script.
4. Create a marker and the tracked prefab assigned to our tracker.
5. Build to the device and test!

<br/>

### 1) Unity Setup
<br/>

1. Create a new Unity 3D Project with version 2020.3+.
2. Import the Magic Leap SDK Package by following the [package install steps](https://developer.magicleap.com/en-us/learn/guides/import-the-magic-leap-unity-package).
3. Complete the ["Getting Started" Unity Setup steps for a new Unity project](https://developer.magicleap.com/en-us/learn/guides/unity-setup-intro).
    - Major steps (for detailed steps follow guide above):
        - Change build target to **Lumin**.
        - Change the color target to **Linear**.
        - Set the location of your **developer certificate**. 
        - Check the **Magic Leap** box under **Project Settings > XR Plug-In Management > Lumin**
4. Make sure the Manifest Privileges include "Camera Capture" by going to **Edit > Project Settings > Magic Leap > Manifest Settings > Reality** and selecting **CameraCapture**. Without camera access, the device cannot scan for markers.

<br/>

### 2) Scene Components
<br/>

1. Create a new scene.
2. Delete the "Main Camera" GameObject and add a Magic Leap **Main Camera**, found by going to **Packages > Magic Leap SDK > Tools > Prefabs**.
3. Add an **Empty Game Object** named "ArUcoTrackingManager".
4. On ArUcoTrackingManager, add a **component** -- a **new script** named "ArUcoTrackingManager".

<br/>

### 3) ArUco Marker Manager Script and Code Snippets
<br/>

***Note: The full script is located at the bottom of the page***
<br/>

1. Open the ArUcoTrackingManager.cs script in a code editor of your choice. 
2. Add the following using directives, settings class, and variables to the top of the script:

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

3. Add the Start() function and supplementary functions that help tidy up the application lifecycle on pause and close.

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

4.Add the main and final function **OnMarkerStatusChange** where the tracking any related functionality lives:

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

<br/>

### 4) Tracked Prefab and Target Marker
<br/>

Now that our script is complete, we can create a prefab that will follow our detected markers and choose the ArUco markers we want to track. 

Let's create a basic GameObject to follow the marker when tracked. We will keep the GameObject visible in the scene on start, so we know the scene has opened correctly even before tracking an image.
- Add a **Cube** named "MLArucoMarkerPrefab" to the scene, and change the **Transform > Position** to (0, 0, 5) and the **Transform > Rotation** to (0, 15, 0), just to make the cube easy to see on launch.
- Change the Cube's **Transform > Scale** to (0.1, 0.1, 0.1). It must be scaled down or it will appear huge on device.
- Add the component script **ML Aruco Tracker Behavior**. This handles the updates to the prefab so it can track the marker's position and rotation and other variables that interface with the ArUco Tracker API.
- Add the Cube to **ArUcoTrackingManager > ArUcoTrackingManager > ML Aruco Marker Prefab**

Now we can decide which type of ArUco marker we want to track. Under our prefab's TrackerBehavior component, we can see the dropdown **Marker Dictionary** and input **Marker Id**. This lets us select which marker we will scan for -- the default is *DICT_4X4_50* and ID *0*, we'll keep those and find a marker to match. An ArUco marker is a synthetic square marker composed by a wide black border and an inner binary matrix which determines its identifier (id). The *4X4* corresponds to the inner binary grid, the id *0* is the specific data within that grid, and the *50* is the size of the marker in pixels. You can read more about ArUco markers [here](https://docs.opencv.org/4.5.2/d5/dae/tutorial_aruco_detection.html).

*Note: You can change which markers to track during runtime by changing the Marker Dictionary property.*

To find an image of the marker that fits the *4x4_50* and id *0* type, we can use a [ArUco Marker Generation Tool](https://chev.me/arucogen/) and input those specific properties. Save the marker that is generated after adjusting the dictionary, id, and size. It doesn't need to be in the project, you just need to be able to open or print the image for scanning. Here's the marker that fits those parameters:

<br/>
<img src="./4x4_50.svg" alt="A colorful image of a submarine" width="100">

<br/>
<br/>

Lastly, we need to go back to the original ArUcoTrackingManager game object and make sure that dictionary matches the prefab's. We'll also need to measure our marker's physical size so that our content will be placed accordingly. Measure your marker image at the size you'll be using when scanning for it, and input that number *in meters* under **ArUco Tracking Manager > Marker Length**. The correct size is essential if we want to place the digital content on the marker appropriately sized and positioned. Markers should be at least .04m wide, or else the device will have trouble scanning it reliably.
<br/>
<br/>


### 5) Build and Test
<br/>
Now we can build to the headset and test it out! Make sure you have the marker printed out or displayed at the correct size. Open the scene on device and you should be able to see the cube right away. Once you point the headset at the target marker, the cube should move to the center of the image.
<br/>
<br/>

---

<br/>

## ArUco Tracking Features
<br/>
For features and best practices for using Magic Leap's ArUco Tracking API, check out the [ArUco Tracking Features page](https://developer.magicleap.com/en-us/learn/guides/lumin-sdk-aruco-tracking).

<br/>
<br/>


---

<br/>

## Troubleshooting and FAQ
<br/>

- Make sure the target markers are well-lit, whether physically printed or digitally displayed, or else the camera may not detect the marker. At the same time, make sure there isn't too much light reflecting off the marker that may obscure its features.
- Print your markers in black and white. High contrast is important to detect the marker and poses.
- To aid detection, include a white border around your marker. The white border must be large enough to be clearly visible on the camera.
- Note that you may want to flush the results array periodically to free memory.
- If you have more than sixteen markers, the smallest or furthest away markers are ignored.
- Once you're done, destroy the tracker and free resources.

<br/>

---

<br/>

## Full ImageTrackingSystem.cs Script

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
