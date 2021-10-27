# Image Tracking
### Tutorial and Code Snippets
<br/>
<br/>
<br/>

## Overview
<br/>

**Image tracking** on the Magic Leap allows you to detect two-dimensional planar images from a custom-defined target image dataset and then continuously track the images' locations and orientations as you or the images move through the environment. You can also place digital content based on the presence and transform of a physical image.

This tutorial shows how to implement image tracking on the Magic Leap by building a Unity project from scratch using the **Magic Leap Unity SDK v0.26**.
<br/>
<br/>

---

<br/>

### Objectives
<br/>

 After completing this tutorial, you will be able to:
 - Recognize and track images with a Magic Leap Unity project
 - Place digital content relative to the tracked image
 - Know some best practices when using image tracking features
<br/>
<br/>


---

<br/>

## Tutorial Steps
<br/>

1. Set up the Unity Project.
2. Create a scene and add the image tracking components. 
3. Create the image tracking script.
4. Upload the target image and assign it to our tracker.
5. Build to the device and test!
    - Includes how to use **Zero Iteration** with image tracking.

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
4. Make sure the Manifest Privileges include "Camera Capture" by going to **Edit > Project Settings > Magic Leap > Manifest Settings > Reality** and selecting **CameraCapture**. Without camera access, the device cannot scan for images.

<br/>

### 2) Scene Components
<br/>

1. Create a new scene.
2. Delete the "Main Camera" GameObject and add a Magic Leap **Main Camera**, found by going to **Packages > Magic Leap SDK > Tools > Prefabs**.
3. Add an **Empty Game Object** named "ImageTrackingManager".
4. On ImageTrackingManager, add a **component** -- a **new script** named "ImageTrackingSystem".

<br/>

### 3) Script and Code Snippets
<br/>

1. Open the ImageTrackingSystem.cs script in a code editor of your choice. 
2. Add the *UnityEngine.XR.MagicLeap* using directive, the *ImageTargetInfo* inspector helper class, and the *ImageTrackingStatus* enum to the top of the script:

        using UnityEngine;
        using UnityEngine.XR.MagicLeap;

        //This creates a class that exposes image target variables to the inspector
        [System.Serializable]
        public class ImageTargetInfo
        {
            public string Name;
            public Texture2D Image;
            public float LongerDimension;
        }

        //This contains the four possible statuses we can encounter while trying to use the tracker.
        public enum ImageTrackingStatus
        {
            Inactive,
            PrivilegeDenied,
            ImageTrackingActive,
            CameraUnavailable
        }

3. Define the global variables in our main class:

        //The main class containing our image tracking functions
        public class ImageTrackingSystem : MonoBehaviour
        {
            //The prefab that will pop up when we detect an image and follow it if it moves
            public GameObject TrackedImageFollower;

            // The image target built from the ImageTargetInfo object
            private MLImageTracker.Target _imageTarget;

            //The inspector field where we assign our target images
            public ImageTargetInfo TargetInfo;

            // The main event and statuses for Image Tracking functionality
            public delegate void TrackingStatusChanged(ImageTrackingStatus status);
            public static TrackingStatusChanged OnImageTrackingStatusChanged;
            public ImageTrackingStatus CurrentStatus;

            //These allow us to see the position and rotation of the detected image from the inspector
            public Vector3 ImagePos = Vector3.zero;
            public Quaternion ImageRot = Quaternion.identity;

4. Add *Privilege Methods* to ensure device has the necessary access to the camera:
    - For sensitive privileges such as CameraCapture, the app will need to explictly request the user to accept this usage. To help with this, the Magic Leap Unity Package provides the MLPrivilegeRequesterBehavior component that will handle prompting the user for privilege usage.

        #region Unity Methods
        private void Start()
        {
            // Request Privileges when the Image Tracker System is started
            ActivatePrivileges(); 
        }

        //Update() was removed
        #endregion

        #region Privilege Methods
        private void ActivatePrivileges()
        {
            // If privilege was not already denied by User:
            if (CurrentStatus != ImageTrackingStatus.PrivilegeDenied)
            {
                // Try to get the component to request privileges
                MLPrivilegeRequesterBehavior requesterBehavior = GetComponent<MLPrivilegeRequesterBehavior>();
                if (requesterBehavior == null)
                {
                    // No Privilege Requester was found, add one and setup for a CameraCapture request
                    requesterBehavior = gameObject.AddComponent<MLPrivilegeRequesterBehavior>();
                    requesterBehavior.Privileges = new MLPrivileges.RuntimeRequestId[]
                    {
                        MLPrivileges.RuntimeRequestId.CameraCapture
                    };
                }
                // Listen for the privileges done event
                requesterBehavior.OnPrivilegesDone += HandlePrivilegesDone;
                requesterBehavior.enabled = true; // Component should be disabled in the editor until requested, this is discussed further below
            }
        }

        void HandlePrivilegesDone(MLResult result)
        {
            // Unsubscribe from future requests for privileges now that this one has been handled.
            GetComponent<MLPrivilegeRequesterBehavior>().OnPrivilegesDone -= HandlePrivilegesDone;

            if (result.IsOk) 
            {
                // The privilege was accepted, the service can begin
                StartImageTracking();
            }
            else
            {
                Debug.LogError("Camera Privilege Denied or Not Present in Manifest");
                UpdateImageTrackingStatus(ImageTrackingStatus.PrivilegeDenied);
            }
        }
        #endregion

5. Add the Image Tracking functionality, including starting and stopping the system, updating the system's status, and handling the callback for when the image detection status has changed:
    - This is where our most important function, *HandleImageTracked*, lives. Almost all functionality you want to add will go here.
        
            #region Image Tracking Methods

            private void UpdateImageTrackingStatus(ImageTrackingStatus status)
            {
                CurrentStatus = status;
                OnImageTrackingStatusChanged?.Invoke(CurrentStatus);
            }

            public void StartImageTracking()
            {
                // Only start Image Tracking if privilege wasn't denied
                if (CurrentStatus != ImageTrackingStatus.PrivilegeDenied)
                {
                    // Is not already started, and failed to start correctly, this is likely due to the camera already being in use:
                    //You will get a warning that the start and stop API has been deprecated, this will be fixed in a future update to this tutorial
                    if (!MLImageTracker.IsStarted && !MLImageTracker.Start().IsOk)
                    {
                        Debug.LogError("Image Tracker Could Not Start");
                        UpdateImageTrackingStatus(ImageTrackingStatus.CameraUnavailable);
                        return;
                    }

                    // MLImageTracker would have been started by previous If statement at this point, so enable it. 
                    if (MLImageTracker.Enable().IsOk)
                    {
                        // Add the target image to the tracker and set the callback
                        _imageTarget = MLImageTracker.AddTarget(TargetInfo.Name, TargetInfo.Image,
                        TargetInfo.LongerDimension, HandleImageTracked);
                        UpdateImageTrackingStatus(ImageTrackingStatus.ImageTrackingActive);
                    }
                    else
                    {
                        Debug.LogError("Image Tracker Could Not Start");
                        UpdateImageTrackingStatus(ImageTrackingStatus.CameraUnavailable);
                        return;
                    }
                }
            }

            public void StopImageTracking(bool pause)
            {
                if (MLImageTracker.IsStarted)
                {
                    if (pause == true) // Temporarily disable the Image Tracker
                    {
                        MLImageTracker.RemoveTarget(TargetInfo.Name);
                        MLImageTracker.Disable();
                    }
                    else
                    {
                        //You will get a warning that the start and stop API has been deprecated, this will be fixed in a future update to this tutorial.
                        MLImageTracker.Stop();
                    }
                }
            }

            /*
            * Most Important Function
            * 
            * This is where the magic happens, anything that you want to happen upon detection or movement of an image, include it here.
            *
            */
            private void HandleImageTracked(MLImageTracker.Target imageTarget,
                                            MLImageTracker.Target.Result imageTargetResult)
            {
                // If tracked, update position / rotation and move following object to that position / rotation
                switch (imageTargetResult.Status)
                {
                    case MLImageTracker.Target.TrackingStatus.Tracked:

                        ImagePos = imageTargetResult.Position;
                        ImageRot = imageTargetResult.Rotation;

                        if (TrackedImageFollower != null)
                        {
                            TrackedImageFollower.transform.position = ImagePos;
                            TrackedImageFollower.transform.rotation = ImageRot;
                        }
                        break;

                    case MLImageTracker.Target.TrackingStatus.NotTracked:
                        // Additional Logic can be added here for when the image is not detected
                        break;
                }
            }
            #endregion

6. Now all we need to do is tidy up the application life cycle by adding instructions for when the application is paused or closed. *Note: Add these to the Unity Methods region, above Start()*

        private void Awake()
        {
            UpdateImageTrackingStatus(ImageTrackingStatus.Inactive);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus == true)
            {
                StopImageTracking(true);
            }
            else
            {
                StartImageTracking();
            }
        }

        private void OnDestroy()
        {
            StopImageTracking(false);
        }


***Note: The full script is located at the bottom of the page***

<br/>

### 4) Image Assets and Follower Prefab
<br/>

Now that our script is complete, we can add the images we want to track. Download the following image to your **Assets** folder:
<br/>
<img src="./MLImage_Submarine.png" alt="A colorful image of a submarine" width="600">
<!-- ![A colorful image of a submarine](./MLImage_Submarine.png) -->
<br/>
<br/>

Click on the image in your Assets folder and select **Read/Write Enabled** in the inspector under **Import Settings > Advanced** -- this ensures we can access the texture memory from our script.
<br/>

Add the image from your Assets to the scene under **ImageTrackingManager > ImageTrackingSystem > Target Info > Image**. In that same inspector section, input "Submarine" to the **Name** field (though the name can be whatever you want), and input "0.236" to the **Longer Dimension** field.
- The 0.236 measurement corresponds to the physical length of the image if you print it out on a standard letter-sized paper (0.236 m = 9.2913386 in). If you want to open the image on your computer and track that image instead of printing it out, you'll need to measure the length of the image as displayed on your screen and input the correct measurement under **Longer Dimension**. This is important as this is how the tracking system knows where to place the image -- if the dimension is longer or shorter than it actually is, the digital content will appear distorted.
<br/>

Now we can create a basic GameObject to follow the image when tracked. We will keep the GameObject visible in the scene on start, so we know the scene has opened correctly even before tracking an image.
- Add a **Cube** to the scene, and change the **Transform > Position** to (0, 0, 5) and the **Transform > Rotation** to (0, 15, 0), just to make the cube easy to see on launch.
- Change the Cube's **Transform > Scale** to (0.1, 0.1, 0.1). It must be scaled down or it will appear huge on device.
- Add the Cube to **ImageTrackingManager > ImageTrackingSystem > Tracked Image Follower**

<br/>

### 5) Build and Test
<br/>
Now we can build to the headset and test it out! Make sure you have the image printed out or displayed and that you have updated the **Longer Dimension** measurement as outlined in step 4. Open the scene on device and you should be able to see the cube right away. Once you point the headset at the target image, the cube should move to the center of the image.
<br/>
<br/>

### Zero Iteration
<br/>

To use Zero Iteration to test this image tracking app, follow the [Image Tracking instructions for ZI](https://developer.magicleap.com/en-us/learn/guides/tools-zi-module#image-tracking-panel).

<br/>

---

<br/>

## Image Tracking Features
<br/>

For a complete list of features and best practices for using Magic Leap's Image Tracking API, check out the [Image Tracking Features page](https://developer.magicleap.com/en-us/learn/guides/lumin-sdk-image-tracking).

<br/>

---

<br/>

## Troubleshooting and FAQ
<br/>

- Make sure the target images are well-lit, whether physically printed or digitally displayed, or else the camera may not detect the image. At the same time, make sure there isn't too much light reflecting off the image that may obscure its features.
- When choosing an image, follow the guidelines in the features section above. QR Codes and ArUco markers are *not* recommended for image targets. For detecting and tracking QR Codes or ArUco markers, see those tutorials (TODO: link).
- Use .jpg or .png for your image files.
- Make sure you are far enough away from the image, the Magic Leap will not display content that is within the headset's clip distance.

<br/>

---

<br/>

## Full ImageTrackingSystem.cs Script

        using UnityEngine;
        using UnityEngine.XR.MagicLeap;

        //This creates a class that exposes image target variables to the inspector
        [System.Serializable]
        public class ImageTargetInfo
        {
            public string Name;
            public Texture2D Image;
            public float LongerDimension;
        }

        //This contains the four possible statuses we can encounter while trying to use the tracker.
        public enum ImageTrackingStatus
        {
            Inactive,
            PrivilegeDenied,
            ImageTrackingActive,
            CameraUnavailable
        }

        //The main class containing our image tracking functions
        public class ImageTrackingSystem : MonoBehaviour
        {
            //The prefab that will pop up when we detect an image and follow it if it moves
            public GameObject TrackedImageFollower;

            // The image target built from the ImageTargetInfo object
            private MLImageTracker.Target _imageTarget;

            //The inspector field where we assign our target images
            public ImageTargetInfo TargetInfo;

            // The main event and statuses for Image Tracking functionality
            public delegate void TrackingStatusChanged(ImageTrackingStatus status);
            public static TrackingStatusChanged OnImageTrackingStatusChanged;
            public ImageTrackingStatus CurrentStatus;

            //These allow us to see the position and rotation of the detected image from the inspector
            public Vector3 ImagePos = Vector3.zero;
            public Quaternion ImageRot = Quaternion.identity;

            #region Unity Method
            private void Awake()
            {
                UpdateImageTrackingStatus(ImageTrackingStatus.Inactive);
            }

            private void OnApplicationPause(bool pauseStatus)
            {
                if (pauseStatus == true)
                {
                    StopImageTracking(true);
                }
                else
                {
                    StartImageTracking();
                }
            }

            private void OnDestroy()
            {
                StopImageTracking(false);
            }


            private void Start()
            {
                // Request Privileges when the Image Tracker System is started
                ActivatePrivileges(); 
            }

            //Update() was removed
            #endregion

            #region Privilege Methods
            private void ActivatePrivileges()
            {
                // If privilege was not already denied by User:
                if (CurrentStatus != ImageTrackingStatus.PrivilegeDenied)
                {
                    // Try to get the component to request privileges
                    MLPrivilegeRequesterBehavior requesterBehavior = GetComponent<MLPrivilegeRequesterBehavior>();
                    if (requesterBehavior == null)
                    {
                        // No Privilege Requester was found, add one and setup for a CameraCapture request
                        requesterBehavior = gameObject.AddComponent<MLPrivilegeRequesterBehavior>();
                        requesterBehavior.Privileges = new MLPrivileges.RuntimeRequestId[]
                        {
                            MLPrivileges.RuntimeRequestId.CameraCapture
                        };
                    }
                    // Listen for the privileges done event
                    requesterBehavior.OnPrivilegesDone += HandlePrivilegesDone;
                    requesterBehavior.enabled = true; // Component should be disabled in the editor until requested, this is discussed further below
                }
            }

            void HandlePrivilegesDone(MLResult result)
            {
                // Unsubscribe from future requests for privileges now that this one has been handled.
                GetComponent<MLPrivilegeRequesterBehavior>().OnPrivilegesDone -= HandlePrivilegesDone;

                if (result.IsOk) 
                {
                    // The privilege was accepted, the service can begin
                    StartImageTracking();
                }
                else
                {
                    Debug.LogError("Camera Privilege Denied or Not Present in Manifest");
                    UpdateImageTrackingStatus(ImageTrackingStatus.PrivilegeDenied);
                }
            }
            #endregion

            #region Image Tracking Methods

            private void UpdateImageTrackingStatus(ImageTrackingStatus status)
            {
                CurrentStatus = status;
                OnImageTrackingStatusChanged?.Invoke(CurrentStatus);
            }

            public void StartImageTracking()
            {
                // Only start Image Tracking if privilege wasn't denied
                if (CurrentStatus != ImageTrackingStatus.PrivilegeDenied)
                {
                    // Is not already started, and failed to start correctly, this is likely due to the camera already being in use:
                    //You will get a warning that the start and stop API has been deprecated, ignore, this will be fixed in a future update to this tutorial
                    if (!MLImageTracker.IsStarted && !MLImageTracker.Start().IsOk)
                    {
                        Debug.LogError("Image Tracker Could Not Start");
                        UpdateImageTrackingStatus(ImageTrackingStatus.CameraUnavailable);
                        return;
                    }

                    // MLImageTracker would have been started by previous If statement at this point, so enable it. 
                    if (MLImageTracker.Enable().IsOk)
                    {
                        // Add the target image to the tracker and set the callback
                        _imageTarget = MLImageTracker.AddTarget(TargetInfo.Name, TargetInfo.Image,
                        TargetInfo.LongerDimension, HandleImageTracked);
                        UpdateImageTrackingStatus(ImageTrackingStatus.ImageTrackingActive);
                    }
                    else
                    {
                        Debug.LogError("Image Tracker Could Not Start");
                        UpdateImageTrackingStatus(ImageTrackingStatus.CameraUnavailable);
                        return;
                    }
                }
            }

            public void StopImageTracking(bool pause)
            {
                if (MLImageTracker.IsStarted)
                {
                    if (pause == true) // Temporarily disable the Image Tracker
                    {
                        MLImageTracker.RemoveTarget(TargetInfo.Name);
                        MLImageTracker.Disable();
                    }
                    else
                    {
                        //You will get a warning that the start and stop API has been deprecated, ignore, this will be fixed in a future update to this tutorial.
                        MLImageTracker.Stop();
                    }
                }
            }

            /*
            * Most Important Function
            * 
            * This is where the magic happens, anything that you want to happen upon detection or movement of an image, include it here.
            *
            */
            private void HandleImageTracked(MLImageTracker.Target imageTarget,
                                            MLImageTracker.Target.Result imageTargetResult)
            {
                // If tracked, update position / rotation and move following object to that position / rotation
                switch (imageTargetResult.Status)
                {
                    case MLImageTracker.Target.TrackingStatus.Tracked:

                        ImagePos = imageTargetResult.Position;
                        ImageRot = imageTargetResult.Rotation;

                        if (TrackedImageFollower != null)
                        {
                            TrackedImageFollower.transform.position = ImagePos;
                            TrackedImageFollower.transform.rotation = ImageRot;
                        }
                        break;

                    case MLImageTracker.Target.TrackingStatus.NotTracked:
                        // Additional Logic can be added here for when the image is not detected
                        break;
                }
            }
            #endregion
        }

