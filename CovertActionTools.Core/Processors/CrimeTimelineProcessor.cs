using System;
using System.Collections.Generic;
using System.Linq;
using CovertActionTools.Core.Models;

namespace CovertActionTools.Core.Processors
{
    public class CrimeTimelineEvent
    {
        public enum CrimeTimelineEventType
        {
            Unknown = -1,
            Message = 0,
            Meeting = 1,
            Package = 2,
            Action = 3,
            Hide = 4,
            
            Error = 100
        }

        public CrimeTimelineEventType Type { get; set; } = CrimeTimelineEventType.Unknown;
        public List<int> EventIds { get; set; } = new();
        public int SourceParticipantId { get; set; } = -1;
        public int? TargetParticipantId { get; set; } = null;
        public List<int> ItemsCreated { get; set; } = new List<int>();
        public List<int> ItemsTransferred { get; set; } = new List<int>();
        public List<int> ItemsDestroyed { get; set; } = new List<int>();
        public string ErrorMessage { get; set; } = string.Empty;
    }
    
    public interface ICrimeTimelineProcessor
    {
        List<CrimeTimelineEvent> ProcessCrimeIntoTimeline(PackageModel model, CrimeModel crime);
    }
    
    internal class CrimeTimelineProcessor : ICrimeTimelineProcessor
    {
        public List<CrimeTimelineEvent> ProcessCrimeIntoTimeline(PackageModel model, CrimeModel crime)
        {
            //the general algorithm is:
            //  Start with only the MM active, messages activate targets
            //  While there are still messages to process:
            //    Find all events from active participants with the required items
            //    Run them
            //    If all events from that participant are done, go into hiding
            //    If all participants in hiding, complete
            //  If there are unprocessed messages, error (event of type error for each message)
            var timeline = new List<CrimeTimelineEvent>();

            var activeParticipants = new HashSet<int>();
            for (var i = 0; i < crime.Participants.Count; i++)
            {
                if (crime.Participants[i].ParticipantType == CrimeModel.ParticipantType.Mastermind)
                {
                    activeParticipants.Add(i);
                }
            }
            activeParticipants.Add(0); //the first participant is the organizer usually
            var participantItems = new Dictionary<int, List<int>>();
            var processedEvents = new HashSet<int>();
            var safetyCheck = 0;
            
            do
            {
                var potentiallyValidEvents = crime
                    .Events
                    .Select((ev, i) => (ev, i))
                    .Where((pair) =>
                        !processedEvents.Contains(pair.i)
                    )
                    .ToDictionary(x => (int?)x.i, x => x.ev);

                var foundAtLeastOneEvent = false;
                
                foreach (var pair in potentiallyValidEvents)
                {
                    if (pair.Key == null)
                    {
                        throw new Exception("Invalid ID");
                    }
                    var id = pair.Key;
                    var ev = pair.Value;
                    var sourceParticipant = ev.SourceParticipantId;
                    var targetParticipant = ev.TargetParticipantId;
                    
                    //for meetings/messages, that means both events have to be in the list to really be valid
                    int? partnerEventId = null;
                    var isHalfAnEvent = false;
                    if (ev.EventType == CrimeModel.EventType.SentPackage)
                    {
                        isHalfAnEvent = true;
                        partnerEventId = potentiallyValidEvents.FirstOrDefault(x =>
                            x.Value.MessageId == ev.MessageId &&
                            x.Value.EventType == CrimeModel.EventType.ReceivedPackage).Key;
                        sourceParticipant = ev.SourceParticipantId;
                        if (partnerEventId != null)
                        {
                            targetParticipant = crime.Events[partnerEventId.Value].SourceParticipantId;
                        }
                    } else if (ev.EventType == CrimeModel.EventType.ReceivedPackage)
                    {
                        isHalfAnEvent = true;
                        partnerEventId = potentiallyValidEvents.FirstOrDefault(x =>
                            x.Value.MessageId == ev.MessageId &&
                            x.Value.EventType == CrimeModel.EventType.SentPackage).Key;
                        targetParticipant = ev.SourceParticipantId;
                        if (partnerEventId != null)
                        {
                            sourceParticipant = crime.Events[partnerEventId.Value].SourceParticipantId;
                        }
                    } else if (ev.EventType == CrimeModel.EventType.SentMessage)
                    {
                        isHalfAnEvent = true;
                        partnerEventId = potentiallyValidEvents.FirstOrDefault(x =>
                            x.Value.MessageId == ev.MessageId &&
                            x.Value.EventType == CrimeModel.EventType.ReceivedMessage).Key;
                        sourceParticipant = ev.SourceParticipantId;
                        if (partnerEventId != null)
                        {
                            targetParticipant = crime.Events[partnerEventId.Value].SourceParticipantId;
                        }
                    } else if (ev.EventType == CrimeModel.EventType.ReceivedMessage)
                    {
                        isHalfAnEvent = true;
                        partnerEventId = potentiallyValidEvents.FirstOrDefault(x =>
                            x.Value.MessageId == ev.MessageId &&
                            x.Value.EventType == CrimeModel.EventType.SentMessage).Key;
                        targetParticipant = ev.SourceParticipantId;
                        if (partnerEventId != null)
                        {
                            sourceParticipant = crime.Events[partnerEventId.Value].SourceParticipantId;
                        }
                    } else if (ev.EventType == CrimeModel.EventType.MetWith)
                    {
                        isHalfAnEvent = true;
                        partnerEventId = potentiallyValidEvents.FirstOrDefault(x =>
                            x.Value.MessageId == ev.MessageId &&
                            x.Value.EventType == CrimeModel.EventType.WasMetBy).Key;
                        sourceParticipant = ev.SourceParticipantId;
                        if (partnerEventId != null)
                        {
                            targetParticipant = crime.Events[partnerEventId.Value].SourceParticipantId;
                        }
                    } else if (ev.EventType == CrimeModel.EventType.WasMetBy)
                    {
                        isHalfAnEvent = true;
                        partnerEventId = potentiallyValidEvents.FirstOrDefault(x =>
                            x.Value.MessageId == ev.MessageId &&
                            x.Value.EventType == CrimeModel.EventType.MetWith).Key;
                        targetParticipant = ev.SourceParticipantId;
                        if (partnerEventId != null)
                        {
                            sourceParticipant = crime.Events[partnerEventId.Value].SourceParticipantId;
                        }
                    }
                    
                    //if it's half a message but the other half isn't in the list, it's not valid
                    if (isHalfAnEvent && partnerEventId == null)
                    {
                        //it was one half of a message
                        continue;
                    }

                    if (!activeParticipants.Contains(sourceParticipant))
                    {
                        //the sender is not active
                        continue;
                    }

                    var eventsToCheckRequirementsOn = new List<CrimeModel.Event>()
                    {
                        ev
                    };
                    if (partnerEventId != null)
                    {
                        eventsToCheckRequirementsOn.Add(crime.Events[partnerEventId.Value]);
                    }

                    var isValid = true;
                    var itemsToAdd = new List<(int participant, int item)>();
                    var itemsToDestroy = new List<(int participant, int item)>();
                    foreach (var eventToCheck in eventsToCheckRequirementsOn)
                    {
                        var checkMovedItems = eventToCheck.EventType != CrimeModel.EventType.Bulletin &&
                                         eventToCheck.EventType != CrimeModel.EventType.ProcessItems;
                        
                        //check destroyed items
                        if (eventToCheck.DestroyedObjectIds.Count > 0)
                        {
                            itemsToDestroy.AddRange(eventToCheck.DestroyedObjectIds.Select(x => (sourceParticipant, x)));
                            if (participantItems.TryGetValue(sourceParticipant, out var ownedItems))
                            {
                                if (eventToCheck.DestroyedObjectIds.Any(x => !ownedItems.Contains(x)))
                                {
                                    isValid = false;
                                }
                            }
                            else
                            {
                                isValid = false;
                            }
                        }

                        if (!isValid)
                        {
                            break;
                        }
                        
                        //check received items
                        if (eventToCheck.ReceivedObjectIds.Count > 0)
                        {
                            itemsToAdd.AddRange(eventToCheck.ReceivedObjectIds.Select(x => (targetParticipant ?? sourceParticipant, x)));
                            if (checkMovedItems)
                            {
                                if (participantItems.TryGetValue(sourceParticipant, out var ownedItems))
                                {
                                    if (eventToCheck.ReceivedObjectIds.Any(x => !ownedItems.Contains(x)))
                                    {
                                        isValid = false;
                                    }
                                }
                                else
                                {
                                    isValid = false;
                                }
                            }
                        }

                        if (!isValid)
                        {
                            break;
                        }
                    }

                    if (!isValid)
                    {
                        continue;
                    }
                    
                    //it's valid
                    processedEvents.Add(id.Value);
                    //so is the partner event if that exists
                    if (partnerEventId != null)
                    {
                        processedEvents.Add(partnerEventId.Value);
                    }

                    activeParticipants.Add(sourceParticipant);
                    if (targetParticipant != null)
                    {
                        activeParticipants.Add(targetParticipant.Value);
                    }

                    var type = CrimeTimelineEvent.CrimeTimelineEventType.Unknown;
                    if (ev.EventType == CrimeModel.EventType.Bulletin || 
                        ev.EventType == CrimeModel.EventType.ProcessItems)
                    {
                        type = CrimeTimelineEvent.CrimeTimelineEventType.Action;
                    } else if (ev.EventType == CrimeModel.EventType.MetWith ||
                               ev.EventType == CrimeModel.EventType.WasMetBy)
                    {
                        type = CrimeTimelineEvent.CrimeTimelineEventType.Meeting;
                    } else if (ev.EventType == CrimeModel.EventType.SentMessage ||
                               ev.EventType == CrimeModel.EventType.ReceivedMessage)
                    {
                        type = CrimeTimelineEvent.CrimeTimelineEventType.Message;
                    } else if (ev.EventType == CrimeModel.EventType.ReceivedPackage ||
                               ev.EventType == CrimeModel.EventType.SentPackage)
                    {
                        type = CrimeTimelineEvent.CrimeTimelineEventType.Package;
                    }

                    var itemsCreated = new List<int>();
                    if (type == CrimeTimelineEvent.CrimeTimelineEventType.Action)
                    {
                        itemsCreated.AddRange(itemsToAdd.Select(x => x.item));
                    }

                    var itemsDestroyed = new List<int>();
                    itemsDestroyed.AddRange(itemsToDestroy.Select(x => x.item));

                    var itemsTransferred = new List<int>();
                    if (type == CrimeTimelineEvent.CrimeTimelineEventType.Message ||
                        type == CrimeTimelineEvent.CrimeTimelineEventType.Meeting ||
                        type == CrimeTimelineEvent.CrimeTimelineEventType.Package)
                    {
                        itemsTransferred.AddRange(itemsToAdd.Select(x => x.item));
                    }

                    if (!participantItems.ContainsKey(sourceParticipant))
                    {
                        participantItems[sourceParticipant] = new List<int>();
                    }
                    participantItems[sourceParticipant].AddRange(itemsCreated);
                    participantItems[sourceParticipant].RemoveAll(x => itemsDestroyed.Contains(x));
                    if (targetParticipant != null)
                    {
                        if (!participantItems.ContainsKey(targetParticipant.Value))
                        {
                            participantItems[targetParticipant.Value] = new List<int>();
                        }

                        participantItems[targetParticipant.Value].AddRange(itemsTransferred);
                        participantItems[sourceParticipant].RemoveAll(x => itemsTransferred.Contains(x));
                    }

                    foundAtLeastOneEvent = true;
                    var eventIds = new List<int>() { id.Value };
                    if (partnerEventId != null)
                    {
                        eventIds.Add(partnerEventId.Value);
                    }
                    timeline.Add(new CrimeTimelineEvent()
                    {
                        EventIds = eventIds,
                        Type = type,
                        SourceParticipantId = ev.SourceParticipantId,
                        TargetParticipantId = ev.TargetParticipantId,
                        ItemsCreated = itemsCreated,
                        ItemsDestroyed = itemsDestroyed,
                        ItemsTransferred = itemsTransferred
                    });
                }
                
                if (!foundAtLeastOneEvent)
                {
                    if (processedEvents.Count != crime.Events.Count)
                    {
                        //there are still events that were never processed
                        for (var i = 0; i < crime.Events.Count; i++)
                        {
                            if (!processedEvents.Contains(i))
                            {
                                timeline.Add(new CrimeTimelineEvent()
                                {
                                    Type = CrimeTimelineEvent.CrimeTimelineEventType.Error,
                                    ErrorMessage = $"Event {i} was never processed"
                                });
                            }
                        }
                    }
                    break;
                }
            } while (safetyCheck++ < 500);
            
            return timeline;
        }
    }
}