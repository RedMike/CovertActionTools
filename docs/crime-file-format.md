# CRIME file format

This file stores information about each individual "crime" in the game, where the "crime" specifically
refers to the setup involving a set of participants/events/objects, which ends up being turned into a 
timeline as time progresses. 

The data stored is very simple and generally involves constraints (e.g. this participant is always female, 
this event requires object X on participant Y in order to run), and does **not** include specific information 
such as the victim/locations.

There is an additional layer of data embedded in the EXE files which stores the specific information, which 
means that the same crime can be re-used for a different victim/location as-is; in this case, the participant 
roles/constraints/events/object icons are re-used for all instances.

## Participants

Each participant in a crime is one of the named people described on the timeline at the end of a session,
except red herrings found during the crime (which are not included in this data, just generated by the game).
The things a participant has are:

A role, which is a string describing their role within the crime itself; the same string 
is used to describe very different behaviour in different crimes (e.g. an `Organizer` in one crime might do 
something very different from an `Organizer` in another). The string is arbitrary and only used to display the
role in-game.

An exposure value, which is a relative number that determines how likely they are to have clues found for
their location/affiliation/etc.

A set of flags that determine things like: if they are the mastermind, if they are forced to be female, 
if they have the ability to go into/come out of hiding whenever possible, if they are an inside contact.

A set of flags that seem to determine location and organization mapping; this allows a crime to specify
that participant X is in the same organisation as participant Y, or that participant Z is in the same 
location as participant Q. If not specifically set, the game will generate these completely randomly, 
which makes the game very difficult (due to travel times and missing events). **Note: this functionality has
not been fully mapped out.**

The "Clue Type" used for the participant, which is the single exclusive way the participant will be flagged
to the player via one of the informant/surveillance/etc clues (e.g. "vehicle registered to X" type clues).

A rank value which is used only to determine score values, and to display the rank in-game.

A number of clearly-unused fields in the game, which have values only in some early crimes; most likely
functionality that was removed.

### Clues/Player Knowledge

Each participant in a crime during a crime has a number of bits of information that can be collected separately
about them, some of which are pure fluff and not relevant to the game, and some which are considered important.

The important bits of information are:

* Name
* Location
* Organisation
* Role - knowing this is what marks them as a participant and allows arrest

The "Clue Type" on the participant data relates to one or more of the first 3, and **cannot** provide the Role.

In addition, which ones it provides is dependent not just on the data inside CRIME files, but also the CLUES file; in
the CLUES file there are two sets of entries that relate to this:

* `C<clue type><id>` which is a non-crime-specific clue that relates to the given clue type (e.g. "was used at airport"
for a flight ticket)
* `C<crime ID><participant ID>` which is a crime-specific clue and includes the clue type and a message; **this means
that changes to these clues should ALSO change the associated clues data**

## Objects

Each object in a crime is one of the items that is generated/used/destroyed by a participant/event. These are things
that can be found using in-game computer terminals, or via random surveillance/clues. Within each crime, they have
a name and icon, where the name is an arbitrary string, and the icon matches the entry in the relevant sprites image.

An object can either not exist at all, or exist on a specific participant, but copies cannot exist. However, the game
does not enforce correct behaviour when it comes to objects "teleporting" from participant to participant. This is 
evident from the game data files themselves, where the object usage is unreliable and trying to handle it correctly
actually leads to errors because the game expects the objects to teleport occasionally.

An object will begin existing as soon as an event is set up to create it.

## Events

Each event in a crime is one of the potential actions that a participant can take. There are two types of events:

1. Individual events: these are events where a participant does something on their own, most commonly using an item
or destroying it, or combining two items into one, or simply running a crime. 
2. Paired events: these are events where two participants do something to one another, sometimes including item usage
or movement. Two separate events are generated for each pair, **and in the actual game data files, these do not 
always match correctly**. In particular, the data is very unreliable around item usage/location usage. Worse, there
are a number of events described in the game data files which will never actually run because their requirements
never get fulfilled; these are most definitely bugs.

In general, an event happens at a specific location (for paired events, it depends on how the two events are defined,
but generally the 'receiver' is what gets used) for the purposes of airport surveillance/message traffic/etc, as 
well as things like bugging/wire-tapping a safehouse. 

Events also have a set of flags defining what type of event they are, with the main ones being:
* Action if nothing is set (mostly just individual events for running crimes)
* Message, which means it will be detected by wire-tapping/bugging
* Meeting, which means it will be detected by bugging/following
* Package, which may not be detectable
* Bulletin, which means the message gets shown to the player directly; crimes are usually done as bulletins

They also have a score, which impacts the player score if the event is allowed to run. There is a minimum value of
score that must be run on a crime in order to count the crime as "successful", which is the source of the bug in the
"Ultimate Plot" crime where not enough score exists to count the crime as "successful" (so the crime repeats on loss).

And lastly, each event has a set of received and destroyed object IDs. Destroyed objects are removed from the game,
but received objects are simply teleported to the receiver (even if they currently exist elsewhere and the sender
does not have them). Individual events with a received event are how items are generated normally, but they are also
sometimes generated directly in a paired event.

## Timeline

The full logic of the event constraints/etc has not been fully mapped out, nor has the logic for things like which
participant is active or not at the start of a crime, however in general the logic is:

* The crime starts with at least the first participant active, potentially it picks the first event in the list to
make its participants active
* Any message received by a non-active participant will make them active too
* An event is eligible to run if all its item requirements are met (for destroying items), and its sender is active
* In-game there is generally a chance to run one event at every midnight, but multiple will not run (at least involving
the same participants)
* If the participant has the 'Can go into hiding at will' flag set, or if the participant has no more events at all,
instead of running an event they can go into hiding
* If the participant has the 'Can go into hiding at will' flag set, then they continue being eligible to run events
and can 'come out of hiding' to run events




