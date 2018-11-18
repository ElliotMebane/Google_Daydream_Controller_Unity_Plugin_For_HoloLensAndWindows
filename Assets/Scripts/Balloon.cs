using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// More complex buoyancy, if you want: 
// https://forum.unity3d.com/threads/buoyancy-script.72974/

/// Balloon controller.
public class Balloon : MonoBehaviour { 

    // The primary body of the balloon.
    private GameObject _body;

    // The little nub part at the base of the balloon.
    private GameObject _nub;

    // The color of the balloon. This will be set using <see cref="GetRandomColor"/>.  
    private Color _balloonColor;

    /// Used to calculate Upthrust for floatation. 0 matches density of surrounding fluid (air). 
    /// Less than 0 is lighter, greater than 0 is heavier
    private float _densityRelatedToFluid = -82f;

    /// <summary>
    /// Boundaries for balloon resetting. Set during initialization.
    /// </summary>
    private float _xPosMin;
    private float _xPosMax;
    private float _yPosMin;
    private float _yPosMax;
    private float _zPosMin;
    private float _zPosMax;
    private float _yVelMin;
    private float _yVelMax;

    // The Balloon's Rigid Body component.
    private Rigidbody _rigidBody;

    // The Audio Source for the Pop sound
    private AudioSource _audioSource;

    // Calculated automatically based on volume of balloon and the fluid (air) density setting _densityRelatedToFluid.
    private float _upThrust;

   
    void Awake ()
    {
        PickNewColor();

        _rigidBody = GetComponent<Rigidbody>();
        _audioSource = GetComponent<AudioSource>();

        CalculateUpThrust();
    }


    /// Picks at random one of 6 colors for the balloon body and nub. Called with <see cref="ResetBalloon(bool)"/>. 
    public void PickNewColor()
    {
        if (_body == null || _nub == null)
        {
            _body = transform.Find("Body").gameObject;
            _nub = transform.Find("Nub").gameObject;
        }

        Renderer tBodyRend = _body.GetComponent<Renderer>();
        Renderer tNubRend = _nub.GetComponent<Renderer>();

        _balloonColor = GetRandomColor();
        
        tBodyRend.material.color = _balloonColor;
        tNubRend.material.color = _balloonColor;
    }

    /// <summary>
    /// Utility method for identifying one of a pre-determined list of colors. 
    /// </summary>
    /// <returns>One of 6 colors (the primary and secondary colors).</returns>
    private Color GetRandomColor()
    {
        int tRand = Random.Range(0, 5);
        switch(tRand)
        {
            case 0:
                return Color.red;
            case 1:
                return Color.green;
            case 2:
                return Color.blue;
            case 3:
                return Color.yellow;
            case 4:
                return Color.cyan;
            case 5:
                return Color.magenta;
            default:
                return Color.red;
        }
    }

    void FixedUpdate()
    {
        if ( transform.position.y > _yPosMax )
        {
            // When resetting a balloon that has gone off screen, reset without playing balloon pop.
            StartCoroutine( ResetBalloon( false ) );
            return;
        }
        else
        {
            _rigidBody.AddForce( Vector3.up * _upThrust, ForceMode.Acceleration );
        }
    }

    /// <summary>
    /// Resets the balloon. Plays the Pop sound if sound is enabled. When sound completes, resets the balloon and places it in new location. 
    /// </summary>
    /// <param name="pAudioEnabled">false: overrides the _audioSource.enabled setting and silences the audio. 
    /// Does not modify the _audioSoure.enabled setting. </param>
    public IEnumerator ResetBalloon( bool pAudioEnabled = true )
    {
        Enable( false );

        if( _audioSource.enabled && pAudioEnabled )
        { 
            _audioSource.Play();
        
            while ( _audioSource.isPlaying )
            {
                yield return null; 
            }
        }

        PickNewColor();

        _rigidBody.velocity = new Vector3(0, Random.Range(_yVelMin, _yVelMax), 0);
       
        transform.position = new Vector3(Random.Range(_xPosMin, _xPosMax), _yPosMin, Random.Range(_zPosMin, _zPosMax));
        transform.Rotate(Vector3.up, Random.Range(-30f, 30f));

        Enable( true );
    }

    /// <summary>
    /// Does several things to disable the balloon including disabling the balloon's collider so the laser pointer 
    /// can not collide with the balloon while the balloon is disabled.  
    /// </summary>
    /// <param name="pEnable">True to enable, False to disable.</param>
    public void Enable( bool pEnable = true )
    {
        enabled = pEnable;
        _body.GetComponent<Collider>().enabled = pEnable;
        SetVisible( pEnable );
    }

    /// <summary>
    /// Set basic appearance (scale and first position)
    /// </summary>
    /// <param name="pScale">The scale of the balloon.</param>
    public void CustomizeInitialAppearance( float pScale )
    {
        transform.localScale = new Vector3( pScale, pScale, pScale );
        transform.position = new Vector3( transform.position.x, Random.Range( _yPosMin, _yPosMax ), transform.position.z );
    }

    /// Initializes balloon appearance boundaries and velocity.  
    public void InitializeValues(
         float pXPosMin,
         float pXPosMax,
         float pYPosMin,
         float pYPosMax,
         float pZPosMin,
         float pZPosMax,
         float pYVelMin,
         float pYVelMax )
    {
        _xPosMin = pXPosMin;
        _xPosMax = pXPosMax;
        _yPosMin = pYPosMin;
        _yPosMax = pYPosMax;
        _zPosMin = pZPosMin;
        _zPosMax = pZPosMax;
        _yVelMin = pYVelMin;
        _yVelMax = pYVelMax;
    }

    /// <summary>
    /// Toggles visibility of the balloon.
    /// </summary>
    /// <param name="pVisible">True for visible. False for invisible.</param>
    public void SetVisible( bool pVisible )
    {
        _body.GetComponent<Renderer>().enabled = pVisible;
        _nub.GetComponent<Renderer>().enabled = pVisible;
    }

    /// Utility method that calculates a thrust coefficient that will be used for applying an upwards push every Update. 
    /// Calculation is based on the balloon's volume and the density of the fluid (air). 
    private void CalculateUpThrust()
    {
        Vector3 tSize = _body.GetComponent<BoxCollider>().bounds.size;
        _upThrust = ( tSize.x * tSize.y * tSize.z ) * _densityRelatedToFluid * -1f;
    }

    /// <summary>
    /// Enables or disables the Pop sound that plays when the laser pops a balloon.
    /// </summary>
    /// <param name="pEnable">True to enable audio. False to disable audio.</param>
    public void EnableAudio( bool pEnable = true )
    {
        _audioSource.enabled = pEnable;
    }

    /// <returns>The color of the balloon and nub.</returns>
    public Color GetBalloonColor()
    {
        return _balloonColor;
    }

}
