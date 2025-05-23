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

public class SableVIITweaks
{
    #region Static Fields
    private static float EyeOpenessLimitByLookDownRatio = 0.6f;

    private static Vector2 EyeLeftGazeLimitX = new Vector2(-0.64f, 0.64f);
    private static Vector2 EyeLeftGazeLimitY = new Vector2(-0.57f, 0.57f);
    private static Vector2 EyeRightGazeLimitX = new Vector2(-0.64f, 0.64f);
    private static Vector2 EyeRightGazeLimitY = new Vector2(-0.57f, 0.57f);   


    private static float EyeOpenessNeutral = 0.75f;
    private static float EyeOpenessMinLimit = 0.1f; 
    private static float ClosenessRange = EyeOpenessNeutral - EyeOpenessMinLimit;
    // How much squinting affects the amount of Eye Openess
    private static float SquintCloseRatio = 0.775f;

    #endregion

    #region Static Methods
    public static void Tweak(TrackingModule trackingModule, UnifiedExpressionShape[] unifiedExpressions, UnifiedEyeData eyeData)
    {
        //trackingModule.Logger.LogInformation("[VirtualDesktopSableVII] Stuff: ");

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


        // Left Eye Squint reduces Left Eye Openess
        eyeData.Left.Openness = MathF.Max(0.0f, eyeData.Left.Openness - unifiedExpressions[(int)UnifiedExpressions.EyeSquintLeft].Weight * ClosenessRange * SquintCloseRatio);

        // Right Eye Squint reduces Right Eye Openess
        eyeData.Right.Openness = MathF.Max(0.0f, eyeData.Right.Openness - unifiedExpressions[(int)UnifiedExpressions.EyeSquintRight].Weight * ClosenessRange * SquintCloseRatio);
    }
    #endregion
}