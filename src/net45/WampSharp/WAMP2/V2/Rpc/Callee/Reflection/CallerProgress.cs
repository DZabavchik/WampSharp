using System;
using WampSharp.V2.Core;
using WampSharp.V2.Core.Contracts;

namespace WampSharp.V2.Rpc
{
#if NET40
    
    public interface IProgress<in T>
    {
        void Report(T value);
    }
#endif
    internal class CallerProgress<T> : IProgress<T>
    {
        private readonly IWampRawRpcOperationRouterCallback mCaller;

        public CallerProgress(IWampRawRpcOperationRouterCallback caller)
        {
            mCaller = caller;
        }

        public void Report(T value)
        {
            mCaller.Result(WampObjectFormatter.Value,
                new YieldOptions {Progress = true},
                new object[] {value});
        }
    }
}
