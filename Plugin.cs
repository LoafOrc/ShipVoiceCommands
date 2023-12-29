using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using VoiceRecognitionAPI;

namespace ShipVoiceCommands {
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency(VoiceRecognitionAPI.Plugin.modGUID)]
    public class ShipVoiceCommandsBase : BaseUnityPlugin {
        private const string modGUID = "me.loaforc.shipvoicecommands";
        private const string modName = "ShipVoiceCommands";
        private const string modVersion = "1.0.0";

        private readonly Harmony harmony = new Harmony(modGUID);
        internal static ShipVoiceCommandsBase instance;
        internal static ManualLogSource logger;

        internal static ConfigEntry<bool> ARE_COMMANDS_GLOBAL;

        internal static ConfigEntry<string> OPEN_DOORS_COMMAND;
        internal static ConfigEntry<string> CLOSE_DOORS_COMMAND;
        internal static ConfigEntry<string> ACTIVATE_TELEPORT_COMMAND;

        void Awake() {
            if (instance == null) instance = this; // Signleton
            else return; // Make sure nothing else gets loaded.
            logger = BepInEx.Logging.Logger.CreateLogSource(modGUID);

            logger.LogInfo("Setting up config.");
            ARE_COMMANDS_GLOBAL = Config.Bind(
                "Gameplay",
                "AreCommandsGlobal",
                false,
                "Are the voice commands global? Or do you need to be near the ship? It is recommend to play with this off."
            );

            OPEN_DOORS_COMMAND = Config.Bind(
                "Commands",
                "OpenShipDoors",
                "open doors,open ship doors",
                "What voice phrases should open the ship doors. Each phrase should be seperated with a comma"
            );

            CLOSE_DOORS_COMMAND = Config.Bind(
                "Commands",
                "CloseShipDoors",
                "close doors,close ship doors",
                "What voice phrases should close the ship doors. Each phrase should be seperated with a comma"
            );

            ACTIVATE_TELEPORT_COMMAND = Config.Bind(
                "Commands",
                "ActivateTeleporter",
                "teleport,teleport player",
                "What voice phrases should activate the ship teleporter. Each phrase should be seperated with a comma"
            );  

            logger.LogInfo("Registering voice commands!");

            Voice.ListenForPhrases(OPEN_DOORS_COMMAND.Value.Split(','), (message) => {
                if (!IsPlayerWithinShipBounds()) return;
                logger.LogInfo("Opening ship doors..");
                HangarShipDoor door = UnityEngine.Object.FindFirstObjectByType<HangarShipDoor>();

                if(door != null) {
                    InteractTrigger trigger = door.transform.Find("HangarDoorButtonPanel/StartButton/Cube (2)").GetComponent<InteractTrigger>();
                    TriggerButton(trigger);
                }
            });

            Voice.ListenForPhrases(CLOSE_DOORS_COMMAND.Value.Split(','), (message) => {
                if (!IsPlayerWithinShipBounds()) return;
                logger.LogInfo("Closing ship doors..");
                HangarShipDoor door = UnityEngine.Object.FindFirstObjectByType<HangarShipDoor>();

                if (door != null) {
                    InteractTrigger trigger = door.transform.Find("HangarDoorButtonPanel/StopButton/Cube (3)").GetComponent<InteractTrigger>();
                    TriggerButton(trigger);
                }
            });

            Voice.ListenForPhrases(ACTIVATE_TELEPORT_COMMAND.Value.Split(','), (message) => {
                if (!IsPlayerWithinShipBounds()) return;
                logger.LogInfo("Activating Ship Teleporter");

                GameObject button = GameObject.Find("Teleporter(Clone)/ButtonContainer/ButtonAnimContainer/RedButton");
                if(button != null) {
                    TriggerButton(button.GetComponent<InteractTrigger>());
                }
            });

            //InverseTeleporter(Clone)/ButtonContainer/ButtonAnimContainer/RedButton

            logger.LogInfo(modName + ":" + modVersion + " has succesfully loaded!");
        }

        void TriggerButton(InteractTrigger trigger) {
            trigger.Interact(GameNetworkManager.Instance.localPlayerController.transform);
        }

        bool IsPlayerWithinShipBounds() {
            if(ARE_COMMANDS_GLOBAL.Value) return true; // We don't even need to bother checking if the player is in the ship (+ it's performance heavy to do that)
            StartOfRound playersManager = UnityEngine.Object.FindFirstObjectByType<StartOfRound>();
            if (playersManager == null) return false; // Umm maybe dont try and use the voice commands if we aren't in game champ
            return playersManager.shipBounds.bounds.Contains(GameNetworkManager.Instance.localPlayerController.transform.position);
        }
    }
}
