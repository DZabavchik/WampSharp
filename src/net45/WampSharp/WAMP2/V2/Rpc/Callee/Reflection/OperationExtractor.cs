﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using WampSharp.Core.Utilities;
using WampSharp.V2.Core.Contracts;
using WampSharp.WAMP2.V2.Rpc.Callee;
using TaskExtensions = WampSharp.Core.Utilities.TaskExtensions;

namespace WampSharp.V2.Rpc
{
    internal class OperationExtractor : IOperationExtractor
    {
        public IEnumerable<OperationToRegister> ExtractOperations(object instance, ICalleeRegistrationInterceptor interceptor)
        {
            Type type = instance.GetType();
            IEnumerable<Type> typesToExplore = GetTypesToExplore(type);

            foreach (Type currentType in typesToExplore)
            {
                IEnumerable<OperationToRegister> currentOperations =
                    GetServiceMethodsOfType(instance, currentType, interceptor);

                foreach (OperationToRegister operation in currentOperations)
                {
                    yield return operation;
                }
            }
        }

        private static IEnumerable<Type> GetTypesToExplore(Type type)
        {
            yield return type;

            foreach (Type currentInterface in type.GetInterfaces())
            {
                yield return currentInterface;
            }
        }

        private IEnumerable<OperationToRegister> GetServiceMethodsOfType
            (object instance,
                Type type,
                ICalleeRegistrationInterceptor interceptor)
        {
            foreach (var method in type.GetPublicInstanceMethods())
            {
                if (interceptor.IsCalleeProcedure(method))
                {
                    IWampRpcOperation operation = CreateRpcMethod(instance, interceptor, method);
                    RegisterOptions options = interceptor.GetRegisterOptions(method);

                    yield return new OperationToRegister(operation, options);
                }
            }
        }

        private bool HasServiceMethodsInType
            (Type type,
                ICalleeRegistrationInterceptor interceptor)
        {
            foreach (var method in type.GetPublicInstanceMethods())
            {
                if (interceptor.IsCalleeProcedure(method))
                {
                    return true;
                }
            }

            return false;
        }

        protected IWampRpcOperation CreateRpcMethod(object instance, ICalleeRegistrationInterceptor interceptor, MethodInfo method)
        {
            string procedureUri =
                interceptor.GetProcedureUri(method);

            //TODO: need better detection of nested
            if (HasServiceMethodsInType(method.ReturnType, interceptor) && method.GetParameters().Length == 0)
            {
                var memberInstance = method.Invoke(instance, parameters: new object[] {});
                return new LocalRpcInterfaceOperation(memberInstance, procedureUri, interceptor);
            }
            else if (!typeof (Task).IsAssignableFrom(method.ReturnType))
            {
                return new SyncMethodInfoRpcOperation(instance, method, procedureUri);
            }
            else
            {
#if !NET40
                if (method.IsDefined(typeof (WampProgressiveResultProcedureAttribute)))
                {
                    MethodInfoValidation.ValidateProgressiveMethod(method);
                    return CreateProgressiveOperation(instance, method, procedureUri);
                }
                else
#endif
                {
                    MethodInfoValidation.ValidateAsyncMethod(method);
                    return new AsyncMethodInfoRpcOperation(instance, method, procedureUri);
                }
            }
        }

#if !NET40
        private static IWampRpcOperation CreateProgressiveOperation(object instance, MethodInfo method, string procedureUri)
        {
            //return new ProgressiveAsyncMethodInfoRpcOperation<returnType>
            // (instance, method, procedureUri);

            Type returnType =
                TaskExtensions.UnwrapReturnType(method.ReturnType);

            Type operationType =
                typeof (ProgressiveAsyncMethodInfoRpcOperation<>)
                    .MakeGenericType(returnType);

            IWampRpcOperation operation =
                (IWampRpcOperation) Activator.CreateInstance(operationType,
                    instance,
                    method,
                    procedureUri);

            return operation;
        }

#endif

    }
}