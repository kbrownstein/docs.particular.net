﻿using log4net;
using NServiceBus;

public class AnotherEventMessageHandler : IHandleMessages<AnotherEventMessage>
{
    static ILog log = LogManager.GetLogger(typeof(AnotherEventMessageHandler));

    public void Handle(AnotherEventMessage message)
    {
        log.InfoFormat("Subscriber 1 received AnotherEventMessage with Id {0}.", message.EventId);
        log.InfoFormat("Message time: {0}.", message.Time);
        log.InfoFormat("Message duration: {0}.", message.Duration);
    }
}