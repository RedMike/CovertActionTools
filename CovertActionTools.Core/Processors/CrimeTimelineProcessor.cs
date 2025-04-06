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
            ItemUpdate = 5,
            
            Error = 100
        }

        public CrimeTimelineEventType Type { get; set; } = CrimeTimelineEventType.Unknown;
        public List<int> EventIds { get; set; } = new();
        public int SourceParticipantId { get; set; } = -1;
        public int? TargetParticipantId { get; set; } = null;
        public List<int> ItemsCreated { get; set; } = new List<int>();
        public List<int> ItemsTransferred { get; set; } = new List<int>();
        public List<int> ItemsDestroyed { get; set; } = new List<int>();
        public int MessageId { get; set; } = -1;
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
            var participantItems = new Dictionary<int, HashSet<int>>();
            var processedEvents = new HashSet<int>();
            var safetyCheck = 0;
            
            do
            {
                //exclude any events we've already processed
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

                    if (processedEvents.Contains(pair.Key.Value))
                    {
                        //we added this message in the same loop
                        continue;
                    }
                    var id = pair.Key;
                    var ev = pair.Value;
                    
                    //figure out the actual source/target and requirements/item changes (because of the sent/received split)
                    var sourceParticipant = ev.SourceParticipantId;
                    var targetParticipant = ev.TargetParticipantId;
                    var isMeeting = false;
                    var sourceRequiredItems = new HashSet<int>();
                    var itemsToAddOrTransfer = new HashSet<int>();
                    var transferItems = false;
                    var itemsToRemove = new HashSet<int>();
                    
                    int? partnerEventId = null;
                    var isSplitEvent = false;
                    if (ev.EventType == CrimeModel.EventType.SentPackage)
                    {
                        isSplitEvent = true;
                        partnerEventId = potentiallyValidEvents.FirstOrDefault(x =>
                            x.Value.MessageId == ev.MessageId &&
                            x.Value.EventType == CrimeModel.EventType.ReceivedPackage).Key;
                        sourceParticipant = ev.SourceParticipantId;
                        sourceRequiredItems.UnionWith(ev.DestroyedObjectIds);
                        itemsToAddOrTransfer.UnionWith(ev.ReceivedObjectIds);
                        itemsToRemove.UnionWith(ev.DestroyedObjectIds);
                        if (partnerEventId != null)
                        {
                            targetParticipant = crime.Events[partnerEventId.Value].SourceParticipantId;
                            sourceRequiredItems.UnionWith(crime.Events[partnerEventId.Value].ReceivedObjectIds);
                            itemsToAddOrTransfer.UnionWith(crime.Events[partnerEventId.Value].ReceivedObjectIds);
                            itemsToRemove.UnionWith(crime.Events[partnerEventId.Value].DestroyedObjectIds);
                        }
                    } else if (ev.EventType == CrimeModel.EventType.ReceivedPackage)
                    {
                        isSplitEvent = true;
                        partnerEventId = potentiallyValidEvents.FirstOrDefault(x =>
                            x.Value.MessageId == ev.MessageId &&
                            x.Value.EventType == CrimeModel.EventType.SentPackage).Key;
                        targetParticipant = ev.SourceParticipantId;
                        sourceRequiredItems.UnionWith(ev.ReceivedObjectIds);
                        itemsToAddOrTransfer.UnionWith(ev.ReceivedObjectIds);
                        itemsToRemove.UnionWith(ev.DestroyedObjectIds);
                        if (partnerEventId != null)
                        {
                            sourceParticipant = crime.Events[partnerEventId.Value].SourceParticipantId;
                            sourceRequiredItems.UnionWith(crime.Events[partnerEventId.Value].DestroyedObjectIds);
                            itemsToAddOrTransfer.UnionWith(crime.Events[partnerEventId.Value].ReceivedObjectIds);
                            itemsToRemove.UnionWith(crime.Events[partnerEventId.Value].DestroyedObjectIds);
                        }
                    } else if (ev.EventType == CrimeModel.EventType.SentMessage)
                    {
                        isSplitEvent = true;
                        partnerEventId = potentiallyValidEvents.FirstOrDefault(x =>
                            x.Value.MessageId == ev.MessageId &&
                            (x.Value.EventType == CrimeModel.EventType.ReceivedMessage ||
                             x.Value.EventType == CrimeModel.EventType.OddReceivedMessage
                            )).Key;
                        sourceParticipant = ev.SourceParticipantId;
                        sourceRequiredItems.UnionWith(ev.DestroyedObjectIds);
                        itemsToAddOrTransfer.UnionWith(ev.ReceivedObjectIds);
                        itemsToRemove.UnionWith(ev.DestroyedObjectIds);
                        if (partnerEventId != null)
                        {
                            targetParticipant = crime.Events[partnerEventId.Value].SourceParticipantId;
                            sourceRequiredItems.UnionWith(crime.Events[partnerEventId.Value].ReceivedObjectIds);
                            itemsToAddOrTransfer.UnionWith(crime.Events[partnerEventId.Value].ReceivedObjectIds);
                            itemsToRemove.UnionWith(crime.Events[partnerEventId.Value].DestroyedObjectIds);
                        }
                    } else if (ev.EventType == CrimeModel.EventType.ReceivedMessage || ev.EventType == CrimeModel.EventType.OddReceivedMessage)
                    {
                        isSplitEvent = true;
                        partnerEventId = potentiallyValidEvents.FirstOrDefault(x =>
                            x.Value.MessageId == ev.MessageId &&
                            x.Value.EventType == CrimeModel.EventType.SentMessage).Key;
                        targetParticipant = ev.SourceParticipantId;
                        sourceRequiredItems.UnionWith(ev.ReceivedObjectIds);
                        itemsToAddOrTransfer.UnionWith(ev.ReceivedObjectIds);
                        itemsToRemove.UnionWith(ev.DestroyedObjectIds);
                        if (partnerEventId != null)
                        {
                            sourceParticipant = crime.Events[partnerEventId.Value].SourceParticipantId;
                            sourceRequiredItems.UnionWith(crime.Events[partnerEventId.Value].DestroyedObjectIds);
                            itemsToAddOrTransfer.UnionWith(crime.Events[partnerEventId.Value].ReceivedObjectIds);
                            itemsToRemove.UnionWith(crime.Events[partnerEventId.Value].DestroyedObjectIds);
                        }
                    } else if (ev.EventType == CrimeModel.EventType.MetWith)
                    {
                        isSplitEvent = true;
                        isMeeting = true; //meetings activate people in reverse
                        partnerEventId = potentiallyValidEvents.FirstOrDefault(x =>
                            x.Value.MessageId == ev.MessageId &&
                            x.Value.EventType == CrimeModel.EventType.WasMetBy).Key;
                        //sourceParticipant = ev.SourceParticipantId;
                        targetParticipant = ev.SourceParticipantId;
                        sourceRequiredItems.UnionWith(ev.ReceivedObjectIds);
                        sourceRequiredItems.UnionWith(ev.DestroyedObjectIds);
                        transferItems = true;
                        itemsToAddOrTransfer.UnionWith(ev.ReceivedObjectIds);
                        itemsToRemove.UnionWith(ev.DestroyedObjectIds);
                        if (partnerEventId != null)
                        {
                            //targetParticipant = crime.Events[partnerEventId.Value].SourceParticipantId;
                            sourceParticipant = crime.Events[partnerEventId.Value].SourceParticipantId;
                            sourceRequiredItems.UnionWith(crime.Events[partnerEventId.Value].ReceivedObjectIds);
                            sourceRequiredItems.UnionWith(crime.Events[partnerEventId.Value].DestroyedObjectIds);
                            itemsToAddOrTransfer.UnionWith(crime.Events[partnerEventId.Value].ReceivedObjectIds);
                            itemsToRemove.UnionWith(crime.Events[partnerEventId.Value].DestroyedObjectIds);
                        }
                    } else if (ev.EventType == CrimeModel.EventType.WasMetBy)
                    {
                        isSplitEvent = true;
                        isMeeting = true; //meetings activate people in reverse
                        partnerEventId = potentiallyValidEvents.FirstOrDefault(x =>
                            x.Value.MessageId == ev.MessageId &&
                            x.Value.EventType == CrimeModel.EventType.MetWith).Key;
                        //targetParticipant = ev.SourceParticipantId;
                        sourceParticipant = ev.SourceParticipantId;
                        sourceRequiredItems.UnionWith(ev.ReceivedObjectIds);
                        sourceRequiredItems.UnionWith(ev.DestroyedObjectIds);
                        transferItems = true;
                        itemsToAddOrTransfer.UnionWith(ev.ReceivedObjectIds);
                        itemsToRemove.UnionWith(ev.DestroyedObjectIds);
                        if (partnerEventId != null)
                        {
                            //sourceParticipant = crime.Events[partnerEventId.Value].SourceParticipantId;
                            targetParticipant = crime.Events[partnerEventId.Value].SourceParticipantId;
                            sourceRequiredItems.UnionWith(crime.Events[partnerEventId.Value].ReceivedObjectIds);
                            sourceRequiredItems.UnionWith(crime.Events[partnerEventId.Value].DestroyedObjectIds);
                            itemsToAddOrTransfer.UnionWith(crime.Events[partnerEventId.Value].ReceivedObjectIds);
                            itemsToRemove.UnionWith(crime.Events[partnerEventId.Value].DestroyedObjectIds);
                        }
                    } else if (ev.EventType == CrimeModel.EventType.Bulletin ||
                               ev.EventType == CrimeModel.EventType.ProcessItems)
                    {
                        sourceRequiredItems.UnionWith(ev.DestroyedObjectIds);
                        itemsToAddOrTransfer.UnionWith(ev.ReceivedObjectIds);
                        itemsToRemove.UnionWith(ev.DestroyedObjectIds);
                    }
                    
                    //if it's half a message but the other half isn't in the list, it's not valid
                    if (isSplitEvent && partnerEventId == null)
                    {
                        //it was one half of a message
                        continue;
                    }
                    
                    if (sourceRequiredItems.Count > 0)
                    {
                        participantItems.TryGetValue(sourceParticipant, out var sourceItems);
                        if (sourceItems == null)
                        {
                            sourceItems = new HashSet<int>();
                        }

                        var targetItems = new HashSet<int>();
                        if (targetParticipant != null)
                        {
                            participantItems.TryGetValue(targetParticipant!.Value, out targetItems);
                            if (targetItems == null)
                            {
                                targetItems = new HashSet<int>();
                            }
                        }
                        
                        //for meetings, either source or target can have the required items
                        if (transferItems)
                        {
                            if (sourceRequiredItems.Any(x => !sourceItems.Contains(x) && !targetItems.Contains(x)))
                            {
                                //required items not owned
                                continue;
                            }

                            if (sourceRequiredItems.Any(x => !sourceItems.Contains(x)))
                            {
                                //the source and target are flipped
                                (sourceParticipant, targetParticipant) = (targetParticipant!.Value, sourceParticipant);
                            }
                        }
                        else
                        {
                            if (sourceRequiredItems.Any(x => !sourceItems.Contains(x)))
                            {
                                //required items not owned
                                continue;
                            }
                        }
                        if (!participantItems.TryGetValue(sourceParticipant, out var items) || sourceRequiredItems.Any(x => !items.Contains(x)))
                        {
                            //required items not owned
                            continue;
                        }
                    }

                    if (!activeParticipants.Contains(sourceParticipant))
                    {
                        //the sender is not active
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
                               ev.EventType == CrimeModel.EventType.ReceivedMessage || 
                               ev.EventType == CrimeModel.EventType.OddReceivedMessage)
                    {
                        type = CrimeTimelineEvent.CrimeTimelineEventType.Message;
                    } else if (ev.EventType == CrimeModel.EventType.ReceivedPackage ||
                               ev.EventType == CrimeModel.EventType.SentPackage)
                    {
                        type = CrimeTimelineEvent.CrimeTimelineEventType.Package;
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
                        SourceParticipantId = sourceParticipant,
                        TargetParticipantId = targetParticipant,
                        MessageId = ev.MessageId,
                        ItemsCreated = isSplitEvent ? new List<int>() : itemsToAddOrTransfer.ToList(),
                        ItemsDestroyed = itemsToRemove.ToList(),
                        ItemsTransferred = isSplitEvent ? itemsToAddOrTransfer.ToList() : new List<int>()
                    });

                    if (!participantItems.ContainsKey(sourceParticipant))
                    {
                        participantItems[sourceParticipant] = new HashSet<int>();
                    }

                    if (itemsToAddOrTransfer.Count > 0)
                    {
                        if (!isSplitEvent)
                        {
                            timeline.Add(new CrimeTimelineEvent()
                            {
                                Type = CrimeTimelineEvent.CrimeTimelineEventType.ItemUpdate,
                                ErrorMessage =
                                    $"{crime.Participants[sourceParticipant].Role.Trim()} now has [{string.Join(", ", itemsToAddOrTransfer.Select(x => crime.Objects[x].Name.Trim()))}]"
                            });
                            participantItems[sourceParticipant].UnionWith(itemsToAddOrTransfer);
                        }
                        else
                        {
                            if (!participantItems.ContainsKey(targetParticipant!.Value))
                            {
                                participantItems[targetParticipant.Value] = new HashSet<int>();
                            }

                            var itemInSource = itemsToAddOrTransfer.All(x => participantItems[sourceParticipant].Contains(x));

                            if (itemInSource)
                            {
                                timeline.Add(new CrimeTimelineEvent()
                                {
                                    Type = CrimeTimelineEvent.CrimeTimelineEventType.ItemUpdate,
                                    ErrorMessage = $"{crime.Participants[targetParticipant.Value].Role.Trim()} now has [{string.Join(", ", itemsToAddOrTransfer.Select(x => crime.Objects[x].Name.Trim()))}]"
                                });
                                participantItems[targetParticipant.Value].UnionWith(itemsToAddOrTransfer);
                                participantItems[sourceParticipant].RemoveWhere(x => itemsToAddOrTransfer.Contains(x));
                            }
                            else
                            {
                                timeline.Add(new CrimeTimelineEvent()
                                {
                                    Type = CrimeTimelineEvent.CrimeTimelineEventType.ItemUpdate,
                                    ErrorMessage = $"{crime.Participants[sourceParticipant].Role.Trim()} now has [{string.Join(", ", itemsToAddOrTransfer.Select(x => crime.Objects[x].Name.Trim()))}]"
                                });
                                participantItems[sourceParticipant].UnionWith(itemsToAddOrTransfer);
                                participantItems[targetParticipant.Value].RemoveWhere(x => itemsToAddOrTransfer.Contains(x));
                            }
                        }
                    }

                    if (itemsToRemove.Count > 0)
                    {
                        if (false && isMeeting)
                        {
                            timeline.Add(new CrimeTimelineEvent()
                            {
                                Type = CrimeTimelineEvent.CrimeTimelineEventType.ItemUpdate,
                                ErrorMessage = $"{crime.Participants[targetParticipant!.Value].Role.Trim()} no longer has [{string.Join(", ", itemsToRemove.Select(x => crime.Objects[x].Name.Trim()))}]"
                            });
                            participantItems[targetParticipant!.Value].RemoveWhere(x => itemsToRemove.Contains(x));
                        }
                        else
                        {
                            timeline.Add(new CrimeTimelineEvent()
                            {
                                Type = CrimeTimelineEvent.CrimeTimelineEventType.ItemUpdate,
                                ErrorMessage = $"{crime.Participants[sourceParticipant].Role.Trim()} no longer has [{string.Join(", ", itemsToRemove.Select(x => crime.Objects[x].Name.Trim()))}]"
                            });
                            participantItems[sourceParticipant].RemoveWhere(x => itemsToRemove.Contains(x));
                        }
                    }
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
                                    ErrorMessage = $"Event {i} was never processed: {crime.Events[i].EventType} {crime.Events[i].SourceParticipantId} to {crime.Events[i].TargetParticipantId} {crime.Events[i].MessageId} received [{string.Join(", ", crime.Events[i].ReceivedObjectIds)}] destroyed [{string.Join(", ", crime.Events[i].DestroyedObjectIds)}]"
                                });
                            }
                        }
                    }

                    if (activeParticipants.Count != crime.Participants.Count)
                    {
                        //there are still participants that were never active
                        for (var i = 0; i < crime.Participants.Count; i++)
                        {
                            if (!activeParticipants.Contains(i))
                            {
                                timeline.Add(new CrimeTimelineEvent()
                                {
                                    Type = CrimeTimelineEvent.CrimeTimelineEventType.Error,
                                    ErrorMessage = $"Participant {i} was never activated"
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