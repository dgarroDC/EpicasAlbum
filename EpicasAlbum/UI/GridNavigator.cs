using UnityEngine;

namespace EpicasAlbum.UI;

// Based on ListNavigator from Suit Log/Custom Ship Log Modes, but not using the Input thing this time...
public class GridNavigator : MonoBehaviour
{    
    private float _pressedLeftTimer;
    private float _pressedRighTimer;
    private float _pressedUpTimer;
    private float _pressedDownTimer;
    private float _nextHoldLeftTime;
    private float _nextHoldRightTime;
    private float _nextHoldUpTime;
    private float _nextHoldDownTime;
    
    public Vector2 GetSelectionChange()
    {
        // See ShipLogMapMode.UpdateMode()
        // I should probably extract a button thing or something...
        if (OWInput.IsPressed(InputLibrary.left) || OWInput.IsPressed(InputLibrary.left2))
        {
            _pressedLeftTimer += Time.unscaledDeltaTime;
        }
        else
        {
            _nextHoldLeftTime = 0f;
            _pressedLeftTimer = 0f;
        }
        if (OWInput.IsPressed(InputLibrary.right) || OWInput.IsPressed(InputLibrary.right2))
        {
            _pressedRighTimer += Time.unscaledDeltaTime;
        }
        else
        {
            _nextHoldRightTime = 0f;
            _pressedRighTimer = 0f;
        }
        if (OWInput.IsPressed(InputLibrary.up) || OWInput.IsPressed(InputLibrary.up2))
        {
            _pressedUpTimer += Time.unscaledDeltaTime;
        }
        else
        {
            _nextHoldUpTime = 0f;
            _pressedUpTimer = 0f;
        }
        if (OWInput.IsPressed(InputLibrary.down) || OWInput.IsPressed(InputLibrary.down2))
        {
            _pressedDownTimer += Time.unscaledDeltaTime;
        }
        else
        {
            _nextHoldDownTime = 0f;
            _pressedDownTimer = 0f;
        }

        int verticalDelta = 0;
        int horizontalDelta = 0;
        if (_pressedLeftTimer > _nextHoldLeftTime)
        {
            _nextHoldLeftTime += _nextHoldLeftTime < 0.1f ? 0.4f : 0.15f;
            horizontalDelta = -1;
        }
        else if (_pressedRighTimer > _nextHoldRightTime)
        {
            _nextHoldRightTime += _nextHoldRightTime < 0.1f ? 0.4f : 0.15f;
            horizontalDelta = 1;
        }
        if (_pressedUpTimer > _nextHoldUpTime)
        {
            _nextHoldUpTime += _nextHoldUpTime < 0.1f ? 0.4f : 0.15f;
            verticalDelta = -1;
        }
        else if (_pressedDownTimer > _nextHoldDownTime)
        {
            _nextHoldDownTime += _nextHoldDownTime < 0.1f ? 0.4f : 0.15f;
            verticalDelta = 1;
        }

        return new Vector2(horizontalDelta, verticalDelta);
    }
}