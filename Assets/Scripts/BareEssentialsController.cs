using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using WSAControllerPlugin;
 
public class BareEssentialsController : MonoBehaviour
{
    public static Color COLOR_LASER_ACTIVE = Color.green;
    public static Color COLOR_LASER_CALIBRATING = Color.yellow;

    /// HandheldControllerBridge Game Object that has a HandheldControllerBridge component and communicates with the ControllerPlugin. 
    public GameObject HandheldControllerBridge;

    // The HandheldControllerBridge component Monobehaviour class instance
    private HandheldControllerBridge _controllerBridge;

    /// The on-screen GameObject representing the handheld controller device in the scene.
    public GameObject ControllerDisplay;

    // Status Label  
    public Text _statusLabel;

    // Offset for the on-screen hadheld controller Game Object
    public Vector3 ControllerOffsetPosition;

    // Laser
    private GameObject _controllerDisplayLaser;
    
    
    void Start()
    {
        // Get a reference to the HandheldControllerBridge Monobehaviour
        _controllerBridge = HandheldControllerBridge.GetComponent<HandheldControllerBridge>();

        // Set up controller button press handlers
        _controllerBridge.HomeBtnDown += HomeBtnDownHandler;
        _controllerBridge.HomeBtnUp += HomeBtnUpHandler; 
 
        // Laser Game Object
        _controllerDisplayLaser = ControllerDisplay.transform.Find( "beam" ).gameObject;
        
        // Subscribe to each BLE state change on _controllerBridge
        _controllerBridge.BLEActive += BLEActiveHandler;
        _controllerBridge.BLEInactive += BLEInactiveHandler;
        _controllerBridge.BLEConnecting += BLEConnectingHandler;
        _controllerBridge.BLEConnectionComplete += BLEConnectionCompleteHandler;
        _controllerBridge.BLEScanning += BLEScanningHandler;
        _controllerBridge.BLEScanComplete += BLEScanCompleteHandler;

        // Respond to Controller Calibration state changes
        _controllerBridge.DelayedCalibrationBegan += ControllerCalibrationBeganHandler;
        _controllerBridge.DelayedCalibrationCancelled += ControllerCalibrationCancelledHandler;
        _controllerBridge.CalibrationComplete += ControllerCalibrationCompleteHandler;

        _controllerBridge.Scan( true );
    }  

    void Update()
    {
        if ( !_controllerBridge.IsCalibrationDelayActive )
        {
            ControllerDisplay.transform.localRotation = _controllerBridge.GetRotation();
        }
        else
        {
            ControllerDisplay.transform.localRotation = Quaternion.identity;
        }

        // Move the on-screen controller when the headset moves 
        // ControllerDisplay.transform.position = Camera.main.transform.position + ControllerOffsetPosition;       
    }


    //
    // HandheldControllerBridge Orientation status Action Handlers
    // 

    private void ControllerCalibrationBeganHandler()
    {
        _controllerDisplayLaser.GetComponent<MeshRenderer>().material.color = COLOR_LASER_CALIBRATING;
        _controllerDisplayLaser.GetComponent<MeshRenderer>().material.SetColor( "_EmissionColor", COLOR_LASER_CALIBRATING );
    }

    private void ControllerCalibrationCancelledHandler()
    {
        _controllerDisplayLaser.GetComponent<MeshRenderer>().material.color = COLOR_LASER_ACTIVE;
        _controllerDisplayLaser.GetComponent<MeshRenderer>().material.SetColor( "_EmissionColor", COLOR_LASER_ACTIVE );
    }

    private void ControllerCalibrationCompleteHandler()
    {
        _controllerDisplayLaser.GetComponent<MeshRenderer>().material.color = COLOR_LASER_ACTIVE;
        _controllerDisplayLaser.GetComponent<MeshRenderer>().material.SetColor( "_EmissionColor", COLOR_LASER_ACTIVE );
    }


    //
    // HandheldControllerBridge Button Down/Up Handlers
    //

    private void HomeBtnDownHandler()
    {
        // Calibrate the controller's orientation with a delay
        // Point the controller and laser in the direction of the calibration laser when it appears. 
        _controllerBridge.BeginControllerCalibration();

        AddStatusText( "Calibrating" );
    }

    private void HomeBtnUpHandler()
    {
        // Cancel the calibration of the controller's orientation.
        _controllerBridge.CancelControllerCalibration();

        AddStatusText( "Calibration Cancelled" ); 
    }


    //
    // HandheldControllerBridge BLE activity action handlers
    // 

    private void BLEInactiveHandler()
    {
        AddStatusText( "Inactive" );
    }

    private void BLEActiveHandler()
    {
        AddStatusText( "Active" );
    }

    private void BLEScanningHandler()
    {
        AddStatusText( "Scanning" );
    }

    private void BLEScanCompleteHandler()
    {
        AddStatusText( "Scan Complete" );
    }

    private void BLEConnectingHandler()
    {
        AddStatusText( "Connecting" );
    }

    private void BLEConnectionCompleteHandler()
    {
        AddStatusText( "Connected" );
    }

    private void AddStatusText( string pString )
    {
        _statusLabel.text = pString + "\n" + _statusLabel.text;
    }
}