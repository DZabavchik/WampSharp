﻿using System.Collections.Generic;
using System.Linq;
using WampSharp.Core.Serialization;
using WampSharp.V2.Core;
using WampSharp.V2.Core.Contracts;

namespace WampSharp.V2
{
    internal abstract class WampSerializedEvent : IWampSerializedEvent
    {
        private readonly long mPublicationId;
        private readonly EventDetails mDetails;
        private readonly ISerializedValue[] mArguments;
        private readonly IDictionary<string, ISerializedValue> mArgumentsKeywords;

        protected WampSerializedEvent(long publicationId, EventDetails details, ISerializedValue[] arguments, IDictionary<string, ISerializedValue> argumentsKeywords)
        {
            mPublicationId = publicationId;
            mDetails = details;
            mArguments = arguments;
            mArgumentsKeywords = argumentsKeywords;
        }

        public long PublicationId
        {
            get
            {
                return mPublicationId;
            }
        }

        public EventDetails Details
        {
            get
            {
                return mDetails;
            }
        }

        public ISerializedValue[] Arguments
        {
            get
            {
                return mArguments;
            }
        }

        public IDictionary<string, ISerializedValue> ArgumentsKeywords
        {
            get
            {
                return mArgumentsKeywords;
            }
        }
    }


    internal class WampSerializedEvent<TMessage> : WampSerializedEvent
    {
        public WampSerializedEvent(IWampFormatter<TMessage> formatter,
                                   long publicationId,
                                   EventDetails details,
                                   TMessage[] arguments = null) :
                                       this(formatter,
                                            publicationId,
                                            details,
                                            arguments,
                                            (IDictionary<string, TMessage>)null)
        {
        }

        public WampSerializedEvent(IWampFormatter<TMessage> formatter,
                                   long publicationId,
                                   EventDetails details,
                                   TMessage[] arguments,
                                   IDictionary<string, TMessage> argumentsKeywords) :
                                       this(formatter,
                                            publicationId,
                                            details,
                                            arguments,
                                            GetSerializedDictionary(formatter, argumentsKeywords))
        {
        }


        private WampSerializedEvent(IWampFormatter<TMessage> formatter,
                                    long publicationId,
                                    EventDetails details,
                                    TMessage[] arguments,
                                    IDictionary<string, ISerializedValue> argumentsKeywords) :
                                        base(publicationId,
                                             details,
                                             GetSerializedArguments(formatter, arguments), argumentsKeywords)
        {
        }

        private static IDictionary<string, ISerializedValue> GetSerializedDictionary(IWampFormatter<TMessage> formatter, IDictionary<string, TMessage> argument)
        {
            if (argument == null)
            {
                return null;
            }
            else
            {
                Dictionary<string, ISerializedValue> result =
                    argument.ToDictionary(x => x.Key, 
                    x => GetSerializedArgument(formatter, x.Value));

                return result;
            }
        }

        private static ISerializedValue GetSerializedArgument(IWampFormatter<TMessage> formatter, object argument)
        {
            if (argument == null)
            {
                return null;
            }
            else if (argument is TMessage)
            {
                return new SerializedValue<TMessage>(formatter, (TMessage)argument);                
            }
            else
            {
                return new SerializedValue<object>(WampObjectFormatter.Value, argument);
            }
        }

        private static ISerializedValue[] GetSerializedArguments(IWampFormatter<TMessage> formatter, TMessage[] arguments)
        {
            if (arguments == null)
            {
                return null;
            }
            else
            {
                return arguments.Select(x => new SerializedValue<TMessage>(formatter, x)).Cast<ISerializedValue>().ToArray();                
            }
        }
    }
}