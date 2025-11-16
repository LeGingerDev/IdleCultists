Relatively to the point, unless specified, usually talking about Unity and stuff around games development, give explanations about tough subjects and be understanding that I have no clue about a lot of stuff :D

I program in C#, Unity specifically.
Assets I utilise - Odin Inspector, Dotween
Utilise IEnumerators and Coroutines to stop and start loops.
I hate the update loop.
Private variables always start with '\_'
Variables will be private with SerializeField and FoldoutGroups
I want SOLID principles, making sure code is structured nicely. Functions for separated and reusable logic.
I like Static Extension methods and classes for handling repetitive work
If it needs accessing from outside, I prefer Getter functions over getter proeperties.
I don't like using Static event Action for stuff. I have a built in system instead, so if you need static events, make sure to add a comment above it to tell me to change to "Topic System"

I have a topic system for events you can find the folder is called Events and it has a set of scripts designed to handle events in a decoupled way. Use that instead of static events.

Major things to know about the Topic System:
Functions that listen to topics need to be public.
The first parameter of the function needs to be "object sender"
The BaseBehaviour script has built in functions to subscribe and unsubscribe to topics. So if you need to use topics instead of static scripts. Make sure to inherit from BaseBehaviour.

Code Structure.
I try to separate systems into their own folders and namespaces with the same set of subfolders in it.
Namespaces are typically based on the folder structure ignoring the word "Scripts"
So if I have a script in "Scripts/Gameplay/Player/Components" the namespace would be "Gameplay.Player.Components"
The only exception is when it's in the !LGD folder. The namespace would start with "LGD" followed by the system folder it's in. So "Assets/!LGD/Systems/Inventory/Managers" would have the namespace "LGD.Inventory.Managers"

Same applies to files.
Manager Scripts go in a folder called "Managers"
Components that sit on GameObjects go in a folder called "Components"
Data files, Enums and ScriptableObjects go in a folder called "Models"
Utility and helper scripts go in a folder called "Utilities"
Tasks using the LGD Task System go in a folder called "Tasks"
Interfaces go in a folder called "Interfaces"
Lastly for a static event Ids script for the Topic System, it sits in the route of the system folder. So "Assets/!LGD/Systems/Inventory/InventoryEventIds.cs"

Debugs need to use the DebugManager system found in "!LGD/Systems/DebugSystem"
And when debugging I'd like the first part of the debug to be the system it's in as this makes it easier to debug in console. So for example if I'm debugging something in the Resources system I'd do: DebugManager.Log("[ResourcesSystem] Resource added successfully"); or for the Core System I'd do DebugManager.Log("[Core] Service started");
