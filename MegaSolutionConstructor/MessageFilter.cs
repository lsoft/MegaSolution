using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MegaSolutionContructor
{
    [ComImport]
    [Guid("00000016-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMessageFilter
    {
        [PreserveSig]
        int HandleInComingCall(
            int dwCallType,
            IntPtr hTaskCaller,
            int dwTickCount,
            IntPtr lpInterfaceInfo);

        [PreserveSig]
        int RetryRejectedCall(
            IntPtr hTaskCallee,
            int dwTickCount,
            int dwRejectType);

        [PreserveSig]
        int MessagePending(
            IntPtr hTaskCallee,
            int dwTickCount,
            int dwPendingType);
    }

    internal class MessageFilter : MarshalByRefObject, IDisposable, IMessageFilter
    {
        private const int SERVERCALL_ISHANDLED = 0;
        private const int PENDINGMSG_WAITNOPROCESS = 2;
        private const int SERVERCALL_RETRYLATER = 2;
        private readonly IMessageFilter oldFilter;

        public MessageFilter()
        {
            //Starting IMessageFilter for COM objects
            int hr =
                CoRegisterMessageFilter(
                    this,
                    out oldFilter);
            Debug.Assert(hr >= 0,
                "Registering COM IMessageFilter failed!");
        }

        public void Dispose()
        {
            //disabling IMessageFilter
            IMessageFilter dummy;
            int hr = CoRegisterMessageFilter(oldFilter,
                out dummy);
            Debug.Assert(hr >= 0,
                "De-Registering COM IMessageFilter failed!");
            GC.SuppressFinalize(this);
        }

        int IMessageFilter.HandleInComingCall(int dwCallType,
            IntPtr threadIdCaller, int dwTickCount, IntPtr lpInterfaceInfo)
        {
            // Return the ole default (don't let the call through).
            return SERVERCALL_ISHANDLED;
        }

        int IMessageFilter.RetryRejectedCall(IntPtr threadIDCallee,
            int dwTickCount, int dwRejectType)
        {
            if (dwRejectType == SERVERCALL_RETRYLATER)
            {
                // Retry the thread call immediately if return >=0 & 
                // <100.
                return 150; //waiting 150 mseconds until retry
            }
            // Too busy; cancel call. SERVERCALL_REJECTED
            return -1;
            //Call was rejected by callee. 
            //(Exception from HRESULT: 0x80010001 (RPC_E_CALL_REJECTED))
        }

        int IMessageFilter.MessagePending(
            IntPtr threadIDCallee, int dwTickCount, int dwPendingType)
        {
            // Perform default processing.
            return PENDINGMSG_WAITNOPROCESS;
        }

        [DllImport("ole32.dll")]
        [PreserveSig]
        private static extern int CoRegisterMessageFilter(
            IMessageFilter lpMessageFilter,
            out IMessageFilter lplpMessageFilter);
    }
}