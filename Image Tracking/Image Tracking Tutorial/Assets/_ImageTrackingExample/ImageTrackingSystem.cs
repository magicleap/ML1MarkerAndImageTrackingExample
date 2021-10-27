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
