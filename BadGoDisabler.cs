
using System;
using UnityEngine;
using UnhollowerRuntimeLib;
using System.Runtime.InteropServices;

namespace VRCPlusPet
{
    internal class BadGoDisabler : MonoBehaviour
    {
        public Delegate ReferencedDelegate;
        public IntPtr MethodInfo;
        public BadGoDisabler(IntPtr obj0) : base(obj0)
        {
            ClassInjector.DerivedConstructorBody(this);
        }
        public BadGoDisabler(Delegate referencedDelegate, IntPtr methodInfo) : base(ClassInjector.DerivedConstructorPointer<BadGoDisabler>())
        {
            ClassInjector.DerivedConstructorBody(this);

            ReferencedDelegate = referencedDelegate;
            MethodInfo = methodInfo;
        }
        ~BadGoDisabler()
        {
            Marshal.FreeHGlobal(MethodInfo);
            MethodInfo = IntPtr.Zero;
            ReferencedDelegate = null;
        }

        void OnEnable() => this.gameObject.SetActive(false);
    }
}
