using Jevil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SlideScale;

public class ConstForce : MonoBehaviour
{
    public ConstForce(IntPtr ptr) : base(ptr) { }
    Rigidbody rb;
    private Vector3 _relativeTorque;
    private Vector3 _relativeForce;
    
    // Don't do physics calls unless they're necessary. (This check is probably unnecessary, but two bool checks won't kill someone
    private bool doTorque;
    private bool doForce;

    public Vector3 relativeTorque
    {
        get => _relativeTorque;
        set
        {
            doTorque = true;
            _relativeTorque = value;
        }
    }
    public Vector3 relativeForce
    {
        get => _relativeForce;
        set
        {
            doForce = true;
            relativeForce = value;
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
#if DEBUG
        if (rb.INOC()) Scale.Warn($"ConstForce cannot be applied to a GameObject that has no RigidBody! Full path: {transform.GetFullPath()}");
#endif
    }

    void FixedUpdate()
    {
        if (doForce)
        {
            rb.AddRelativeForce(_relativeForce, ForceMode.Impulse);
        }

        if (doTorque)
        {
            rb.AddRelativeTorque(_relativeTorque, ForceMode.Impulse);
        }
    }
}
