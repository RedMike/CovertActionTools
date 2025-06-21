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
        public int Iteration { get; set; }
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
            //TODO: how does the game figure out who is first active?
            for (var i = 0; i < crime.Participants.Count; i++)
            {
                if (crime.Participants[i].IsMastermind)
                {
                    activeParticipants.Add(i);
                }
            }
            activeParticipants.Add(0); //the first participant is the organizer usually
            var participantItems = new Dictionary<int, HashSet<int>>();
            var destroyedItems = new HashSet<int>();
            var processedEvents = new HashSet<int>();
            var safetyCheck = 0;
            var iteration = 0;
            
            //TODO: bugs
            //  crime 6 event 13 - the bomber is receiving the photographs they're meant to give
            //  crime 9 events 11/12 - bagman receiving things they're meant to give
            //  crime 10 event 11 - extractor not receiving escapee
            //  crime 3 - activation order is clearly wrong
            
            do
            {
                iteration++;
                var busyParticipants = new HashSet<int>();
                
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
                    
                    var itemParticipant = ev.MainParticipantId;
                    var otherParticipant = ev.SecondaryParticipantId ?? ev.MainParticipantId;
                    if (ev.ItemsToSecondary)
                    {
                        itemParticipant = ev.SecondaryParticipantId ?? ev.MainParticipantId;
                        otherParticipant = ev.MainParticipantId;
                    }
                    if ((ev.IsPairedEvent && !activeParticipants.Contains(otherParticipant)) ||
                        (!ev.IsPairedEvent && !activeParticipants.Contains(ev.MainParticipantId)))
                    {
                        //the sender is not active
                        continue;
                    }
                    
                    if (busyParticipants.Contains(itemParticipant) || busyParticipants.Contains(otherParticipant))
                    {
                        //if the participant is already busy, we can't do anything this iteration
                        //this is just to simulate messages happening across multiple days
                        continue;
                    }

                    if (!participantItems.ContainsKey(ev.MainParticipantId))
                    {
                        participantItems[ev.MainParticipantId] = new HashSet<int>();
                    }

                    if (ev.SecondaryParticipantId != null)
                    {
                        if (!participantItems.ContainsKey(ev.SecondaryParticipantId.Value))
                        {
                            participantItems[ev.SecondaryParticipantId.Value] = new HashSet<int>();
                        }
                    }

                    //destroyed objects must exist on the right participant before being allowed
                    if (ev.DestroyedObjectIds.Any())
                    {
                        if (ev.DestroyedObjectIds.Any(x => !destroyedItems.Contains(x) && !participantItems[otherParticipant].Contains(x)))
                        {
                            //not owned destroyed item
                            continue;
                        }
                    }
                    
                    //normally, received items on paired events must exist on the right participant before being allowed
                    if (ev.IsPairedEvent && ev.ReceivedObjectIds.Any())
                    {
                        var requirementsMet = true;
                        foreach (var objId in ev.ReceivedObjectIds)
                        {
                            if (participantItems[otherParticipant].Contains(objId))
                            {
                                //the item is owned correctly
                                continue;
                            }
                            //if a received item doesn't exist on any individual event, then the item requirement is skipped
                            if (crime.Events.All(x => x.IsPairedEvent || !x.ReceivedObjectIds.Contains(objId)))
                            {
                                continue;
                            }

                            requirementsMet = false;
                            break;
                        }

                        if (!requirementsMet)
                        {
                            //not owned received item
                            continue;
                        }
                    }
                    
                    //it's valid
                    foundAtLeastOneEvent = true;
                    processedEvents.Add(id.Value);
                    
                    //mark participant as busy in some situations
                    if (ev.IsMeeting)
                    {
                        //mark both as busy
                        busyParticipants.Add(ev.MainParticipantId);
                        busyParticipants.Add(ev.SecondaryParticipantId ?? ev.MainParticipantId);
                    }
                    else
                    {
                        //mark only the recipient as busy
                        busyParticipants.Add(ev.MainParticipantId);
                    }

                    //activate both participants
                    activeParticipants.Add(ev.MainParticipantId);
                    if (ev.SecondaryParticipantId != null)
                    {
                        activeParticipants.Add(ev.SecondaryParticipantId.Value);
                    }
                    
                    //add/move items
                    destroyedItems.UnionWith(ev.DestroyedObjectIds);
                    participantItems[itemParticipant].UnionWith(ev.ReceivedObjectIds);
                    foreach (var participantPair in participantItems)
                    {
                        if (participantPair.Key == itemParticipant)
                        {
                            continue;
                        }

                        participantPair.Value.RemoveWhere(x => ev.DestroyedObjectIds.Contains(x));
                        participantPair.Value.RemoveWhere(x => ev.ReceivedObjectIds.Contains(x));
                    }
                    
                    timeline.Add(new CrimeTimelineEvent()
                    {
                        Iteration = iteration,
                        EventIds = new List<int>() { id.Value },
                        Type = ev.Type switch
                        {
                            CrimeModel.EventType.Individual => CrimeTimelineEvent.CrimeTimelineEventType.Action,
                            CrimeModel.EventType.Message => CrimeTimelineEvent.CrimeTimelineEventType.Message,
                            CrimeModel.EventType.Package => CrimeTimelineEvent.CrimeTimelineEventType.Package,
                            CrimeModel.EventType.Meeting => CrimeTimelineEvent.CrimeTimelineEventType.Meeting,
                            _ => CrimeTimelineEvent.CrimeTimelineEventType.Unknown
                        },
                        SourceParticipantId = ev.IsPairedEvent ? ev.SecondaryParticipantId ?? ev.MainParticipantId : ev.MainParticipantId,
                        TargetParticipantId = ev.IsPairedEvent ? ev.MainParticipantId : null,
                        MessageId = ev.MessageId,
                        ItemsCreated = ev.IsPairedEvent ? new() : ev.ReceivedObjectIds.OrderBy(x => x).ToList(),
                        ItemsDestroyed = ev.DestroyedObjectIds.OrderBy(x => x).ToList(),
                        ItemsTransferred = ev.IsPairedEvent ? ev.ReceivedObjectIds.OrderBy(x => x).ToList() : new(),
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
                                    ErrorMessage = $"Event {i + 1} was never processed"
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
                                    ErrorMessage = $"Participant {i + 1} was never activated"
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