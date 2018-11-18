using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using WSAControllerPlugin;

/// <summary>
/// A sample MonoBehaviour class that demonstrates the use of the <see cref="HandheldControllerBridge"/> for connecting a Bluetooth Low Energy handheld controller device with a HoloLens application built with Unity. 
/// This sample demonstrates 2 options for establishing a connection with the handheld controller device: <br />1) Automatic connection is used when you anticipate that only one Handheld Controller is in the area. It automatically connects
/// with one of the handheld controller devices within connection range. <br />2) Manual connection. The user initiates a BLE scan to locate nearby handheld controller device, then chooses one of the discovered
/// devices and initializes a connection with the selected device.<br/><br/>
/// Once a handheld controller device is connected the user calibrates it by pressing the Home button while pointing the device in the Forward direction. The on-screen controller and laser are oriented towards the Forward direction
/// during calibration to aid the user in pointing the controller towards the desired direction for calibration. The laser turns yellow for the delay period before calibration happens, then turns green once calibration has happened.<br/><br/>
/// Debug values are output to the screen. The debug values may be hidden and shown by pressing the App button on the controller. The measured Unity FPS will persist on screen. Press the App button a second time 
/// to hide the FPS display as well. Click the App button a third time to return to the default display of all debug information.<br /><br />
/// Balloons are popped by pointing the laser pointer at them. The *pop* sound may be enabled/disabled with the Volume Plus/Volume Minus buttons on the controller.<br /><br />
/// The on-screen controller may be placed at any position relative to the HoloLens headset and as the user moves throughout space the controller will remain positioned with the soecified offset relative to the headset and the application's Forward direction. 
/// The on-screen controller does not rotate with the user's body. 
/// Several default on-screen controller offset positions have been provided: Right hand near the hip and slightly in front of the body, Left hand near the hip and slightly in front of the body, and 2 positions with the controller shown in the field of view. 
/// </summary>
public class MainController : MonoBehaviour
{
    public static Color COLOR_LASER_ACTIVE = Color.green;
    public static Color COLOR_LASER_CALIBRATING = Color.yellow;

    /// HandheldControllerBridge Game Object that has a HandheldControllerBridge component and communicates with the ControllerPlugin. 
    public GameObject handheldControllerBridge;

    /// The number of balloons to display.
    public int balloonCount = 1;
    /// The scale of the balloons.
    public float balloonScale = 0.3f;
    private List<Balloon> _balloonScripts;

    // The HandheldControllerBridge component Monobehaviour class instance
    private HandheldControllerBridge _controllerBridge;

    private Text _statusLabel;
    private Button _autoConnectBtn;
    private Button _scanBtn;
    private GameObject _dropDownGO;
    private Dropdown _devicesDropdown;
    private Button _connectBtn;
    private Button _disconnectBtn;
    private Text _autoConnectStatusLabel;
   
    private string _statusMsgStart = "Scan for Devices";
    private string _statusMsgInactive = "Inactive";
    private string _statusMsgActive = "Active";
    private string _statusMsgScanning = "Scanning...";
    private string _statusMsgScanComplete = "Select a Device";
    private string _statusMsgDeviceSelected = "Click Connect to connect to the selected device.";
    private string _statusMsgConnecting = "Connecting...";
    private string _statusMsgConnected = "Connected";

    private string _dropdownBlankEntryLabel = "None";
    private string _dropdownCaptionStart = "Device List";
    private string _dropdownCaptionSelect = "Select a Device";

    private GameObject _fpsCanvas;
    private GameObject _handheldControllerCanvas;
    private GameObject _controllerBridgeCanvas;
    private Text[] _handheldControllerValues;

    /// The on-screen GameObject representing the handheld controller device in the scene.
    public GameObject controllerDisplay;

    private int _mainBtnClickCount = 0;
    private int _touchCount = 0;
    private int _appBtnClickCount = 0;
    private int _homeBtnClickCount = 0;
    private int _volPlusBtnClickCount = 0;
    private int _volMinusBtnClickCount = 0;

    // Offset for the on-screen hadheld controller Game Object
    private Vector3 _positionOffsetViewportBottomLeft = new Vector3( -0.2f, -0.5f, 2f );
    private Vector3 _positionOffsetViewportBottomRight = new Vector3( 0.2f, -0.25f, 1f );
    private Vector3 _positionOffsetRightHand = new Vector3( 0.2f, -0.5f, 0.2f );
    private Vector3 _positionOffsetLeftHand = new Vector3( -0.2f, -0.6f, 0 );
    private Vector3 _controllerOffsetPosition;

    // Indicator for showing several display settings for the UI canvases. Cycles between 0, 1, 2.
    private int _canvasShowSwitch = 0;
    
    // Laser
    private RaycastHit _hit;
    private Ray _ray = new Ray();
    private GameObject _controllerDisplayLaser;
    
    /// The left boundary of the border for balloons
    public float xPosMin = -5f;
    /// The right boundary of the border for balloons
    public float xPosMax = 5f;
    /// The bottom boundary of the border for balloons
    public float yPosMin = 0f;
    /// The top boundary of the border for balloons. 0 is the vertical location of the headset. 
    public float yPosMax = 3;
    /// The forward boundary of the border for balloons, farthest from the camera
    public float zPosMin = 6f;
    /// The forward boundary of the border for balloons, closest to the camera
    public float zPosMax = 15f;
    /// The minimum vertical velocity for balloons. Positive numbers for upwards movement.
    public float yVelMin = 0.5f;
    /// The maximum vertical velocity for balloons. Positive numbers for upwards movement.
    public float yVelMax = 1;

    // Measuring the achieved framerate
    private int _frameCount = 0;
    private float _measuredFPS = 0;
    private float _frameTimeLast;
    private float _frameTimeNext;

    void Start()
    {
        Application.targetFrameRate = 60;

        _controllerOffsetPosition = _positionOffsetRightHand;

        // Get a reference to the HandheldControllerBridge Monobehaviour
        _controllerBridge = handheldControllerBridge.GetComponent<HandheldControllerBridge>();
        
        // OPTIONAL: How many data packages to retain. Set to 0 if you only want to have access to the most recent data package received from the controller. 
        // _controllerBridge.SetDataStorageSize( 10 );

        // OPTIONAL: Override the deafult calibration time here.
        // _controllerBridge.SetCalibrationDelayTime( 4f );

        // OPTIONAL: override Service and Characteristic UUIDs here:
        // _controllerBridge.SetBLEService( "0000fe55-0000-1000-8000-00805f9b34fb" );
        // _controllerBridge.SetBLECharacteristic( "00000001-1000-1000-8000-00805f9b34fb" );

        /////////////////////////

        // Used for calculating the achieved FPS
        _frameTimeLast = Time.time;
        _frameTimeNext = Time.time + 1f;

        // Configure the Canvas UIs
        _controllerBridgeCanvas = GameObject.Find( "Canvas" );

        _autoConnectBtn = _controllerBridgeCanvas.transform.Find( "AutoConnectButton" ).GetComponent<Button>();
        _autoConnectBtn.onClick.AddListener( AutoConnectBtnClickedHandler );
        _autoConnectBtn.enabled = true;

        _fpsCanvas = GameObject.Find( "CanvasFPS" );
        _autoConnectStatusLabel = _fpsCanvas.transform.Find( "FPSLabel" ).GetComponent<Text>();
        _autoConnectStatusLabel.text = "FPS: Default";

        _statusLabel = _controllerBridgeCanvas.transform.Find("StatusMsg").GetComponent<Text>();
        _statusLabel.text = _statusMsgStart;

        _scanBtn = _controllerBridgeCanvas.transform.Find("ScanButton").GetComponent<Button>();
        _scanBtn.onClick.AddListener( ScanBtnClickedHandler );
        _scanBtn.enabled = true;

        _dropDownGO = _controllerBridgeCanvas.transform.Find("Dropdown").gameObject;
        _devicesDropdown = _dropDownGO.GetComponent<Dropdown>();
        _devicesDropdown.captionText.text = _dropdownCaptionStart;
        _devicesDropdown.onValueChanged.AddListener( DevicesDropDownSelectedHandler );
        _devicesDropdown.enabled = false;

        _connectBtn = _controllerBridgeCanvas.transform.Find("ConnectButton").GetComponent<Button>();
        _connectBtn.onClick.AddListener( ConnectBtnClickedHandler );
        _connectBtn.enabled = false;

        _disconnectBtn = _controllerBridgeCanvas.transform.Find("DisconnectButton").GetComponent<Button>();
        _disconnectBtn.onClick.AddListener( DisconnectBtnClickedHandler );
        _disconnectBtn.enabled = true;
        
        _handheldControllerCanvas = GameObject.Find("CanvasResults");
        _handheldControllerValues = _handheldControllerCanvas.GetComponentsInChildren<Text>();
        int i;
        for (i = 0; i < 22; i++)
        {  
            _handheldControllerValues[i].text  = string.Format("default: {0}", i);
        }

        _balloonScripts = new List<Balloon>();
        for( i = 0; i < balloonCount; i++ )
        {
            GameObject tBalloon = Instantiate(Resources.Load<GameObject>("Balloon"));
            Balloon tBalloonScript = tBalloon.GetComponent<Balloon>();
            tBalloonScript.InitializeValues(xPosMin, xPosMax, yPosMin, yPosMax, zPosMin, zPosMax, yVelMin, yVelMax);
            StartCoroutine( tBalloonScript.ResetBalloon( false ) );
            tBalloonScript.CustomizeInitialAppearance( balloonScale );
            _balloonScripts.Add( tBalloonScript );
        }

        _controllerDisplayLaser = controllerDisplay.transform.Find("beam").gameObject;

        // Subscribe to the Up/Down button Actions
        _controllerBridge.TouchBtnDown += TouchBtnDownHandler;
        _controllerBridge.TouchBtnUp += TouchBtnUpHandler;
        _controllerBridge.MainBtnDown += MainBtnDownHandler;
        _controllerBridge.MainBtnUp += MainBtnUpHandler;
        _controllerBridge.AppBtnDown += AppBtnDownHandler;
        _controllerBridge.AppBtnUp += AppBtnUpHandler;
        _controllerBridge.AppBtnUp += ChangeCanvasDisplayHandler;
        _controllerBridge.HomeBtnDown += HomeBtnDownHandler;
        _controllerBridge.HomeBtnUp += HomeBtnUpHandler;
        _controllerBridge.VolPlusBtnDown += VolPlusBtnDownHandler;
        _controllerBridge.VolPlusBtnUp += VolPlusBtnUpHandler;
        _controllerBridge.VolMinusBtnDown += VolMinusBtnDownHandler;
        _controllerBridge.VolMinusBtnUp += VolMinusBtnUpHandler;

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
    }

    private void AutoConnectBtnClickedHandler()
    {
        _statusLabel.text = _statusMsgConnecting;

        _controllerBridge.AutoConnect();
    }

    private void ScanBtnClickedHandler()
    {
        _statusLabel.text = _statusMsgScanning;

        _controllerBridge.Scan( false );
    }

    // Only called if there are 1 or more devices found after pressing Scan. 
    private void SetDropDownEntries()
    {
        _devicesDropdown.ClearOptions();
        List<string> tDeviceNames = new List<string>();

        if ( _controllerBridge.GetControllerDevices().Count == 1)
        {
            tDeviceNames.Add( _controllerBridge.GetControllerDevices()[0].name );

            Debug.Log( _controllerBridge.GetControllerDevices()[ 0 ].deviceId );

            _devicesDropdown.AddOptions(tDeviceNames);
            
            _devicesDropdown.enabled = false;

            DevicesDropDownSelectedHandler(1);
        }
        else 
        {
            // To prevent auto-selection of the first item in the list add a blank item at the top of the list when
            // more than one device was found
            Dropdown.OptionData tLabelEmpty = new Dropdown.OptionData();
            tLabelEmpty.text = _dropdownBlankEntryLabel;
            _devicesDropdown.options.Add(tLabelEmpty);

            foreach ( ControllerDevice tDevice in _controllerBridge.GetControllerDevices() )
            {
                tDeviceNames.Add(tDevice.name);
            }
            _devicesDropdown.AddOptions(tDeviceNames);

            _devicesDropdown.captionText.text = _dropdownCaptionSelect;

            _devicesDropdown.enabled = true;
        }
    }

    private void DevicesDropDownSelectedHandler(int pSelectedIndex)
    {
        _connectBtn.enabled = true;

        _statusLabel.text = _statusMsgDeviceSelected;
    }

    private void ConnectBtnClickedHandler()
    {
        int tSelectedIndex;

        // When there was only one device detected we didn't push a spaceholder item into the dropdown,
        // so no need to adjust the index reference
        if ( _devicesDropdown.options.Count == 1 )
        {
            tSelectedIndex = _dropDownGO.GetComponent<Dropdown>().value;
        }
        else
        { 
            tSelectedIndex = _dropDownGO.GetComponent<Dropdown>().value - 1;
        }

        _controllerBridge.SetSelectedControllerDevice( _controllerBridge.GetControllerDevices()[ tSelectedIndex ] );

        _statusLabel.text = _statusMsgConnecting;

        _connectBtn.enabled = false;
        _disconnectBtn.enabled = true;

        _controllerBridge.InitializeService();
    }

    private void DisconnectBtnClickedHandler()
    {
        _connectBtn.enabled = true;
        _disconnectBtn.enabled = false;

        _controllerBridge.DisconnectService();
    }

    void Update()
    {
        // Measure Unity's achieved FPS
        _frameCount++;
        if ( Time.time > _frameTimeNext )
        {
            _measuredFPS = _frameCount / ( Time.time - _frameTimeLast );
            _frameCount = 0;
            _frameTimeLast = Time.time;
            _frameTimeNext = Time.time + 1f;
        }

        DisplayControllerValues();
        
        // When orienting, rotate handheld controller display towards the Forward direction, otherwise rotate it according to the
        // rotation value stored in the ControllerPlugin.
        // Color changes of laser are handled elsewhere when event Actions on the HandheldControllerBridge are Invoked
        // Only change the color in response to Actions, not continually. 
        if ( !_controllerBridge.IsCalibrationDelayActive )
        {
            controllerDisplay.transform.localRotation = _controllerBridge.GetRotation(); 
        }
        else
        {
            controllerDisplay.transform.localRotation = Quaternion.identity;
        }
        
        // Move the on-screen controller when the headset moves 
        controllerDisplay.transform.position = Camera.main.transform.position + _controllerOffsetPosition;
       
        // Laser hit detection/reaction
        _ray.origin = controllerDisplay.transform.position;
        Vector3 tTargetDirection = controllerDisplay.transform.forward;
        _ray.direction = tTargetDirection;

        if ( Physics.Raycast( _ray, out _hit ) )
        {
            if ( _hit.collider.transform.parent.name == "Balloon(Clone)" )
            {
                GameObject tExplosion = (GameObject)Instantiate(Resources.Load("Ef_SparksParticle_01"), _hit.collider.transform.position, _hit.collider.transform.rotation);

                Gradient grad = new Gradient();
                Color tBalloonColor = _hit.collider.transform.GetComponentInParent<Balloon>().GetBalloonColor();
                grad.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(tBalloonColor, 0.15f), new GradientColorKey(tBalloonColor, 1.0f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0), new GradientAlphaKey(1.0f, 0.75f), new GradientAlphaKey(0.0f, 1.0f) }
                    );

                ParticleSystem.ColorOverLifetimeModule tColorOverLifetime = tExplosion.GetComponent<ParticleSystem>().colorOverLifetime;
                tColorOverLifetime.color = new ParticleSystem.MinMaxGradient( grad );
                tColorOverLifetime.enabled = true;
                
                Destroy(tExplosion, 3f);

                Balloon tBalloon = _hit.collider.transform.GetComponentInParent<Balloon>();
                // Reset with a delay so the sound plays in the current position. 
                StartCoroutine( tBalloon.ResetBalloon() );              
            }
        }
    }
    
    private void DisplayControllerValues()
    {
        // Debug value display
        _handheldControllerValues[ 17 ].text = "IsScanComplete: " + _controllerBridge.GetControllerPlugin().IsScanComplete + ", IsServiceInitialized: " + _controllerBridge.GetControllerPlugin().IsServiceInitialized;
        _handheldControllerValues[ 18 ].text = "BLE State: " + _controllerBridge.GetState();

        if( !_controllerBridge.IsBridgeInitialized )
        {
            return;
        }

        // values that require _controllerBridge to be initialized

        _autoConnectStatusLabel.text = "Measured FPS: " + _measuredFPS.ToString();
    
        // Time and sequence
        _handheldControllerValues[ 0 ].text = "Time: " + _controllerBridge.GetTime().ToString();
        _handheldControllerValues[ 1 ].text = "Sequence: " + _controllerBridge.GetSequence().ToString();

        // IMU sensor data
        _handheldControllerValues[ 2 ].text = string.Format( "Orientation: {0}, {1}, {2}", _controllerBridge.GetXOri(), _controllerBridge.GetYOri(), _controllerBridge.GetZOri() );
        _handheldControllerValues[ 3 ].text = string.Format( "Gyroscope: {0}, {1}, {2}", _controllerBridge.GetXGyro(), _controllerBridge.GetYGyro(), _controllerBridge.GetZGyro() );
        _handheldControllerValues[ 4 ].text = string.Format( "Accelerometer: {0}, {1}, {2}", _controllerBridge.GetXAcc(), _controllerBridge.GetYAcc(), _controllerBridge.GetZAcc() );
        double[] tOriScaled = _controllerBridge.GetOrientationScaled();
        _handheldControllerValues[ 5 ].text = string.Format( "Orientation Scaled: {0:0.0}, {1:0.0}, {2:0.0}", tOriScaled[ 0 ], tOriScaled[ 1 ], tOriScaled[ 2 ] );
        double[] tGyroScaled = _controllerBridge.GetGyroScaled();
        _handheldControllerValues[ 6 ].text = string.Format( "Gyroscope Scaled: {0:0.0}, {1:0.0}, {2:0.0}", tGyroScaled[ 0 ], tGyroScaled[ 1 ], tGyroScaled[ 2 ] );
        double[] tAccScaled = _controllerBridge.GetAccScaled();
        _handheldControllerValues[ 7 ].text = string.Format( "Acceleration Scaled: {0:0.0}, {1:0.0}, {2:0.0}", tAccScaled[ 0 ], tAccScaled[ 1 ], tAccScaled[ 2 ] );

        // Button states and events
        _handheldControllerValues[ 8 ].text = string.Format( "Touchpad. Touch count: {0}, is touching: {1}, Coords: {2}, {3}", _touchCount, _controllerBridge.IsBtnDown( ControllerPlugin.ControllerButtons.TOUCH_BTN ), _controllerBridge.GetXTouch(), _controllerBridge.GetYTouch() );
        _handheldControllerValues[ 9 ].text = string.Format( "Main Button. Click count: {0}, is pressed: {1}", _mainBtnClickCount, _controllerBridge.IsBtnDown( ControllerPlugin.ControllerButtons.MAIN_BTN ) );
        _handheldControllerValues[ 10 ].text = string.Format( "App Button. Click count: {0}, is pressed: {1}", _appBtnClickCount, _controllerBridge.IsBtnDown( ControllerPlugin.ControllerButtons.APP_BTN ) );
        _handheldControllerValues[ 11 ].text = string.Format( "Home Button. Click count: {0}, is pressed: {1}", _homeBtnClickCount, _controllerBridge.IsBtnDown( ControllerPlugin.ControllerButtons.HOME_BTN ) );
        _handheldControllerValues[ 12 ].text = string.Format( "Vol Plus Button. Click count: {0}, is pressed: {1}", _volPlusBtnClickCount, _controllerBridge.IsBtnDown( ControllerPlugin.ControllerButtons.VOL_PLUS_BTN ) );
        _handheldControllerValues[ 13 ].text = string.Format( "Vol Minus Button. Click count: {0}, is pressed: {1}", _volMinusBtnClickCount, _controllerBridge.IsBtnDown( ControllerPlugin.ControllerButtons.VOL_MINUS_BTN ) );

        // Extras
        _handheldControllerValues[ 14 ].text = "Raw Data: " + _controllerBridge.MostRecentRawDataString();
        _handheldControllerValues[ 15 ].text = "Device measurements per second: " + _controllerBridge.GetFramerateMeasurementResults().ToString();
        _handheldControllerValues[ 16 ].text = "State: " + _controllerBridge.GetState().ToString();

        _handheldControllerValues[ 19 ].text = "isBridgeInitialized: " + _controllerBridge.IsBridgeInitialized.ToString();
    }

    // Show/Hide the UI Canvases. 
    // One click hides the primary UI Canvas. Second click also hides the FPS Canvas. Third click makes both Canvases visible. 
    private void ChangeCanvasDisplayHandler()
    {
        _canvasShowSwitch++;
        if( _canvasShowSwitch > 2 )
        {
            _canvasShowSwitch = 0;
        }
        
        switch( _canvasShowSwitch )
        {
            case 0:
                {
                    _fpsCanvas.GetComponent<CanvasGroup>().alpha = 1;
                    _fpsCanvas.SetActive( true );

                    _controllerBridgeCanvas.GetComponent<CanvasGroup>().alpha = 1;
                    _controllerBridgeCanvas.SetActive( true );

                    _handheldControllerCanvas.GetComponent<CanvasGroup>().alpha = 1;
                    _handheldControllerCanvas.SetActive( true );
                }
                break;
            case 1:
                {
                    _fpsCanvas.GetComponent<CanvasGroup>().alpha = 1;
                    _fpsCanvas.SetActive( true );

                    _controllerBridgeCanvas.GetComponent<CanvasGroup>().alpha = 0;
                    _controllerBridgeCanvas.SetActive( false );

                    _handheldControllerCanvas.GetComponent<CanvasGroup>().alpha = 0;
                    _handheldControllerCanvas.SetActive( false );
                }
                break;
            case 2:
                {
                    _fpsCanvas.GetComponent<CanvasGroup>().alpha = 0;
                    _fpsCanvas.SetActive( false );

                    _controllerBridgeCanvas.GetComponent<CanvasGroup>().alpha = 0;
                    _controllerBridgeCanvas.SetActive( false );

                    _handheldControllerCanvas.GetComponent<CanvasGroup>().alpha = 0;
                    _handheldControllerCanvas.SetActive( false );
                }
                break;
            default:
                {
                    _fpsCanvas.GetComponent<CanvasGroup>().alpha = 0;
                    _fpsCanvas.SetActive( false );

                    _controllerBridgeCanvas.GetComponent<CanvasGroup>().alpha = 1;
                    _controllerBridgeCanvas.SetActive( true );

                    _handheldControllerCanvas.GetComponent<CanvasGroup>().alpha = 1;
                    _handheldControllerCanvas.SetActive( true );
                }
                break;
        }
    }
    
    // Enable or disable pop audio for all balloons
    private void EnableBalloonAudio( bool pEnable = true )
    {
        int i;
        for ( i = 0; i < balloonCount; i++ )
        {
            _balloonScripts[ i ].EnableAudio( pEnable );
        }
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
        _controllerDisplayLaser.GetComponent<MeshRenderer>().material.color = COLOR_LASER_ACTIVE ;
        _controllerDisplayLaser.GetComponent<MeshRenderer>().material.SetColor( "_EmissionColor", COLOR_LASER_ACTIVE );
    }


    //
    // HandheldControllerBridge Button Down/Up Handlers
    // 

    private void TouchBtnDownHandler()
    {
        _handheldControllerValues[ _handheldControllerValues.Length - 1 ].text = "Touch Button Down";
    }

    private void TouchBtnUpHandler()
    {
        _handheldControllerValues[ _handheldControllerValues.Length - 1 ].text = "Touch Button Up";

        _touchCount++;
    }

    private void MainBtnDownHandler()
    {
        _handheldControllerValues[ _handheldControllerValues.Length - 1 ].text = "Main Button Down";
    }

    private void MainBtnUpHandler()
    {
        _handheldControllerValues[ _handheldControllerValues.Length - 1 ].text = "Main Button Up";

        _mainBtnClickCount++;
    }

    private void HomeBtnDownHandler()
    {
        _handheldControllerValues[ _handheldControllerValues.Length - 1 ].text = "Home Button Down";

        // Calibrate the controller's orientation with a delay
        _controllerBridge.BeginControllerCalibration();

        // Calibrate the controller's orientation immediately
        // _controllerBridge.SetControllerCalibration();
    }

    private void HomeBtnUpHandler()
    {
        _handheldControllerValues[ _handheldControllerValues.Length - 1 ].text = "Home Button Up";

        _homeBtnClickCount++;

        // Cancel the calibration of the controller's orientation.
        _controllerBridge.CancelControllerCalibration();
    }

    private void AppBtnDownHandler()
    {
        _handheldControllerValues[ _handheldControllerValues.Length - 1 ].text = "App Button Down";
    }

    private void AppBtnUpHandler()
    {
        _handheldControllerValues[ _handheldControllerValues.Length - 1 ].text = "App Button Up";

        _appBtnClickCount++;
    }

    private void VolMinusBtnDownHandler()
    {
        _handheldControllerValues[ _handheldControllerValues.Length - 1 ].text = "Vol Down Button Down";
    }

    private void VolMinusBtnUpHandler()
    {
        _handheldControllerValues[ _handheldControllerValues.Length - 1 ].text = "Vol Down Button Up";
        EnableBalloonAudio( false );
        
        _volMinusBtnClickCount++;
    }

    private void VolPlusBtnDownHandler()
    {
        _handheldControllerValues[ _handheldControllerValues.Length - 1 ].text = "Vol Up Button Down";
    }

    private void VolPlusBtnUpHandler()
    {
        _handheldControllerValues[ _handheldControllerValues.Length - 1 ].text = "Vol Up Button Up";
        EnableBalloonAudio( true );

        _volPlusBtnClickCount++;
    }

    
    //
    // HandheldControllerBridge BLE activity action handlers
    // 

    private void BLEInactiveHandler()
    {
        _statusLabel.text = _statusMsgInactive;
    }
    
    private void BLEActiveHandler()
    {
        _statusLabel.text = _statusMsgActive;
    }

    private void BLEScanningHandler()
    {
        _statusLabel.text = _statusMsgScanning;
    }

    private void BLEScanCompleteHandler()
    {
        _statusLabel.text = _statusMsgScanComplete;

        // Set drop down entries
        if ( _controllerBridge.GetControllerDevices().Count > 0 )
        {
            _statusLabel.text = _statusMsgScanComplete;

            SetDropDownEntries();
        }
    }

    private void BLEConnectingHandler()
    {
        _statusLabel.text = _statusMsgConnecting;
    }

    private void BLEConnectionCompleteHandler()
    {
        _statusLabel.text = _statusMsgConnected;
    }
}