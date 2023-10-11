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
    public enum DataType { 
        SCALE = 0,
        MASS = 1
    }

    public void Dispose() => GC.SuppressFinalize(this);
    public SerializedGameObjectReference serializedGO;
    public DataType dataType;
    public Vector3 scale;
    public float mass;

    public void Deserialize(FusionReader reader)
    {
        dataType = (DataType) reader.ReadByte();
        serializedGO = reader.ReadFusionSerializable<SerializedGameObjectReference>();
        scale = reader.ReadVector3();
        mass = reader.ReadSingle();
    }

    public void Serialize(FusionWriter writer)
    {
        writer.Write((byte)dataType);
        writer.Write(serializedGO);
        writer.Write(scale);
        writer.Write(mass);
    }

    public static ScaleData Create(Transform transform)
    {
        return new ScaleData()
        {
            serializedGO = new SerializedGameObjectReference(transform.gameObject),
            scale = transform.localScale,
            mass = 0,
            dataType = DataType.SCALE
        };
    }

    public static ScaleData Create(Rigidbody body)
    {
        return new ScaleData()
        {
            serializedGO = new SerializedGameObjectReference(body.gameObject),
            scale = Vector3.one,
            mass = body.mass,
            dataType = DataType.MASS
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

        // Couldn't find the object that was referenced.
        if (!data.serializedGO.gameObject) 
        {
            return;
        }

        switch (data.dataType) {
            case ScaleData.DataType.SCALE:
#if DEBUG
                ScaleModule.Log($"Scaling object {data.serializedGO.gameObject.name} to {data.scale}");
#endif
                data.serializedGO.gameObject.transform.localScale = data.scale;
                break;
            case ScaleData.DataType.MASS:
#if DEBUG
                ScaleModule.Log($"Scaling object mass of {data.serializedGO.gameObject.name} to {data.mass}");
#endif
                data.serializedGO.gameObject.GetComponent<Rigidbody>().mass = data.mass;
                break;
        }
    }
}