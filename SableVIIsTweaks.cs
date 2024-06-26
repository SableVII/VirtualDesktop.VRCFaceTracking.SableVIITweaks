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

public class SableVIITweaks
{
    #region Static Fields
    private static float EyeOpenessLimitByLookDownRatio = 0.8f;

    private static Vector2 EyeLeftGazeLimitX = new Vector2(-0.64f, 0.64f);
    private static Vector2 EyeLeftGazeLimitY = new Vector2(-0.57f, 0.57f);
    private static Vector2 EyeRightGazeLimitX = new Vector2(-0.64f, 0.64f);
    private static Vector2 EyeRightGazeLimitY = new Vector2(-0.57f, 0.57f);   
    #endregion

    #region Static Methods
    public static void Tweak(TrackingModule trackingModule, UnifiedExpressionShape[] unifiedExpressions, UnifiedEyeData eyeData)
    {
        //trackingModule.Logger.LogInformation("[VirtualDesktopSableVII] Left Eye Openess Before: " + eyeData.Left.Openness);
        // Limit Left Eye Openess based on Look Down
        if (eyeData.Left.Gaze.y < 0.0f)
        {
            float leftEyeOpenessLimit = 1.0f - (eyeData.Left.Gaze.y/(-1.0f) * EyeOpenessLimitByLookDownRatio);
            if (eyeData.Left.Openness > leftEyeOpenessLimit)
            {
                eyeData.Left.Openness = leftEyeOpenessLimit;
            }
        }

        // Limit Right Eye Openess based on Look Down
        if (eyeData.Right.Gaze.y < 0.0f)
        {
            float rightEyeOpenessLimit = 1.0f - (eyeData.Right.Gaze.y/(-1.0f) * EyeOpenessLimitByLookDownRatio);
            if (eyeData.Right.Openness > rightEyeOpenessLimit)
            {
                eyeData.Right.Openness = rightEyeOpenessLimit;
            }
        }

        // Eye Gaze Limiting
        eyeData.Left.Gaze = new Vector2(float.Clamp(eyeData.Right.Gaze.x, EyeLeftGazeLimitX.x, EyeLeftGazeLimitX.y), float.Clamp(eyeData.Right.Gaze.y, EyeLeftGazeLimitY.x, EyeLeftGazeLimitY.y));
        eyeData.Right.Gaze = new Vector2(float.Clamp(eyeData.Right.Gaze.x, EyeRightGazeLimitX.x, EyeRightGazeLimitX.y), float.Clamp(eyeData.Right.Gaze.y, EyeRightGazeLimitY.x, EyeRightGazeLimitY.y));
    }
    #endregion
}