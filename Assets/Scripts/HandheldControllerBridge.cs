using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using WSAControllerPlugin;

// Cube Space Effects particle effects: https://www.assetstore.unity3d.com/en/#!/content/74319
// Blop sound effect by Mark DiAngelo: http://soundbible.com/2067-Blop.html
// Controller model:
// TechnoBuddhist https://github.com/TechnoBuddhist/VR-Controller-Daydream
// RJE https://sketchfab.com/models/c1944c64e06544babc90e9d0aa953551#

/// <summary>
/// Handles communication between a HandheldController plugin for a Bluetooth Low Energy controller and a Unity application. 
/// </summary>
public class HandheldControllerBridge : MonoBehaviour
{
    /// Set to true once basic initialization has been completed. Used to block unsafe operations before the class is ready. 
    [HideInInspector]
    public bool IsBridgeInitialized = false;

    /// True after the orientation calibration delay is started and while we're waiting for the <see cref="_calibrationDelayTime"/> to expire and the 
    /// calibration to be completed. False once calibration has been performed. 
    [HideInInspector]
    public bool IsCalibrationDelayActive = false;

    // 
    // BLE State Actions
    // 

    /// Invoked when the Plugin's Bluetooth configuration becomes inactive.
    public event Action BLEInactive;

    /// Invoked when the Plugin is scanning for handheld controller devices to connect with.
    public event Action BLEScanning;

    /// Invoked when the Plugin has finished scanning for handheld controller devices to connect with.
    public event Action BLEScanComplete;

    /// Invoked when the Plugin is connecting with a handheld controller device.
    public event Action BLEConnecting;

    /// Invoked when the Plugin has finished connecting with a handheld controller device.
    public event Action BLEConnectionComplete;

    /// Invoked when the Plugin is connected with a handheld controller device and is actively relaying the data from the handheld controller device.
    public event Action BLEActive;

    // 
    // Button Down/Up Actions
    // 

    /// <summary>
    /// The handheld controller device's touchpad was touched. This is invoked when there is finger contact with the touchpad not when 
    /// the touchpad button is pressed down. Invoked once on the first frame the touchpad state was detected.
    /// </summary>
    public event Action TouchBtnDown;

    /// The handheld controller device's touchpad was released. This is invoked when there is no loner finger contact with the touchpad not when 
    /// the touchpad button is released. Invoked once on the first frame the touchpad state was detected.
    public event Action TouchBtnUp;

    /// The handheld controller device's main button (touchpad) was pressed down. Invoked once on the first frame the button state was detected.
    public event Action MainBtnDown;

    /// The handheld controller device's main button (touchpad) was released. Invoked once on the first frame the button state was detected.
    public event Action MainBtnUp;

    /// The handheld controller device's home button was pressed down. Invoked once on the first frame the button state was detected.
    public event Action HomeBtnDown;

    /// The handheld controller device's home button was released. Invoked once on the first frame the button state was detected.
    public event Action HomeBtnUp;

    /// The handheld controller device's app button was pressed down. Invoked once on the first frame the button state was detected.
    public event Action AppBtnDown;

    /// The handheld controller device's app button was released. Invoked once on the first frame the button state was detected.
    public event Action AppBtnUp;

    /// The handheld controller device's volume Plus (Volume Up) button was pressed down (depressed). Invoked once on the first frame the button state was detected.
    public event Action VolMinusBtnDown;

    /// The handheld controller device's volume Plus (Volume Up) button was released. Invoked once on the first frame the button state was detected.
    public event Action VolMinusBtnUp;

    /// The handheld controller device's volume Minus (Volume Down) button was pressed down (depressed). Invoked once on the first frame the button state was detected.
    public event Action VolPlusBtnDown;

    /// The handheld controller device's volume Minus (Volume Down) button was released. Invoked once on the first frame the button state was detected.
    public event Action VolPlusBtnUp;

    /// The delayed orientation calibration began
    public event Action DelayedCalibrationBegan;

    /// The delayed orientation calibration was cancelled
    public event Action DelayedCalibrationCancelled;

    /// The orientation calibration occurred
    public event Action CalibrationComplete;
    
    // The HandheldController device that was selected for connecting with. After scanning for devices has completed use GetControllerDevices to retrieve the full List of ControllerDevices that were found.
    // private ControllerDevice _selectedControllerDevice;
    private ControllerPlugin _controllerPlugin;
    private BLEStates _state = BLEStates.inactive;

    // The orientation of the handheld controller in space. It has been scaled and prepared as a Quaternion. Set a GameObject's transform.localRotation property to this and it will match the orientation of the controller. 
    // The handheld controller's orientation will drift and should be re-calibrated occasionally.
    private Quaternion _rotation = Quaternion.identity;
    // A Quaternion representing the controller's orientation in space without the calibrated offset applied. 
    private Quaternion _controllerPoseInSensorSpace = Quaternion.identity;
    // A Quatarnion that is stored when the orientation is calibrated. 
    private Quaternion _handheldControllerOrientationOffset = Quaternion.identity;
    // The orientation values from the HandheldController after being multiplied by a scaling value. 
    private Vector3 _oriVector = new Vector3();
    // The Accelerometer values from the HandheldController after being multiplied by a scaling value. 
    private Vector3 _accVector = new Vector3();
    // The Gyroscope values from the HandheldController after being multiplied by a scaling value. 
    private Vector3 _gyroVector = new Vector3();
    // The finger-contact/no finger-contact state of the touchpad.
    private bool _touchBtnDown = false;
    private bool _mainBtnDown = false;
    private bool _appBtnDown = false;
    private bool _homeBtnDown = false;
    private bool _volMinusBtnDown = false;
    private bool _volPlusBtnDown = false;
    // The number of seconds that a user should hold the orientation calibration button before the calibration is performed. 
    private float _calibrationDelayTime = 1.0f;


    void Awake()
    {
        _controllerPlugin = new ControllerPlugin();

        ResetButtonStates();
    }

    /// <summary>
    /// Scan for handheld controller devices and automatically connect with one of them. 
    /// Primarily for use when there's only one handheld controller device advertising in the area. 
    /// No sorting of devices is performed. If multiple HandheldController devices are in the area
    /// it is preferable to scan for devices using <see cref="Scan"/> then choose from among the discovered devices to connect with.</summary>
    public void AutoConnect()
    {
        Scan( true );
    }

    /// <summary>
    /// Scan for nearby handheld controller devices.  
    /// </summary>
    /// <param name="pAutoConnect">After Scanning has completed, automatically choose the first device in the list and complete the BLE connection process.
    /// This is useful if you only expect there to be one device in the area.</param>
    public void Scan( bool pAutoConnect = true )
    {
        _controllerPlugin.Scan( pAutoConnect );

        // If the ControllerPlugin was set to AutoConnect, set BLE State to Connecting, otherwise Scanning.
        if ( pAutoConnect )
        {
            SetBLEState( BLEStates.connecting );
        }
        else
        {
            SetBLEState( BLEStates.scanning );
        }

        IsBridgeInitialized = true;
    }

    /// <summary>
    /// After scanning for handheld controller devices and selecting one of them with <see cref="SetSelectedControllerDevice(ControllerDevice)"/>, call InitializeService to connect to the selected device.  
    /// </summary>
    /// <returns>True if there was a device to connect with and the initialization was attempted. False if there were no devices to connect with in the <see cref="GetControllerDevices"/> List.</returns>
    public bool InitializeService()
    {
        if ( !IsBridgeInitialized )
        {
            return false;
        }
        
        bool tSuccess = _controllerPlugin.InitializeService();

        if ( tSuccess )
        {
            SetBLEState( BLEStates.connecting );
        }

        return tSuccess; 
    }

    /// Disconnect from the active handheld controller device. 
    public void DisconnectService()
    {
        if ( !IsBridgeInitialized )
        {
            return;
        }
        
        _controllerPlugin.DisconnectService();

        SetBLEState( BLEStates.inactive );
    }
 
    void Update()
    {
        if ( !IsBridgeInitialized )
        {
            return;
        }

        switch ( _state )
        {
            case BLEStates.inactive:
                {
                    // do nothing
                }
                break;
            case BLEStates.active:
                {
                    // Update button status
                    HandleControllerButtons();
                    // Set orientation for use by a Game Object
                    SetRotationQuaternion();
                }
                break;
            case BLEStates.scanning:
                {
                    if ( _controllerPlugin.IsScanComplete )
                    {
                        SetBLEState( BLEStates.scanComplete );
                    }
                }
                break;
            case BLEStates.scanComplete:
                {
                    // empty
                }
                break;
            case BLEStates.connecting:
                {
                    if ( _controllerPlugin.IsServiceInitialized )
                    {
                        SetBLEState( BLEStates.active );
                    }
                }
                break;
            case BLEStates.connectionComplete:
                {
                    // empty
                }
                break;
            default:
                break;
        }
    }

    /// Sets _state and Invokes state change Actions. Used in conjunction with Update to manage the BLE connection. 
    private void SetBLEState( BLEStates pState )
    {
        // Return if the state is already set to the specified state
        if ( _state == pState )
        {
            return;
        }

        _state = pState;

        switch ( _state )
        {
            case BLEStates.inactive:
                {
                    if ( BLEInactive != null )
                    {
                        BLEInactive();
                    }
                }
                break;
            case BLEStates.active:
                {
                    if ( BLEActive != null )
                    {
                        BLEActive();
                    }
                }
                break;
            case BLEStates.scanning:
                {
                    if ( BLEScanning != null )
                    {
                        BLEScanning();
                    }
                }
                break;
            case BLEStates.scanComplete:
                {
                    if ( BLEScanComplete != null )
                    {
                        BLEScanComplete();
                    }
                }
                break;
            case BLEStates.connecting:
                {
                    if ( BLEConnecting != null )
                    {
                        BLEConnecting();
                    }
                }
                break;
            case BLEStates.connectionComplete:
                {
                    if ( BLEConnectionComplete != null )
                    {
                        BLEConnectionComplete();
                    }
                }
                break;
            default:
                break;
        }
    }

    // Update the _rotation Quaternion for use by any object that wants to reference the calibrated orientation of the controller in application space.
    private void SetRotationQuaternion()
    {
        double[] tOrientation = _controllerPlugin.GetOrientationScaled();
        _oriVector.Set( (float)-tOrientation[ 0 ], (float)-tOrientation[ 1 ], (float)tOrientation[ 2 ] );

        float w = _oriVector.magnitude;

        // Store local orientation for use when setting offset
        _controllerPoseInSensorSpace = Quaternion.AngleAxis( w * Mathf.Rad2Deg, _oriVector );

        // Apply calibrated offset
        _rotation = _handheldControllerOrientationOffset * _controllerPoseInSensorSpace;
    }

    // Manage the Down/Up state of the Handheld Controller's buttons and invoke Actions for the Down/Up states of each button.
    // Each button is Down or Up for one Unity frame. 
    private void HandleControllerButtons()
    {
        // Touch Button
        if ( _controllerPlugin.IsBtnDown( ControllerPlugin.ControllerButtons.TOUCH_BTN ) && !_touchBtnDown )
        {
            _touchBtnDown = true;
            TouchBtnDown();
        }

        if ( !_controllerPlugin.IsBtnDown( ControllerPlugin.ControllerButtons.TOUCH_BTN ) && _touchBtnDown )
        {
            _touchBtnDown = false;
            TouchBtnUp();
        }

        // Main Button
        if ( _controllerPlugin.IsBtnDown( ControllerPlugin.ControllerButtons.MAIN_BTN ) && !_mainBtnDown )
        {
            _mainBtnDown = true;
            MainBtnDown();
        }

        if ( !_controllerPlugin.IsBtnDown( ControllerPlugin.ControllerButtons.MAIN_BTN ) && _mainBtnDown )
        {
            _mainBtnDown = false;
            MainBtnUp();
        }

        // App Button
        if ( _controllerPlugin.IsBtnDown( ControllerPlugin.ControllerButtons.APP_BTN ) && !_appBtnDown )
        {
            _appBtnDown = true;
            AppBtnDown();
        }

        if ( !_controllerPlugin.IsBtnDown( ControllerPlugin.ControllerButtons.APP_BTN ) && _appBtnDown )
        {
            _appBtnDown = false;
            AppBtnUp();
        }

        // Home Button
        if ( _controllerPlugin.IsBtnDown( ControllerPlugin.ControllerButtons.HOME_BTN ) && !_homeBtnDown )
        {
            _homeBtnDown = true;
            HomeBtnDown();
        }

        if ( !_controllerPlugin.IsBtnDown( ControllerPlugin.ControllerButtons.HOME_BTN ) && _homeBtnDown )
        {
            _homeBtnDown = false;
            HomeBtnUp();
        }

        // Volume Minus Button 
        if ( _controllerPlugin.IsBtnDown( ControllerPlugin.ControllerButtons.VOL_MINUS_BTN ) && !_volMinusBtnDown )
        {
            _volMinusBtnDown = true;
            VolMinusBtnDown();
        }

        if ( !_controllerPlugin.IsBtnDown( ControllerPlugin.ControllerButtons.VOL_MINUS_BTN ) && _volMinusBtnDown )
        {
            _volMinusBtnDown = false;
            VolMinusBtnUp();
        }

        // Volume Plus Button
        if ( _controllerPlugin.IsBtnDown( ControllerPlugin.ControllerButtons.VOL_PLUS_BTN ) && !_volPlusBtnDown )
        {
            _volPlusBtnDown = true;
            VolPlusBtnDown();
        }

        if ( !_controllerPlugin.IsBtnDown( ControllerPlugin.ControllerButtons.VOL_PLUS_BTN ) && _volPlusBtnDown )
        {
            _volPlusBtnDown = false;
            VolPlusBtnUp();
        }
    }

    /// Begin calibration of the controller's orientation. Once the specified delay period has elapsed, the calibration will be performed. 
    /// Set <see cref="_calibrationDelayTime"/> before calling this to customize the delay time before the <see cref="SetControllerCalibration"/> method is called to perform the calibration.
    public void BeginControllerCalibration()
    {
        if ( !IsBridgeInitialized )
        {
            return;
        }

        if ( !IsInvoking( "SetControllerCalibration" ) )
        {
            Invoke( "SetControllerCalibration", _calibrationDelayTime );

            IsCalibrationDelayActive = true;

            DelayedCalibrationBegan();
        }
    }

    /// Cancel calibration that was begun with <see cref="BeginControllerCalibration"/> if the delay time hasn't elapsed yet. 
    public void CancelControllerCalibration()
    {
        if ( !IsBridgeInitialized )
        {
            return; 
        }

        if ( IsInvoking( "SetControllerCalibration" ) )
        {
            CancelInvoke( "SetControllerCalibration" );

            IsCalibrationDelayActive = false;

            DelayedCalibrationCancelled();
        }
    }

    /// <summary>
    /// Calibrate the orientation of the handheld controller device. Call directly or call with a delay using <see cref="BeginControllerCalibration"/>. 
    /// The handheld controller device should be pointed in the application's Forward direction when this method is called. 
    /// </summary> 
    public void SetControllerCalibration()
    {
        _handheldControllerOrientationOffset = Quaternion.Inverse( _controllerPoseInSensorSpace );

        // In case the orientation calibration delay was active and this was called directly
        CancelInvoke( "SetControllerCalibration" );

        IsCalibrationDelayActive = false;

        CalibrationComplete();
    }

    /// <summary>
    /// Retrieve a string representation of the most recent byte array data (raw data) received from the handheld controller device. 
    /// </summary>
    /// <param name="pSeparator">String that appears between each byte in the array.</param>
    /// <returns>Representation of the most recent byte array received from the handheld controller. 
    /// Each byte formatted with the default ToString method's formatting:<br />
    /// <a href="https://msdn.microsoft.com/en-us/library/xd12z8ts(v=vs.110).aspx" target="_blank"/>https://msdn.microsoft.com/en-us/library/xd12z8ts(v=vs.110).aspx</a></returns>
    public string MostRecentRawDataString( string pSeparator = "-" )
    {
        if ( !IsBridgeInitialized )
        {
            return "";
        }

        byte[] tBytes = _controllerPlugin.GetMostRecentRawData();
        int tBytesLength = tBytes.Length;
        string tReturn = "";
        for ( int i = 0; i < tBytesLength; i++ )
        {
            tReturn += tBytes[ i ].ToString();
            if ( i < tBytesLength - 1 )
            {
                tReturn += pSeparator;
            };
        }

        return tReturn;
    }

    // Set all button Down flags to false.
    private void ResetButtonStates()
    {
        _touchBtnDown = false;
        _mainBtnDown = false;
        _appBtnDown = false;
        _homeBtnDown = false;
        _volMinusBtnDown = false;
        _volPlusBtnDown = false;
    }




    //////////////////////////
    // Helpers  
    //////////////////////////

    /// <summary>
    /// Get the rotation of the handheld controller in space. It has been scaled and prepared as a Quaternion. 
    /// Set a GameObject's transform.localRotation property to this and it will match the rotation of the controller. 
    /// The rotation value will drift over time and should be re-calibrated occasionally with <see cref="SetControllerCalibration"/>.
    /// </summary>
    /// <returns>A Quaternion that may be used to set the transform.localRotation property of a Game Object.</returns>
    public Quaternion GetRotation()
    {
        return _rotation;
    }

    /// <returns>The ControllerDevice specified automatically during <see cref="AutoConnect"/> or manually by <see cref="SetSelectedControllerDevice(ControllerDevice)"/>.</returns>
    public ControllerDevice GetSelectedControllerDevice()
    {
        return _controllerPlugin.GetSelectedControllerDevice();
    }

    /// <summary>
    /// Set the ControllerDevice to connect with. 
    /// </summary>
    /// <param name="pControllerDevice">A ControllerDevice discovered during <see cref="Scan(bool)"/>.</param>
    public void SetSelectedControllerDevice( ControllerDevice pControllerDevice )
    {
        _controllerPlugin.SetSelectedControllerDevice( pControllerDevice );
    }

    /// <summary>
    /// The current BLE connection state. This is used to facilitate the connection with a handheld controller device. 
    /// </summary>
    public BLEStates GetState()
    {
        return _state;
    }

    /// The ControllerPlugin object that communicates directly with the handheld controller device. 
    public ControllerPlugin GetControllerPlugin()
    {
        return _controllerPlugin;
    }

    /// <summary>
    /// Limit how many data records from the handheld controller device the ControllerPlugin retains. Set to 0 to retain only the most recent byte array received. Set any number N that is greater than 0 to store the last N records received. Approximately 60 values are received from Handheld Controllers each second.  
    /// </summary>
    /// <param name="pRawDataSize">The Number of records to retain.</param>
    public void SetDataStorageSize( int pRawDataSize )
    {
        if ( _controllerPlugin == null )
        {
            return;
        }

        _controllerPlugin.SetDataStorageSize( pRawDataSize );
    }

    /// A List of the stored byte arrays received by the ControllerPlugin. Set the number or records stored with <see cref="SetDataStorageSize"/>.
    public List<byte[]> GetRawData()
    {
        if ( _controllerPlugin == null )
        {
            return null;
        }

        return _controllerPlugin.GetRawData();
    }

    /// Get the setting for the number of data packages that will be stored. 
    /// <returns>-1 if this is called before this class has been initialized.</returns>
    public int GetRawDataStorageSize()
    {
        if ( !IsBridgeInitialized )
        {
            return -1;
        }

        return _controllerPlugin.GetRawDataStorageSize();
    }

    /// <summary>Determine if a button on the handheld controller device is currently being held down.</summary>
    /// <param name="pBtn">Pass an ControllerPlugin.ControllerButtons enum to specify which button to check.</param>
    /// <returns>True if the specified button is currently held down. False if it is not currently being held down.</returns>
    public bool IsBtnDown( ControllerPlugin.ControllerButtons pBtn )
    {
        if ( !IsBridgeInitialized )
        {
            return false;
        }

        return _controllerPlugin.IsBtnDown( pBtn );
    }

    /// <summary>
    /// Call after running <see cref="Scan(bool)"/>. 
    /// </summary>
    /// <returns>A List of ControllerDevice objects representing the discovered handheld controller devices. Choose one of these to connect with.</returns>
    public List<ControllerDevice> GetControllerDevices()
    {
        if ( !IsBridgeInitialized )
        {
            return null;
        }

        return _controllerPlugin.GetControllerDevices();
    }

    /// <summary>
    /// The most recent Time value received from the handheld controller device.
    /// </summary>
    public int GetTime()
    {
        if ( !IsBridgeInitialized )
        {
            return 0;
        }

        return _controllerPlugin.GetTime();
    }

    /// <summary>
    /// The most recent Sequence value received from the handheld controller device.
    /// </summary>
    public int GetSequence()
    {
        if ( !IsBridgeInitialized )
        {
            return 0;
        }

        return _controllerPlugin.GetSequence();
    }

    /// <summary>
    /// The most recent X Orientation value received from the handheld controller device.
    /// </summary>
    public int GetXOri()
    {
        if ( !IsBridgeInitialized )
        {
            return 0;
        }

        return _controllerPlugin.GetXOri();
    }

    /// <summary>
    /// The most recent Y Orientation value received from the handheld controller device.
    /// </summary>
    public int GetYOri()
    {
        if ( !IsBridgeInitialized )
        {
            return 0;
        }

        return _controllerPlugin.GetYOri();
    }

    /// <summary>
    /// The most recent Z Orientation value received from the handheld controller device.
    /// </summary>
    public int GetZOri()
    {
        if ( !IsBridgeInitialized )
        {
            return 0;
        }

        return _controllerPlugin.GetZOri();
    }

    /// <summary>
    /// The most recent X Acceleration value received from the handheld controller device.
    /// </summary>
    public int GetXAcc()
    {
        if ( !IsBridgeInitialized )
        {
            return 0;
        }

        return _controllerPlugin.GetXAcc();
    }

    /// <summary>
    /// The most recent Y Acceleration value received from the handheld controller device.
    /// </summary>
    public int GetYAcc()
    {
        if ( !IsBridgeInitialized )
        {
            return 0;
        }

        return _controllerPlugin.GetYAcc();
    }

    /// <summary>
    /// The most recent Z Acceleration value received from the handheld controller device.
    /// </summary>
    public int GetZAcc()
    {
        if ( !IsBridgeInitialized )
        {
            return 0;
        }

        return _controllerPlugin.GetZAcc();
    }

    /// <summary>
    /// The most recent X Gyroscope value received from the handheld controller device.
    /// </summary>
    public int GetXGyro()
    {
        if ( !IsBridgeInitialized )
        {
            return 0;
        }

        return _controllerPlugin.GetXGyro();
    }

    /// <summary>
    /// The most recent Y Gyroscope value received from the handheld controller device.
    /// </summary>
    public int GetYGyro()
    {
        if ( !IsBridgeInitialized )
        {
            return 0;
        }

        return _controllerPlugin.GetYGyro();
    }

    /// <summary>
    /// The most recent Z Gyroscope value received from the handheld controller device.
    /// </summary>
    public int GetZGyro()
    {
        if ( !IsBridgeInitialized )
        {
            return 0;
        }

        return _controllerPlugin.GetZGyro();
    }

    /// <summary>
    /// The most recent X Touchpad value received from the handheld controller device. 
    /// </summary>
    /// <returns>Range is 0 - 255, however the touchpad is circular so corner values are never returned. Value will be 0 if there is no finger contact with the touchpad. </returns>
    public int GetXTouch()
    {
        if ( !IsBridgeInitialized )
        {
            return 0;
        }

        return _controllerPlugin.GetXTouch();
    }

    /// <summary>
    /// The most recent Y Touchpad value received from the handheld controller device. 
    /// </summary
    /// <returns>Range is 0 - 255, however the touchpad is circular so corner values are never returned. Value will be 0 if there is no finger contact with the touchpad. </returns>
    public int GetYTouch()
    {
        if ( !IsBridgeInitialized )
        {
            return 0;
        }

        return _controllerPlugin.GetYTouch();
    }

    /// <summary>
    /// The final byte in the byte array data received from handheld controller devices. The property it represents is not specified.
    /// </summary>
    public int GetByteTwenty()
    {
        if ( !IsBridgeInitialized )
        {
            return 0;
        }

        return _controllerPlugin.GetByteTwenty();
    }

    /// <summary>
    /// The raw Inertial Measurement Unit values received from the handheld controller device must be scaled before being used in Unity. This method scales the Orientation values. 
    /// </summary>
    /// <returns>X, Y and Z values are each scaled by this formula: ( 2f * Math.PI / 4095f ). Values in the returned array are [X, Y, Z].</returns>
    public double[] GetOrientationScaled()
    {
        if ( !IsBridgeInitialized )
        {
            return null;
        }

        return _controllerPlugin.GetOrientationScaled();
    }

    /// <summary>
    /// The raw Inertial Measurement Unit values received from the handheld controller device must be scaled before being used in Unity. This method scales the Acceleration values. 
    /// </summary>
    /// <returns>X, Y and Z values are each scaled by this formula: ( 8f * 9.8f / 4095f ). Values in the returned array are [X, Y, Z].</returns>
    public double[] GetAccScaled()
    {
        if ( !IsBridgeInitialized )
        {
            return null;
        }
        
        return _controllerPlugin.GetAccScaled();
    }

    /// <summary>
    /// The raw Inertial Measurement Unit values received from the handheld controller device must be scaled before being used in Unity. This method scales the Gyroscope values. 
    /// </summary>
    /// <returns>X, Y and Z values are each scaled by this formula: ( 2048f / 180f * Math.PI / 4095f ). Values in the returned array are [X, Y, Z].</returns>
    public double[] GetGyroScaled()
    {
        if ( !IsBridgeInitialized )
        {
            return null;
        }

        return _controllerPlugin.GetGyroScaled();
    }

    /// The most recent byte array data value received from the handheld controller device. 
    public byte[] GetMostRecentRawData()
    {
        if ( !IsBridgeInitialized )
        {
            return null;
        }

        return _controllerPlugin.GetMostRecentRawData();
    }

    /// <summary>
    /// The number of byte arrays received every second from the handheld controller device. This is calculated as an average of the last 10 seconds and is updated once every 10 seconds. This number may be higher than the framerate Unity achieves because the ControllerPlugin operates in a different thread than the main Unity thread. 
    /// </summary>
    /// <returns>The average number of data byte arrays received each second from the handheld controller device. </returns>
    public float GetFramerateMeasurementResults()
    {
        return _controllerPlugin.GetFramerateMeasurementResults();
    }

    /// Optional: Set the UUID for the service to look for in the device to something other than the default value. 
    /// <param name="pUUID">UUID string in the format: dddddddd-dddd-dddd-dddd-dddddddddddd</param>
    public void SetBLEService( string pServiceUUID )
    {
        _controllerPlugin.SetBLEService( pServiceUUID );
    }

    /// Optional: Set the BLE Characteristic to a value other than the default value. 
    /// <param name="pUUID">UUID string in the format: dddddddd-dddd-dddd-dddd-dddddddddddd</param>
    public void SetBLECharacteristic( string pCharacteristicUUID )
    {
        _controllerPlugin.SetBLECharacteristic( pCharacteristicUUID );
    }

    /// Resets the ControllerPlugin's BLE state tracking flags to false.   
    public void ResetBLEFlags()
    {
        _controllerPlugin.ResetBLEFlags();
    }

    /// Specify the number of seconds that a user should hold the calibration button before the calibration is performed. 
    public void SetCalibrationDelayTime( float tSeconds )
    {
        _calibrationDelayTime = tSeconds;
    }

    /// Get the calibration delay time setting. 
    public float GetCalibrationDelayTime()
    {
        return _calibrationDelayTime;
    }

}

/// <summary>
/// Enums representing the BLE connection state of the ControllerPlugin. 
/// </summary>
public enum BLEStates
{
    inactive,
    scanning,
    scanComplete,
    connecting,
    connectionComplete,
    active
}