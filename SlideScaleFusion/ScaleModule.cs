using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LabFusion.SDK.Modules;
using LabFusion.Utilities;
using UnityEngine;
using Jevil;
using SlideScale;
using LabFusion.Network;

namespace SlideScaleFusion;

public static class ModuleInfo
{
    public const string Name = "SlideScaleSync"; // Name of the Module.  (MUST BE SET)
    public const string Version = "1.0.0"; // Version of the Module.  (MUST BE SET)
    public const string Author = "extraes"; // Author of the Module.  (MUST BE SET)
    public const string Abbreviation = null; // Abbreviation of the Module. (Set as null if none)
    public const bool AutoRegister = true; // Should the Module auto register when the assembly is loaded?
    public const ConsoleColor Color = ConsoleColor.Magenta; // The color of the logged load info. (MUST BE SET)
}

public class ScaleModule : Module
{
    static ScaleModule instance;
    public static ScaleModule Instance => instance;

    HashSet<Transform> scaledObjects = new(UnityObjectComparer<Transform>.Instance);

    public override void OnModuleLoaded()
    {
        instance = this;
        Scale.ObjectScaled += ObjectScaled;
        MultiplayerHooking.OnPlayerCatchup += CatchupPlayer;
    }

    private void ObjectScaled(Transform transform)
    {
        scaledObjects.Add(transform);
        using FusionMessage msg = GetObjectScaleMsg(transform);
        MessageSender.SendToServer(NetworkChannel.Reliable, msg);
    }

    private FusionMessage GetObjectScaleMsg(Transform scaledT)
    {
        using FusionWriter writer = FusionWriter.Create();
        using ScaleData data = ScaleData.Create(scaledT);
        writer.Write(data);

        return FusionMessage.ModuleCreate<ScaleMessage>(writer);
    }

    private void CatchupPlayer(ulong longId)
    {
        foreach (Transform transform in scaledObjects)
        {
            using FusionMessage msg = GetObjectScaleMsg(transform);
            MessageSender.SendFromServer(longId, NetworkChannel.Reliable, msg);
        }
    }

    #region Logging
    public static void Log(object msg) => instance.LoggerInstance.Log(msg?.ToString() ?? "<null>");
    public static void Warn(object msg) => instance.LoggerInstance.Warn(msg?.ToString() ?? "<null>");
    public static void Error(object msg) => instance.LoggerInstance.Error(msg?.ToString() ?? "<null>");
    public static void Error(string whatFailed, Exception ex) => instance.LoggerInstance.LogException(whatFailed ?? "<null>", ex);
    #endregion
}

