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
using LabFusion.Data;
using LabFusion.Network;

namespace SlideScaleFusion;

public sealed class ScaleData : IFusionSerializable, IDisposable
{
    public void Dispose() => GC.SuppressFinalize(this);
    public GameObject targetGo;
    public Vector3 scale;

    public void Deserialize(FusionReader reader)
    {
        targetGo = reader.ReadGameObject();
    }

    public void Serialize(FusionWriter writer)
    {
        writer.Write(targetGo);
        writer.Write(scale);
    }

    public static ScaleData Create(Transform transform)
    {
        return new ScaleData()
        {
            targetGo = transform.gameObject,
            scale = transform.localScale,
        };
    }
}

public sealed class ScaleMessage : ModuleMessageHandler
{
    public override void HandleMessage(byte[] bytes, bool isServerHandled = false)
    {
        using FusionReader reader = FusionReader.Create(bytes);
        using ScaleData data = reader.ReadFusionSerializable<ScaleData>();

        if (NetworkInfo.IsServer && isServerHandled)
        {
            using FusionMessage msg = FusionMessage.ModuleCreate<ScaleMessage>(bytes);
            MessageSender.BroadcastMessage(NetworkChannel.Reliable, msg);
            return;
        }

#if DEBUG
       ScaleModule.Log($"Scaling object {data.targetGo.name} to {data.scale}");
#endif

        data.targetGo.transform.localScale = data.scale;
    }
}