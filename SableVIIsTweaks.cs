using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using VRCFaceTracking;
using VRCFaceTracking.Core.Library;
using VRCFaceTracking.Core.Params.Data;
using VRCFaceTracking.Core.Params.Expressions;
using VRCFaceTracking.Core.Types;
using VirtualDesktop.FaceTracking;
using System.Diagnostics;
using System.ComponentModel.DataAnnotations;

public unsafe class SableVIITweaks
{
    #region Static Fields
    private static float _eyeOpenessLimitByLookDownRatio = 0.6f;

    private static Vector2 _eyeLeftGazeLimitX = new Vector2(-0.64f, 0.64f);
    private static Vector2 _eyeLeftGazeLimitY = new Vector2(-0.57f, 0.57f);
    private static Vector2 _eyeRightGazeLimitX = new Vector2(-0.64f, 0.64f);
    private static Vector2 _eyeRightGazeLimitY = new Vector2(-0.57f, 0.57f);


    private static float _eyeOpenessNeutral = 0.75f;
    private static float _eyeOpenessMinLimit = 0.15f;
    private static float _closenessRange = _eyeOpenessNeutral - _eyeOpenessMinLimit;
    // How much squinting affects the amount of Eye Openess
    private static float _squintCloseRatio = 0.725f;

    private static Stopwatch _deltaTimeStopwatch = new Stopwatch();

    // In miliseconds the time it takes for a forced blink to let go
    private static long _blinkTime = 55;
    private static bool _awaitingLeftEyeBlink = false;
    private static long _leftEyeBlinkTimer = 0;
    private static bool _awaitingRightEyeBlink = false;
    private static long _rightEyeBlinkTimer = 0;

    private static float _minLeftEyeOpeness = 0.17f;
    private static float _minRightEyeOpeness = 0.17f;

    #endregion

    #region Static Methods
    public static void PostTweak(TrackingModule trackingModule, UnifiedExpressionShape[] unifiedExpressions, UnifiedEyeData eyeData)
    {
        _deltaTimeStopwatch.Stop();
        // deltaTime in miliseconds
        long deltaTime = _deltaTimeStopwatch.ElapsedMilliseconds; // Maybe Max function is neccissary
        _deltaTimeStopwatch.Restart();
        //trackingModule.Logger.LogInformation("[VirtualDesktopSableVII] Stuff: ");

        // Limit Left Eye Openess based on Look Down
        if (eyeData.Left.Gaze.y < 0.0f)
        {
            float leftEyeOpenessLimit = 1.0f - (eyeData.Left.Gaze.y / (-1.0f) * _eyeOpenessLimitByLookDownRatio);
            if (eyeData.Left.Openness > leftEyeOpenessLimit)
            {
                eyeData.Left.Openness = leftEyeOpenessLimit;
            }
        }

        // Limit Right Eye Openess based on Look Down
        if (eyeData.Right.Gaze.y < 0.0f)
        {
            float rightEyeOpenessLimit = 1.0f - (eyeData.Right.Gaze.y / (-1.0f) * _eyeOpenessLimitByLookDownRatio);
            if (eyeData.Right.Openness > rightEyeOpenessLimit)
            {
                eyeData.Right.Openness = rightEyeOpenessLimit;
            }
        }

        // Left Eye Squint reduces Left Eye Openess
        eyeData.Left.Openness = MathF.Max(0.0f, eyeData.Left.Openness - unifiedExpressions[(int)UnifiedExpressions.EyeSquintLeft].Weight * _closenessRange * _squintCloseRatio);

        // Right Eye Squint reduces Right Eye Openess
        eyeData.Right.Openness = MathF.Max(0.0f, eyeData.Right.Openness - unifiedExpressions[(int)UnifiedExpressions.EyeSquintRight].Weight * _closenessRange * _squintCloseRatio);

        // Eye Gaze Limiting  - Not needed anymore cuz of openness math changes
        //eyeData.Left.Gaze = new Vector2(float.Clamp(eyeData.Right.Gaze.x, EyeLeftGazeLimitX.x, EyeLeftGazeLimitX.y), float.Clamp(eyeData.Right.Gaze.y, EyeLeftGazeLimitY.x, EyeLeftGazeLimitY.y));
        //eyeData.Right.Gaze = new Vector2(float.Clamp(eyeData.Right.Gaze.x, EyeRightGazeLimitX.x, EyeRightGazeLimitX.y), float.Clamp(eyeData.Right.Gaze.y, EyeRightGazeLimitY.x, EyeRightGazeLimitY.y));

        // Testing Anti-frog-eye stuff
        if (Math.Abs(eyeData.Left.Openness - eyeData.Right.Openness) < 0.335)
        {
            float average = (eyeData.Left.Openness + eyeData.Right.Openness) / 2.0f;
            eyeData.Left.Openness = average;
            eyeData.Right.Openness = average;
        }


        // Blink Locking
        // Ensure that when a blink happens, the eye will not unblink untill a certain amount of time has passed. This will allow for the smoothing operation to not unintentionally completely 'delete' quick blinks
        // Left Eye Blink Locking
        if (_awaitingLeftEyeBlink == true)
        {
            _leftEyeBlinkTimer += deltaTime;

            if (_leftEyeBlinkTimer >= _blinkTime)
            {
                _leftEyeBlinkTimer = 0;
                _awaitingLeftEyeBlink = false;

                // Maybe force eye openness to a slightly new value to ensure the value gets updated and not potentially culled
            }
            else
            {
                eyeData.Left.Openness = 0.0f;
            }
        }
        if (_awaitingLeftEyeBlink == false && eyeData.Left.Openness <= _minLeftEyeOpeness)
        {
            _awaitingLeftEyeBlink = true;
        }

        // Right Eye Blink Locking
        if (_awaitingRightEyeBlink == true)
        {
            _rightEyeBlinkTimer += deltaTime;

            if (_rightEyeBlinkTimer >= _blinkTime)
            {
                _rightEyeBlinkTimer = 0;
                _awaitingRightEyeBlink = false;

                // Maybe force eye openness to a slightly new value to ensure the value gets updated and not potentially culled
            }
            else
            {
                eyeData.Right.Openness = 0.0f;
            }
        }
        if (_awaitingRightEyeBlink == false && eyeData.Right.Openness <= _minRightEyeOpeness)
        {
            _awaitingRightEyeBlink = true;
        }

        // Force full left blink when threshold is passed
        if (eyeData.Left.Openness < _minLeftEyeOpeness)
        {
            eyeData.Left.Openness = 0.0f;
        }

        // Force full right blink when threshold is passed
        if (eyeData.Right.Openness < _minRightEyeOpeness)
        {
            eyeData.Right.Openness = 0.0f;
        }
    }

    public static void TweakEyeOpenness(UnifiedEyeData eye, float* expressions)
    {
        eye.Left.Openness = 1.0f - Math.Max(0, Math.Min(1, expressions[(int)Expressions.EyesClosedL] + expressions[(int)Expressions.EyesClosedL] * expressions[(int)Expressions.LidTightenerL]));
        eye.Right.Openness = 1.0f - (float)Math.Max(0, Math.Min(1, expressions[(int)Expressions.EyesClosedR] + expressions[(int)Expressions.EyesClosedR] * expressions[(int)Expressions.LidTightenerR]));
    }
    
    #endregion
}