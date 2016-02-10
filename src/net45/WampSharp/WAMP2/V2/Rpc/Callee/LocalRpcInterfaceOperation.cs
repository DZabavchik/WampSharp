using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Internal;
using WampSharp.Core.Serialization;
using WampSharp.Logging;
using WampSharp.V2;
using WampSharp.V2.Core;
using WampSharp.V2.Core.Contracts;
using WampSharp.V2.Error;
using WampSharp.V2.Rpc;

namespace WampSharp.WAMP2.V2.Rpc.Callee
{
    public class LocalRpcInterfaceOperation: IWampRpcOperation
    {
        private readonly object mInstance;
        private readonly string mPrefix;
        private readonly string mPath;
        private readonly ILog mLogger;
        private readonly Dictionary<string, OperationToRegister> mOperationMap =
          new Dictionary<string, OperationToRegister>();
        protected readonly static IWampFormatter<object> ObjectFormatter = WampObjectFormatter.Value;

        public LocalRpcInterfaceOperation(object instance, string prefix) :
            this(instance, String.Empty, prefix, CalleeRegistrationInterceptor.Default)
        {
        }

        public LocalRpcInterfaceOperation(object instance, string prefix, ICalleeRegistrationInterceptor interceptor) : 
            this(instance, String.Empty, prefix, interceptor) {
        }

        public LocalRpcInterfaceOperation(object instance, string path, string prefix, ICalleeRegistrationInterceptor interceptor)
        {
            mInstance = instance;
            mPrefix = prefix;
            mPath = path;
            mLogger = LogProvider.GetLogger(typeof(LocalRpcInterfaceOperation) + "." + (path.IsNullOrEmpty() ? "" : path +".") + prefix);

            OperationExtractor extractor = new OperationExtractor();

            IEnumerable<OperationToRegister> operationsToRegister =
                extractor.ExtractOperations(instance, interceptor);

            foreach (var operationMapRecord in operationsToRegister)
            {
                mOperationMap.Add(operationMapRecord.Operation.Procedure, operationMapRecord);
            }
        }

        public void Invoke<TMessage>(IWampRawRpcOperationRouterCallback caller, IWampFormatter<TMessage> formatter,
       InvocationDetails details)
        {
            InnerInvoke(caller, formatter, details, null, null);
        }

        public void Invoke<TMessage>(IWampRawRpcOperationRouterCallback caller, IWampFormatter<TMessage> formatter,
            InvocationDetails details, TMessage[] arguments)
        {
            InnerInvoke(caller, formatter, details, arguments, null);
        }

        public void Invoke<TMessage>(IWampRawRpcOperationRouterCallback caller, IWampFormatter<TMessage> formatter,
            InvocationDetails details, TMessage[] arguments, IDictionary<string, TMessage> argumentsKeywords)
        {
            InnerInvoke(caller, formatter, details, arguments, argumentsKeywords);
        }

        protected void InnerInvoke<TMessage>(IWampRawRpcOperationRouterCallback caller,
            IWampFormatter<TMessage> formatter,
            InvocationDetails details,
            TMessage[] arguments,
            IDictionary<string, TMessage> argumentsKeywords)
        {
            var procedureTail = details.Procedure.Substring(mPrefix.Length + 1);

            OperationToRegister record;
            if (mOperationMap.TryGetValue(procedureTail, out record))
            {
                record.Operation.Invoke(caller, formatter, details, arguments, argumentsKeywords);
                return;
            }
            else if (procedureTail.Contains('.'))
            {
                var propertyName = procedureTail.Substring(0, procedureTail.IndexOf('.'));
                if (mOperationMap.TryGetValue(propertyName, out record))
                {
                    InvocationDetails childDetails = (InvocationDetails)Activator.CreateInstance(details.GetType(), new object[] {details});
                    childDetails.Procedure = procedureTail;
                    record.Operation.Invoke(caller, formatter, childDetails, arguments, argumentsKeywords);
                    return;
                }
            }

            IWampErrorCallback callback = new WampRpcErrorCallback(caller);
            WampRpcRuntimeException wampException = ConvertExceptionToRuntimeException(new NotSupportedException("Method not supported " + procedureTail));
            callback.Error(wampException);
        }

        protected static WampRpcRuntimeException ConvertExceptionToRuntimeException(Exception exception)
        {
            return new WampRpcRuntimeException(exception.Message, exception);
        }

        protected class WampRpcErrorCallback : IWampErrorCallback
        {
            private readonly IWampRawRpcOperationRouterCallback mCallback;

            public WampRpcErrorCallback(IWampRawRpcOperationRouterCallback callback)
            {
                mCallback = callback;
            }

            public void Error(object details, string error)
            {
                mCallback.Error(ObjectFormatter, details, error);
            }

            public void Error(object details, string error, object[] arguments)
            {
                mCallback.Error(ObjectFormatter, details, error, arguments);
            }

            public void Error(object details, string error, object[] arguments, object argumentsKeywords)
            {
                mCallback.Error(ObjectFormatter, details, error, arguments, argumentsKeywords);
            }
        }
        public string Procedure
        {
            get { return mPrefix; }
        }
    }
}
