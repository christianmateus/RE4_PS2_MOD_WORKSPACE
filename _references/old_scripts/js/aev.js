const fs = require('fs');
const { ipcRenderer } = require('electron');

// Const for testing text output (DEBUG)

/* ===============
    CREATE
   =============== */

// Getting elements
var rootEl = document.querySelector("html");
var addNewEventEl = document.getElementById("addEntry");
var removeEventEl = document.getElementById("removeEntry");
var copyBtnEl = document.getElementById("copyBtn");
var pasteBtnEl = document.getElementById("pasteBtn");
var undoBtnEl = document.getElementById("undoBtn");
var redoBtnEl = document.getElementById("redoBtn");
var containerMainConfig = document.querySelector(".container-main-config");
var containerEventConfig = document.querySelector(".container-event-config");
var countEl = document.getElementById("count");
var indexEl = document.querySelector(".index");
var eventNumber = document.getElementById("event-changer-input");
var nextBtn = document.getElementById("nextBtn");
var prevBtn = document.getElementById("prevBtn");
var eventConfig = document.getElementsByClassName("event-config");
var headerFileName = document.getElementById("header-filename");
var headerFileSize = document.getElementById("header-filesize");
var mainHeader = document.getElementById("campo-superior");

// Getting elements from Main Config
var eventIdSelect = document.querySelector(".selectEventName")
var unk1El = document.querySelector("#unk1");
var unk2El = document.querySelector("#unk2");
var unk3El = document.querySelector("#unk3");
var topLeftXEl = document.querySelector(".top-leftX");
var topLeftZEl = document.querySelector(".top-leftZ");
var topRightXEl = document.querySelector(".top-rightX");
var topRightZEl = document.querySelector(".top-rightZ");
var bottomLeftXEl = document.querySelector(".bottom-leftX");
var bottomLeftZEl = document.querySelector(".bottom-leftZ");
var bottomRightXEl = document.querySelector(".bottom-rightX");
var bottomRightZEl = document.querySelector(".bottom-rightZ");
var lowerLimitEl = document.querySelector(".lower-limit");
var higherLimitEl = document.querySelector(".higher-limit");

// Getting elements from definition bytes
var definitionByte1 = document.querySelector("#def1");
var definitionByte2 = document.querySelector("#def2");
var definitionByte3 = document.querySelector("#def3");
var definitionByte4 = document.querySelector("#def4");

// Getting elements from event type 1
var teleportXEl = document.querySelector("#teleport-x");
var teleportYEl = document.querySelector("#teleport-y");
var teleportZEl = document.querySelector("#teleport-z");
var teleportFacingAngleEl = document.querySelector("#teleport-rotation");
var roomSelectEl = document.querySelector(".room-select");
var roomImage = document.querySelector(".room-image");

// Getting elements from event type 2
var offset88El = document.getElementById("offset88");
var offset89El = document.getElementById("offset89");
var offset90El = document.getElementById("offset90");

// Getting elements from event type 4
var enemyGroupEl = document.getElementById("enemyGroupByte");

// Getting elements from event type 5
var messageEl = document.getElementById("message");
var messageCameraEl = document.getElementById("camera-offset");
var messageSoundEl = document.getElementById("sound-offset");

// Getting elements from event type A
var reactionTimeEl = document.getElementById("reaction-time");
var damageAmountEl = document.getElementById("damage-amount");
var animationTypeEl = document.getElementById("animation-type");
var damageOriginEl = document.getElementById("damage-origin");

// Getting elements from event type 10
var ladderXEl = document.getElementById("ladder-x");
var ladderYEl = document.getElementById("ladder-y");
var ladderZEl = document.getElementById("ladder-z");
var ladderFacingAngleEl = document.getElementById("ladder-rotation");
var ladderStepsEl = document.getElementById("ladder-steps");
var ladderCameraStartEl = document.getElementById("cam-param3");
var ladderCameraEndEl = document.getElementById("cam-param4");
var ladderUnknown1El = document.getElementById("cam-param1")
var ladderUnknown2El = document.getElementById("cam-param2")

// Getting elements from event type 11
var itemDependentEl = document.getElementById("item-dependent-id");

// Getting elements from event type 12
var hideEventX = document.getElementById("hide-event-x");
var hideEventY = document.getElementById("hide-event-y");
var hideEventZ = document.getElementById("hide-event-z");

// Getting elements from event type 15
var grappleGunAreaX = document.getElementById("grapple-gun-x");
var grappleGunAreaY = document.getElementById("grapple-gun-y");
var grappleGunAreaZ = document.getElementById("grapple-gun-z");
var grappleGunDestinationX = document.getElementById("grapple-destination-x");
var grappleGunDestinationY = document.getElementById("grapple-destination-y");
var grappleGunDestinationZ = document.getElementById("grapple-destination-z");

// Const for getting Menu elements
const openFile = document.getElementById("openAEVfile");
const closeBtn = document.getElementById("closeAEVfile");
const saveBtn = document.getElementById("saveAEVfile");
const saveAsBtn = document.getElementById("saveAEVas");
const quitApp = document.getElementById("quitApp");
const minimizeBtn = document.getElementById("minimize");
const maximizeBtn = document.getElementById("maximize");
const closeWindowBtn = document.getElementById("closeWindow");

var toggleWhiteTheme = document.querySelector(".white-theme-btn");
var toggleDarkTheme = document.querySelector(".dark-theme-btn");

// Menu actions (open/save/quit)
openFile.addEventListener("click", () => {
    ipcRenderer.send("openAEVfile")
    ipcRenderer.send("closeAEVfile")
})

saveAsBtn.addEventListener("click", () => {
    ipcRenderer.send("saveAsAEVfile")
})

quitApp.addEventListener("click", () => {
    ipcRenderer.send("quitApp")
})

// Window menu actions
menuWindow.addEventListener("click", () => {
    ipcRenderer.send("openMainMenu")
})

minimizeBtn.addEventListener("click", () => {
    ipcRenderer.send("minimize")
})

maximizeBtn.addEventListener("click", () => {
    ipcRenderer.send("maximize")
})

closeWindowBtn.addEventListener("click", () => {
    ipcRenderer.send("closeWindow")
})

/* ==========================================
        NEW FUNCTIONALITY: CHANGE THEME  
   ========================================== */

// GETTING THEME SELECTED
function checkSelectedTheme() {
    if (localStorage.key)
        if (localStorage.getItem("THEME") == "whiteSelected") {
            if (!rootEl.classList.contains("white-mode")) {
                rootEl.classList.add("white-mode");
            } else {
                rootEl.classList.remove("white-mode");
            }
        }
}

rootEl.addEventListener("loadstart", checkSelectedTheme())

// THEME: WHITE
toggleWhiteTheme.addEventListener("click", () => {
    if (!rootEl.classList.contains("white-mode")) {
        rootEl.classList.add("white-mode");
        localStorage.setItem("THEME", "whiteSelected");
    } else {
        return;
    }
})
// THEME: DARK
toggleDarkTheme.addEventListener("click", () => {
    if (rootEl.classList.contains("white-mode")) {
        rootEl.classList.remove("white-mode");
        localStorage.setItem("THEME", "darkSelected");
    } else {
        return;
    }
})

/* ===============
    READ
   =============== */

// Global variables
var chunk = 0; // Will be incremented in each chunk

// Menu bar buttons 
copyBtnEl.addEventListener("click", () => document.execCommand("copy")); // Copy function
pasteBtnEl.addEventListener("click", () => document.execCommand("paste")); // Paste function
undoBtnEl.addEventListener("click", () => document.execCommand("undo")); // Undo function
redoBtnEl.addEventListener("click", () => document.execCommand("redo")); // Redo function

// Getting file path
ipcRenderer.on("aevFileChannel", (e, filepath) => {

    var fd = fs.openSync(filepath); // Opening the file in memory
    var buffer = fs.readFileSync(fd); // Converting file string to buffer (hex)
    var AEVFileName = String(filepath);
    var conv = AEVFileName.replace(/^[^.]*\\/gm, '');

    let total_event = buffer.readUInt8(6); // Reading number of objects i < eventIdSelect.length
    countEl.setAttribute("value", total_event);
    headerFileSize.value = buffer.length + " bytes";
    headerFileName.value = conv;

    // Event Listeners
    nextBtn.addEventListener("click", (e) => {
        if (eventNumber.value != total_event) {
            eventNumber.value = Number(eventNumber.value) + 1;
            showNextEvent(160);
        } else {
            return;
        }
    })

    prevBtn.addEventListener("click", (e) => {
        if (eventNumber.value > 1) {
            eventNumber.value = Number(eventNumber.value) - 1;
            showNextEvent(-160);
        } else {
            return;
        }
    })

    // Reading event type
    var getEventType = buffer.readUint8(69);
    for (i = 0; i < eventIdSelect.length; i++) {
        if (eventIdSelect.options[i].value == getEventType) {
            eventIdSelect.selectedIndex = i;
        }
    }

    // Showing the event based on its type
    for (i = 0; i < eventConfig.length; i++) {
        if (eventConfig[i].classList.contains(`type${getEventType}`)) {
            eventConfig[i].classList.remove("hide");
        }
    }

    // Reading index byte
    var getIndex = buffer.readUint8(70);
    indexEl.setAttribute("value", getIndex);

    // Reading unk bytes
    unk1El.value = buffer.readUint8(71);
    unk2El.value = buffer.readUint8(72);
    unk3El.value = buffer.readUint8(73);

    // Reading triggerzone coordinates
    topLeftXEl.value = buffer.readFloatLE(36).toFixed(2);
    topLeftZEl.value = buffer.readFloatLE(40).toFixed(2);
    topRightXEl.value = buffer.readFloatLE(44).toFixed(2);
    topRightZEl.value = buffer.readFloatLE(48).toFixed(2);
    bottomRightXEl.value = buffer.readFloatLE(52).toFixed(2);
    bottomRightZEl.value = buffer.readFloatLE(56).toFixed(2);
    bottomLeftXEl.value = buffer.readFloatLE(60).toFixed(2);
    bottomLeftZEl.value = buffer.readFloatLE(64).toFixed(2);

    // Reading lower and higher limits
    lowerLimitEl.value = buffer.readFloatLE(24).toFixed(0);
    higherLimitEl.value = buffer.readFloatLE(28).toFixed(0);

    // Reading definition bytes
    definitionByte1.value = buffer.readUint8(84);
    definitionByte2.value = buffer.readUint8(85);
    definitionByte3.value = buffer.readUint8(86);
    definitionByte4.value = buffer.readUint8(87);

    // Event type 1
    teleportXEl.value = buffer.readFloatLE(112).toFixed(2);
    teleportYEl.value = buffer.readFloatLE(116).toFixed(2);
    teleportZEl.value = buffer.readFloatLE(120).toFixed(2);
    teleportFacingAngleEl.value = buffer.readFloatLE(124).toFixed(2);

    var getRoomID1 = buffer.readUint8(128);
    var getRoomID2 = buffer.readUint8(129);
    for (i = 0; i < roomSelectEl.length; i++) {
        if (getRoomID2 < 10) {
            if (roomSelectEl.options[i].value.substring(0, 1) == getRoomID1 &&
                roomSelectEl.options[i].value.substring(1, 2) == getRoomID2) {
                roomSelectEl.selectedIndex = i;
                //Change image acording to room ID
                if (getEventType == 1) {
                    roomImage.src = `./images/rooms/${getRoomID1}${getRoomID2}.png`
                }
                break;
            }
        } else {
            if (roomSelectEl.options[i].value.substring(0, 1) == getRoomID1 &&
                roomSelectEl.options[i].value.substring(1, 3) == getRoomID2) {
                roomSelectEl.selectedIndex = i;
                //Change image acording to room ID
                if (getEventType == 1) {
                    roomImage.src = `./images/rooms/${getRoomID1}${getRoomID2}.png`
                }
                break;
            }
        }
    }

    // Event type 2
    offset88El.value = buffer.readUint8(88);
    offset89El.value = buffer.readUint8(89);
    offset90El.value = buffer.readUint8(90);

    // Event type 4
    enemyGroupEl.value = buffer.readUint8(114);

    // Event type 5
    messageEl.value = buffer.readUint8(114);
    messageCameraEl.value = buffer.readUint8(116);
    messageSoundEl.value = buffer.readUint8(118);

    // Event type A
    reactionTimeEl.value = buffer.readUint8(112);
    animationTypeEl.value = buffer.readUint8(116);
    damageOriginEl.value = buffer.readUint8(117);
    damageAmountEl.value = buffer.readUint8(120);

    // Event type 10
    ladderXEl.value = buffer.readFloatLE(112);
    ladderYEl.value = buffer.readFloatLE(116);
    ladderZEl.value = buffer.readFloatLE(120);
    ladderFacingAngleEl.value = buffer.readFloatLE(128);
    ladderStepsEl.value = buffer.readUint8(132)
    ladderUnknown1El.value = buffer.readUint8(133);
    ladderUnknown2El.value = buffer.readUint8(134);
    ladderCameraStartEl.value = buffer.readUint8(135);
    ladderCameraEndEl.value = buffer.readUint8(136);

    // Event type 11
    itemDependentEl.value = buffer.readUInt8(112);

    // Event type 12
    hideEventX.value = buffer.readFloatLE(112).toFixed(2);
    hideEventY.value = buffer.readFloatLE(116).toFixed(2);
    hideEventZ.value = buffer.readFloatLE(120).toFixed(2);

    // Event type 15
    grappleGunAreaX.value = buffer.readFloatLE(112).toFixed(2);
    grappleGunAreaY.value = buffer.readFloatLE(116).toFixed(2);
    grappleGunAreaZ.value = buffer.readFloatLE(120).toFixed(2);
    grappleGunDestinationX.value = buffer.readFloatLE(128).toFixed(2);
    grappleGunDestinationY.value = buffer.readFloatLE(132).toFixed(2);
    grappleGunDestinationZ.value = buffer.readFloatLE(136).toFixed(2);


    function showNextEvent(operator) {
        chunk = chunk + operator;

        // Reading event type
        let getCloneEventType = buffer.readUint8(69 + chunk);
        for (i = 0; i < eventIdSelect.length; i++) {
            if (eventIdSelect.options[i].value == getCloneEventType) {
                eventIdSelect.selectedIndex = i;
            }
        }

        // Showing event based on type
        for (i = 0; i < eventConfig.length; i++) {
            if (eventConfig[i].classList.contains(`type${getCloneEventType}`)) {
                eventConfig[i].classList.remove("hide");
            }
        }

        // Hiding event based on type
        for (i = 0; i < eventConfig.length; i++) {
            if (!eventConfig[i].classList.contains(`type${getCloneEventType}`)) {
                eventConfig[i].classList.add("hide");
            }
        }

        // Getting index 
        indexEl.value = buffer.readUint8(70 + chunk);

        // Getting unknown bytes
        unk1El.value = buffer.readUint8(71 + chunk);
        unk2El.value = buffer.readUint8(72 + chunk);
        unk3El.value = buffer.readUint8(73 + chunk);

        // Getting triggerzone coordinates
        topLeftXEl.value = buffer.readFloatLE(36 + chunk).toFixed(2);
        topLeftZEl.value = buffer.readFloatLE(40 + chunk).toFixed(2);
        topRightXEl.value = buffer.readFloatLE(44 + chunk).toFixed(2);
        topRightZEl.value = buffer.readFloatLE(48 + chunk).toFixed(2);
        bottomRightXEl.value = buffer.readFloatLE(52 + chunk).toFixed(2);
        bottomRightZEl.value = buffer.readFloatLE(56 + chunk).toFixed(2);
        bottomLeftXEl.value = buffer.readFloatLE(60 + chunk).toFixed(2);
        bottomLeftZEl.value = buffer.readFloatLE(64 + chunk).toFixed(2);

        // Getting lower and higher limits
        lowerLimitEl.value = buffer.readFloatLE(24 + chunk).toFixed(2);
        higherLimitEl.value = buffer.readFloatLE(28 + chunk).toFixed(2);

        // Reading definition bytes
        definitionByte1.value = buffer.readUint8(84 + chunk);
        definitionByte2.value = buffer.readUint8(85 + chunk);
        definitionByte3.value = buffer.readUint8(86 + chunk);
        definitionByte4.value = buffer.readUint8(87 + chunk);

        // Reading Event 1: Door Teleport 
        var getCloneTeleportX = buffer.readFloatLE(112 + chunk).toFixed(2);
        var getCloneTeleportY = buffer.readFloatLE(116 + chunk).toFixed(2);
        var getCloneTeleportZ = buffer.readFloatLE(120 + chunk).toFixed(2);
        var getCloneFacingAngle = buffer.readFloatLE(124 + chunk).toFixed(2);
        teleportXEl.value = getCloneTeleportX;
        teleportYEl.value = getCloneTeleportY;
        teleportZEl.value = getCloneTeleportZ;
        teleportFacingAngleEl.value = getCloneFacingAngle;

        var getCloneRoomID1 = buffer.readUint8(128 + chunk);
        var getCloneRoomID2 = buffer.readUint8(129 + chunk);
        for (i = 0; i < roomSelectEl.length; i++) {
            if (roomSelectEl.options[i].value.substring(0, 1) == getCloneRoomID1 &&
                roomSelectEl.options[i].value.substring(1, 2) == getCloneRoomID2 ||
                roomSelectEl.options[i].value.substring(0, 1) == getCloneRoomID1 &&
                roomSelectEl.options[i].value.substring(1, 3) == getCloneRoomID2) {
                roomSelectEl.selectedIndex = i;
            }
        }
        if (getCloneEventType == 1) {
            roomImage.src = `./images/rooms/${getCloneRoomID1}${getCloneRoomID2}.png`
        }

        // Reading Event 2: Cutscene 
        var getCloneOffset88 = buffer.readUint8(88 + chunk);
        var getCloneOffset89 = buffer.readUint8(89 + chunk);
        var getCloneOffset90 = buffer.readUint8(90 + chunk);
        offset88El.value = getCloneOffset88;
        offset89El.value = getCloneOffset89;
        offset90El.value = getCloneOffset90;

        // Reading Event 4: Enemy Group Trigger
        var getCloneEnemyGroupByte = buffer.readUint8(114 + chunk);
        enemyGroupEl.value = getCloneEnemyGroupByte;

        // Reading Event 5: Message
        let getCloneMessage = buffer.readUint8(114 + chunk);
        let getCloneMessageCamera = buffer.readUint8(116 + chunk);
        let getCloneMessageSound = buffer.readUint8(118 + chunk);
        messageEl.value = getCloneMessage;
        messageCameraEl.value = getCloneMessageCamera;
        messageSoundEl.value = getCloneMessageSound;

        // Reading Event A: Damage
        let getCloneReactionTime = buffer.readUint8(112 + chunk);
        let getCloneAnimationType = buffer.readUint8(116 + chunk);
        let getCloneDamageOrigin = buffer.readUint8(117 + chunk);
        let getCloneDamageAmount = buffer.readUint8(120 + chunk);
        reactionTimeEl.value = getCloneReactionTime;
        animationTypeEl.value = getCloneAnimationType;
        damageOriginEl.value = getCloneDamageOrigin;
        damageAmountEl.value = getCloneDamageAmount;

        // Reading Event 10: Ladder Climb
        ladderXEl.value = buffer.readFloatLE(112 + chunk).toFixed(2);
        ladderYEl.value = buffer.readFloatLE(116 + chunk).toFixed(2);
        ladderZEl.value = buffer.readFloatLE(120 + chunk).toFixed(2);
        ladderFacingAngleEl.value = buffer.readFloatLE(128 + chunk).toFixed(2);
        ladderStepsEl.value = buffer.readUint8(132 + chunk)
        ladderUnknown1El.value = buffer.readUint8(133 + chunk);
        ladderUnknown2El.value = buffer.readUint8(134 + chunk);
        ladderCameraStartEl.value = buffer.readUint8(135 + chunk);
        ladderCameraEndEl.value = buffer.readUint8(136 + chunk);

        // Reading Event 11: Item Dependencies
        itemDependentEl.value = buffer.readUInt8(112 + chunk);

        // Reading Event 12: HIDE Ashley
        hideEventX.value = buffer.readFloatLE(112 + chunk).toFixed(2);
        hideEventY.value = buffer.readFloatLE(116 + chunk).toFixed(2);
        hideEventZ.value = buffer.readFloatLE(120 + chunk).toFixed(2);

        // Reading Event 15: Grapple Gun
        grappleGunAreaX.value = buffer.readFloatLE(112 + chunk).toFixed(2);
        grappleGunAreaY.value = buffer.readFloatLE(116 + chunk).toFixed(2);
        grappleGunAreaZ.value = buffer.readFloatLE(120 + chunk).toFixed(2);
        grappleGunDestinationX.value = buffer.readFloatLE(128 + chunk).toFixed(2);
        grappleGunDestinationY.value = buffer.readFloatLE(132 + chunk).toFixed(2);
        grappleGunDestinationZ.value = buffer.readFloatLE(136 + chunk).toFixed(2);

    }

    /* ===============
        UPDATE
       =============== */

    // Getting any change from Main Config Container values
    containerMainConfig.addEventListener("change", function (e) {
        if (e.target.classList.contains("selectEventName")) {
            // Gets value from select/option and sets to buffer
            var select_number = parseInt(eventNumber.value); // Get clicked select ID attribute
            var select_opt = e.target.selectedIndex; // Get *option* selected from the clicked select
            var event_value = e.target.options[select_opt].value; // Get value from selected opt
            var chunk_save = 160 * (parseInt(select_number) - 1); // Multiply chunk size by select ID number

            // Showing event based on type
            for (i = 0; i < eventConfig.length; i++) {
                if (eventConfig[i].classList.contains(`type${event_value}`)) {
                    eventConfig[i].classList.remove("hide");

                }
            }
            // Hiding event based on type
            for (i = 0; i < eventConfig.length; i++) {
                if (!eventConfig[i].classList.contains(`type${event_value}`)) {
                    eventConfig[i].classList.add("hide");
                }
            }
            // Writes data on buffer on every hange
            console.log(chunk_save);
            buffer.writeUint8(event_value, 69 + parseInt(chunk_save));
        }
        if (e.target.className == "index") {
            // Gets value from index input and sets to buffer
            var setIndex = parseInt(e.target.value);
            var chunk_save = 160 * (eventNumber.value - 1);
            if (setIndex <= 255) {
                buffer.writeUint8(setIndex, 70 + chunk_save);
                indexEl.style.color = "#000";
                indexEl.style.outline = "none";
            } else {
                indexEl.style.color = "red";
                indexEl.style.outline = "1px solid red";
            }
        }
        if (e.target.classList.contains("top-leftX")) {
            // Gets value from Top Left X and sets to buffer
            var top_leftX = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            buffer.writeFloatLE(top_leftX, 36 + chunk_save).toFixed(2);
        }
        if (e.target.classList.contains("top-leftZ")) {
            // Gets value from Top Left Z and sets to buffer
            var top_leftZ = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            buffer.writeFloatLE(top_leftZ, 40 + chunk_save).toFixed(2);
        }
        if (e.target.classList.contains("top-rightX")) {
            // Gets value from Top Right X and sets to buffer
            var top_rightX = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            buffer.writeFloatLE(top_rightX, 44 + chunk_save).toFixed(2);
        }
        if (e.target.classList.contains("top-rightZ")) {
            // Gets value from Top Right Z and sets to buffer
            var top_rightZ = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            buffer.writeFloatLE(top_rightZ, 48 + chunk_save).toFixed(2);
        }
        if (e.target.classList.contains("bottom-rightX")) {
            // Gets value from Bottom Right X and sets to buffer
            var bottom_rightX = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            buffer.writeFloatLE(bottom_rightX, 52 + chunk_save).toFixed(2);
        }
        if (e.target.classList.contains("bottom-rightZ")) {
            // Gets value from Bottom Right Z and sets to buffer
            var bottom_rightZ = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            buffer.writeFloatLE(bottom_rightZ, 56 + chunk_save).toFixed(2);
        }
        if (e.target.classList.contains("bottom-leftX")) {
            // Gets value from Bottom Left X and sets to buffer
            var bottom_leftX = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            buffer.writeFloatLE(bottom_leftX, 60 + chunk_save).toFixed(2);
        }
        if (e.target.classList.contains("bottom-leftZ")) {
            // Gets value from Bottom Left Z and sets to buffer
            var bottom_leftZ = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            buffer.writeFloatLE(bottom_leftZ, 64 + chunk_save).toFixed(2);
        }
        if (e.target.classList.contains("lowerLimitEl")) {
            // Gets value from Lower Limit and sets to buffer
            var lower_limit = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            buffer.writeFloatLE(lower_limit, 24 + chunk_save).toFixed(2);
        }
        if (e.target.classList.contains("higherLimitEl")) {
            // Gets value from Higher Limit and sets to buffer
            var higher_limit = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            buffer.writeFloatLE(higher_limit, 28 + chunk_save).toFixed(2);
        }
        if (e.target.classList.contains("unk1")) {
            // Gets value from Unknown Bytes and sets to buffer
            var setUnk1 = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            if (setUnk1 <= 255) {
                buffer.writeUint8(setUnk1, 71 + chunk_save);
                unk1El.style.color = "#000";
                unk1El.style.outline = "none";
            } else {
                unk1El.style.color = "red";
                unk1El.style.outline = "1px solid red";
            }
        }
        if (e.target.classList.contains("unk2")) {
            // Gets value from Unknown Bytes and sets to buffer
            var setUnk2 = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            if (setUnk2 <= 255) {
                buffer.writeUint8(setUnk2, 72 + chunk_save);
                unk2El.style.color = "#000";
                unk2El.style.outline = "none";
            } else {
                unk2El.style.color = "red";
                unk2El.style.outline = "1px solid red";
            }
        }
        if (e.target.classList.contains("unk3")) {
            // Gets value from Unknown Bytes and sets to buffer
            var setUnk3 = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            if (setUnk3 <= 255) {
                buffer.writeUint8(setUnk3, 73 + chunk_save);
                unk3El.style.color = "#000";
                unk3El.style.outline = "none";
            } else {
                unk3El.style.color = "red";
                unk3El.style.outline = "1px solid red";
            }
        }
    })

    containerEventConfig.addEventListener("change", function (e) {
        if (e.target.classList.contains("def1")) {
            // Gets value from Definition Bytes and sets to buffer
            var setDef1 = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            if (setDef1 <= 255) {
                buffer.writeUint8(setDef1, 84 + chunk_save);
                definitionByte1.style.color = "#000";
                definitionByte1.style.outline = "none";
            } else {
                definitionByte1.style.color = "red";
                definitionByte1.style.outline = "1px solid red";
            }
        }
        if (e.target.classList.contains("def2")) {
            // Gets value from Definition Bytes and sets to buffer
            var setDef2 = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            if (setDef2 <= 255) {
                buffer.writeUint8(setDef2, 85 + chunk_save);
                definitionByte2.style.color = "#000";
                definitionByte2.style.outline = "none";
            } else {
                definitionByte2.style.color = "red";
                definitionByte2.style.outline = "1px solid red";
            }
        }
        if (e.target.classList.contains("def3")) {
            // Gets value from Definition Bytes and sets to buffer
            var setDef3 = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            if (setDef3 <= 255) {
                buffer.writeUint8(setDef3, 86 + chunk_save);
                definitionByte3.style.color = "#000";
                definitionByte3.style.outline = "none";
            } else {
                definitionByte3.style.color = "red";
                definitionByte3.style.outline = "1px solid red";
            }
        }
        if (e.target.classList.contains("def4")) {
            // Gets value from Definition Bytes and sets to buffer
            var setDef4 = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            if (setDef4 <= 255) {
                buffer.writeUint8(setDef4, 87 + chunk_save);
                definitionByte4.style.color = "#000";
                definitionByte4.style.outline = "none";
            } else {
                definitionByte4.style.color = "red";
                definitionByte4.style.outline = "1px solid red";
            }
        }

        /* ===== Event: Door Teleport ====== */
        if (e.target.classList.contains("room-select")) {
            // Gets value from Room Selector and sets to buffer
            let selectRoom_opt = e.target.selectedIndex;
            let setRoomCompleteID = e.target.options[selectRoom_opt].value;
            let stageID = setRoomCompleteID.substring(0, 1);

            function getRoomID() {
                if (setRoomCompleteID.length == 1) {
                    return setRoomCompleteID.substring(1, 2);
                } else {
                    return setRoomCompleteID.substring(1, 3);
                }
            }

            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            buffer.writeUint8(stageID, 128 + chunk_save);
            buffer.writeUint8(getRoomID(), 129 + chunk_save);
            // Changing image acording to room ID
            roomImage.src = `./images/rooms/${stageID}${getRoomID()}.png`
        }
        if (e.target.classList.contains("teleport-x")) {
            // Gets value from Teleport X and sets to buffer
            var setTeleportX = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            buffer.writeFloatLE(setTeleportX, 112 + chunk_save).toFixed(2);
        }
        if (e.target.classList.contains("teleport-y")) {
            // Gets value from Teleport Y and sets to buffer
            var setTeleportY = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            buffer.writeFloatLE(setTeleportY, 116 + chunk_save).toFixed(2);
        }
        if (e.target.classList.contains("teleport-z")) {
            // Gets value from Teleport Z and sets to buffer
            var setTeleportZ = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            buffer.writeFloatLE(setTeleportZ, 120 + chunk_save).toFixed(2);
        }
        if (e.target.classList.contains("teleport-rotation")) {
            // Gets value from Teleport Rotation and sets to buffer
            var setTeleportZ = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            buffer.writeFloatLE(setTeleportZ, 124 + chunk_save).toFixed(2);
        }

        /* ===== Event: Cutscene ====== */
        if (e.target.classList.contains("offset88")) {
            // Gets value from Offset 88 and sets to buffer
            var setOffset88 = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            if (setOffset88 <= 255) {
                buffer.writeUint8(setOffset88, 88 + chunk_save);
                offset88El.style.color = "#000";
                offset88El.style.outline = "none";
            } else {
                offset88El.style.color = "red";
                offset88El.style.outline = "1px solid red";
            }
        }
        if (e.target.classList.contains("offset89")) {
            // Gets value from Offset 89 and sets to buffer
            var setOffset89 = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            if (setOffset89 <= 255) {
                buffer.writeUint8(setOffset89, 89 + chunk_save);
                offset89El.style.color = "#000";
                offset89El.style.outline = "none";
            } else {
                offset89El.style.color = "red";
                offset89El.style.outline = "1px solid red";
            }
        }
        if (e.target.classList.contains("offset90")) {
            // Gets value from Offset 90 and sets to buffer
            var setOffset90 = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            if (setOffset90 <= 255) {
                buffer.writeUint8(setOffset90, 90 + chunk_save);
                offset90El.style.color = "#000";
                offset90El.style.outline = "none";
            } else {
                offset90El.style.color = "red";
                offset90El.style.outline = "1px solid red";
            }
        }

        /* ===== Event: Enemy Group Trigger ====== */
        if (e.target.classList.contains("enemyGroupByte")) {
            // Gets value from Enemy Group Byte and sets to buffer
            var setEnemyGroup = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            if (setEnemyGroup <= 255) {
                buffer.writeUint8(setEnemyGroup, 114 + chunk_save);
                enemyGroupEl.style.color = "#000";
                enemyGroupEl.style.outline = "none";
            } else {
                enemyGroupEl.style.color = "red";
                enemyGroupEl.style.outline = "1px solid red";
            }
        }

        /* ===== Event: Message ====== */
        if (e.target.classList.contains("message")) {
            // Gets value from Message Byte and sets to buffer
            var setMessage = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            if (setMessage <= 255) {
                buffer.writeUint8(setMessage, 114 + chunk_save);
                messageEl.style.color = "#000";
                messageEl.style.outline = "none";
            } else {
                messageEl.style.color = "red";
                messageEl.style.outline = "1px solid red";
            }
        }
        if (e.target.classList.contains("camera-offset")) {
            // Gets value from Message Camera Byte and sets to buffer
            var setMessageCamera = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            if (setMessageCamera <= 255) {
                buffer.writeUint8(setMessageCamera, 116 + chunk_save);
                messageCameraEl.style.color = "#000";
                messageCameraEl.style.outline = "none";
            } else {
                messageCameraEl.style.color = "red";
                messageCameraEl.style.outline = "1px solid red";
            }
        }
        if (e.target.classList.contains("sound-offset")) {
            // Gets value from Message Sound Byte and sets to buffer
            var setMessageSound = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            if (setMessageSound <= 255) {
                buffer.writeUint8(setMessageSound, 118 + chunk_save);
                messageSoundEl.style.color = "#000";
                messageSoundEl.style.outline = "none";
            } else {
                messageSoundEl.style.color = "red";
                messageSoundEl.style.outline = "1px solid red";
            }
        }

        /* ===== Event: Damage ====== */
        if (e.target.id == "reaction-time") {
            // Gets value from Reaction Time and sets to buffer
            var setReactionTime = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            if (setReactionTime <= 255) {
                buffer.writeUint8(setReactionTime, 112 + chunk_save);
                reactionTimeEl.style.color = "#000";
                reactionTimeEl.style.outline = "none";
            } else {
                reactionTimeEl.style.color = "red";
                reactionTimeEl.style.outline = "1px solid red";
            }
        }
        if (e.target.id == "animation-type") {
            // Gets value from Animation Type and sets to buffer
            var setAnimationType = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            if (setAnimationType <= 255) {
                buffer.writeUint8(setAnimationType, 116 + chunk_save);
                animationTypeEl.style.color = "#000";
                animationTypeEl.style.outline = "none";
            } else {
                animationTypeEl.style.color = "red";
                animationTypeEl.style.outline = "1px solid red";
            }
        }
        if (e.target.id == "damage-origin") {
            // Gets value from Damage Origin and sets to buffer
            var setDamageOrigin = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            if (setDamageOrigin <= 255) {
                buffer.writeUint8(setDamageOrigin, 117 + chunk_save);
                damageOriginEl.style.color = "#000";
                damageOriginEl.style.outline = "none";
            } else {
                damageOriginEl.style.color = "red";
                damageOriginEl.style.outline = "1px solid red";
            }
        }
        if (e.target.id == "damage-amount") {
            // Gets value from Damage Amount and sets to buffer
            var setDamageAmount = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            if (setDamageAmount <= 255) {
                buffer.writeUint8(setDamageAmount, 120 + chunk_save);
                damageAmountEl.style.color = "#000";
                damageAmountEl.style.outline = "none";
            } else {
                damageAmountEl.style.color = "red";
                damageAmountEl.style.outline = "1px solid red";
            }
        }

        /* ===== Event: Ladder Climb ====== */
        if (e.target.id == "ladder-x") {
            // Gets value from Ladder X and sets to buffer
            var setLadderX = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            buffer.writeFloatLE(setLadderX, 112 + chunk_save).toFixed(2);
        }
        if (e.target.id == "ladder-y") {
            // Gets value from Ladder Y and sets to buffer
            var setLadderY = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            buffer.writeFloatLE(setLadderY, 116 + chunk_save).toFixed(2);
        }
        if (e.target.id == "ladder-z") {
            // Gets value from Ladder Z and sets to buffer
            var setLadderZ = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            buffer.writeFloatLE(setLadderZ, 120 + chunk_save).toFixed(2);
        }
        if (e.target.id == "ladder-rotation") {
            // Gets value from Facing Angle and sets to buffer
            var setLadderFacingAngle = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            buffer.writeFloatLE(setLadderFacingAngle, 128 + chunk_save).toFixed(2);
        }
        if (e.target.id == "ladder-steps") {
            // Gets value from Ladder Steps and sets to buffer
            var setLadderSteps = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            if (setLadderSteps <= 255) {
                buffer.writeUint8(setLadderSteps, 132 + chunk_save);
                ladderStepsEl.style.color = "#000";
                ladderStepsEl.style.outline = "none";
            } else {
                ladderStepsEl.style.color = "red";
                ladderStepsEl.style.outline = "1px solid red";
            }
        }
        if (e.target.id == "cam-param1") {
            // Gets value from Ladder Unknown 1 and sets to buffer
            var setLadderUnknown1 = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            if (setLadderUnknown1 <= 255) {
                buffer.writeUint8(setLadderUnknown1, 133 + chunk_save);
                ladderUnknown1El.style.color = "#000";
                ladderUnknown1El.style.outline = "none";
            } else {
                ladderUnknown1El.style.color = "red";
                ladderUnknown1El.style.outline = "1px solid red";
            }
        }
        if (e.target.id == "cam-param2") {
            // Gets value from Ladder Unknown 2 and sets to buffer
            var setLadderUnknown2 = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            if (setLadderUnknown2 <= 255) {
                buffer.writeUint8(setLadderUnknown2, 134 + chunk_save);
                ladderUnknown2El.style.color = "#000";
                ladderUnknown2El.style.outline = "none";
            } else {
                ladderUnknown2El.style.color = "red";
                ladderUnknown2El.style.outline = "1px solid red";
            }
        }
        if (e.target.id == "cam-param3") {
            // Gets value from Ladder Camera Start and sets to buffer
            var setLadderCameraStart = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            if (setLadderCameraStart <= 255) {
                buffer.writeUint8(setLadderCameraStart, 135 + chunk_save);
                ladderCameraStartEl.style.color = "#000";
                ladderCameraStartEl.style.outline = "none";
            } else {
                ladderCameraStartEl.style.color = "red";
                ladderCameraStartEl.style.outline = "1px solid red";
            }
        }
        if (e.target.id == "cam-param4") {
            // Gets value from Ladder Camera End and sets to buffer
            var setLadderCameraEnd = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            if (setLadderCameraEnd <= 255) {
                buffer.writeUint8(setLadderCameraEnd, 136 + chunk_save);
                ladderCameraEndEl.style.color = "#000";
                ladderCameraEndEl.style.outline = "none";
            } else {
                ladderCameraEndEl.style.color = "red";
                ladderCameraEndEl.style.outline = "1px solid red";
            }
        }

        /* ===== Event: Item Dependencies ====== */
        if (e.target.id == "item-dependent-id") {
            // Gets value from Use Key and sets to buffer
            var setItemDependent = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            if (setItemDependent <= 255) {
                buffer.writeUint8(setItemDependent, 112 + chunk_save);
                itemDependentEl.style.color = "#000";
                itemDependentEl.style.outline = "none";
            } else {
                itemDependentEl.style.color = "red";
                itemDependentEl.style.outline = "1px solid red";
            }
        }

        /* ===== Event: HIDE Event ====== */
        if (e.target.id == "hide-event-x") {
            // Gets value from Hide Event X and sets to buffer
            var setHideEventX = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            buffer.writeFloatLE(setHideEventX, 112 + chunk_save).toFixed(2);
        }
        if (e.target.id == "hide-event-y") {
            // Gets value from Hide Event Y and sets to buffer
            var setHideEventY = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            buffer.writeFloatLE(setHideEventY, 116 + chunk_save).toFixed(2);
        }
        if (e.target.id == "hide-event-z") {
            // Gets value from Hide Event Z and sets to buffer
            var setHideEventZ = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            buffer.writeFloatLE(setHideEventZ, 120 + chunk_save).toFixed(2);
        }

        /* ===== Event: Grapple Gun ====== */
        if (e.target.id == "grapple-gun-x") {
            // Gets value from Grapple Gun Shot X and sets to buffer
            var setGrappleAreaX = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            buffer.writeFloatLE(setGrappleAreaX, 112 + chunk_save).toFixed(2);
        }
        if (e.target.id == "grapple-gun-y") {
            // Gets value from Grapple Gun Shot Y and sets to buffer
            var setGrappleAreaY = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            buffer.writeFloatLE(setGrappleAreaY, 116 + chunk_save).toFixed(2);
        }
        if (e.target.id == "grapple-gun-z") {
            // Gets value from Grapple Gun Shot Z and sets to buffer
            var setGrappleAreaZ = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            buffer.writeFloatLE(setGrappleAreaZ, 120 + chunk_save).toFixed(2);
        }
        if (e.target.id == "grapple-destination-x") {
            // Gets value from Grapple Gun Destination X and sets to buffer
            var setGrappleDestinationX = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            buffer.writeFloatLE(setGrappleDestinationX, 128 + chunk_save).toFixed(2);
        }
        if (e.target.id == "grapple-destination-y") {
            // Gets value from Grapple Gun Destination Y and sets to buffer
            var setGrappleDestinationY = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            buffer.writeFloatLE(setGrappleDestinationY, 132 + chunk_save).toFixed(2);
        }
        if (e.target.id == "grapple-destination-z") {
            // Gets value from Grapple Gun Destination Z and sets to buffer
            var setGrappleDestinationZ = e.target.value;
            var chunk_save = 160 * (parseInt(eventNumber.value) - 1);
            buffer.writeFloatLE(setGrappleDestinationZ, 136 + chunk_save).toFixed(2);
        }
    })

    /* ==========================================
        NEW FUNCIONALITY: ADD NEW EVENT
       ========================================== */
    var BUFFER_newEvent;  // Buffer used for getting a new Chunk of 160 bytes
    var BUFFER_final;     // Buffer used to merge initial buffer + new chunk
    var BUFFER_temporary; // Buffer used for removing CDCDCDCD bytes from the end of the file
    var BUFFER_topPart;   // Buffer used to split the file and get the top part
    var BUFFER_downPart;  // Buffer used to split the file and get the bottom part

    // Functions for Text box
    function showTextBox(message, status) {
        var removedMessage = document.querySelector(".removedEntry");
        removedMessage.style.display = "block"
        removedMessage.style.backgroundColor = status;
        if (status == "exported") {
            document.getElementById("removedEntry").firstElementChild.style.backgroundColor = "#333"
            document.getElementById("removedEntry").firstElementChild.style.border = "2px solid #ddd"
            document.getElementById("removedEntry").firstElementChild.style.width = "35%"
            document.getElementById("removedEntry").firstElementChild.innerHTML = '<i class="fa-solid fa-file-export"></i>&nbsp&nbsp' + message;
        }
        if (status == "imported") {
            document.getElementById("removedEntry").firstElementChild.style.backgroundColor = "#353"
            document.getElementById("removedEntry").firstElementChild.style.border = "2px solid #7f7"
            document.getElementById("removedEntry").firstElementChild.style.color = "2px solid #7f7"
            document.getElementById("removedEntry").firstElementChild.style.color = "2px solid #7f7"
            document.getElementById("removedEntry").firstElementChild.style.width = "35%"
            document.getElementById("removedEntry").firstElementChild.innerHTML = '<i class="fa-solid fa-file-import"></i>&nbsp&nbsp' + message;
        }
        setTimeout(() => {
            removedMessage.style.display = "none"
        }, 2000);
    }

    addNewEventEl.addEventListener("click", function addNewEvent() {
        // Check if there's CDCDCDCD bytes at the end of the file
        if (buffer.at(buffer.length - 16) == 205) {
            BUFFER_temporary = buffer.slice(0, buffer.length - 16); // Removes CDCDCDCD bytes
            buffer = BUFFER_temporary;
        }
        BUFFER_newEvent = buffer.subarray(buffer.length - 160);  // Copies last event

        BUFFER_final = Buffer.concat([buffer, BUFFER_newEvent]); // Merge copied event to main buffer
        let iterator = 0;
        for (let j = 0; j < 16; j++) { // Zeroes every byte in the event config
            BUFFER_final.writeUint32LE(0, (BUFFER_final.length - 160) + 96 + iterator);
            iterator += 4;
        }
        buffer = BUFFER_final;
        countEl.value++; // Updates the total events (display only)
        total_event++; // Updates the total events (including event changer arrows)

        let getLastIndexValue = buffer.readUint8(70 + (160 * (total_event - 1)));
        getLastIndexValue++;
        buffer.writeUint8(total_event, 6);
        buffer.writeUint8(getLastIndexValue, 70 + (160 * (total_event - 1)));
        headerFileSize.value = buffer.length + " bytes";
        // Creates a text box for 1 sec if successful
        let addedMessage = document.querySelector(".addedEntry");
        addedMessage.style.display = "block"
        setTimeout(() => {
            addedMessage.style.display = "none"
        }, 1000);
        return buffer;
    });

    removeEventEl.addEventListener("click", function removeEvent() {
        if (buffer.length > 176) {
            BUFFER_topPart = buffer.subarray(0, (160 * (eventNumber.value - 1)) + 16);
            BUFFER_downPart = buffer.subarray(BUFFER_topPart.length + 160);
            BUFFER_final = Buffer.concat([BUFFER_topPart, BUFFER_downPart]);
            buffer = BUFFER_final;
            total_event--;

            buffer.writeUint8(total_event, 6);
            headerFileSize.value = buffer.length + " bytes";
            if (eventNumber.value == 1) {
                showNextEvent(0);
            } else {
                showNextEvent(-160);
                eventNumber.value = eventNumber.value = Number(eventNumber.value - 1);
            }
            if (countEl.value > 1) {
                countEl.value--;
            }
            // Creates a text box for 1 sec if successful
            var removedMessage = document.querySelector(".removedEntry");
            removedMessage.style.display = "block"
            setTimeout(() => {
                removedMessage.style.display = "none"
            }, 1000);

            return buffer;
        }
        return showTextBox("You cannot remove any more events!")
    });

    /* =================================
        EXPORT/IMPORT FUNCTIONALITY
       ================================= */

    // Getting elements
    const btnExportEl = document.getElementById("export-event");
    const btnExportAllEl = document.getElementById("export-all-event");
    const btnImportEl = document.getElementById("import-event");
    const btnImportAllEl = document.getElementById("import-all-event");

    // Function for generating faces
    function generateFaces(quantity) {
        let faces_complete = "";
        sum = 0;
        for (i = 0; i != buffer.readUint8(6); i++) {
            if (quantity == 1) { i = eventNumber.value - 1; }
            let faces = `o  event_${Number(i + 1)}_type_${buffer.readUint8(69 + (160 * i))}` +
                `\n` +
                "f " + (1 + sum) + " " + (2 + sum) + " " + (3 + sum) + `\n` +
                "f " + (1 + sum) + " " + (3 + sum) + " " + (4 + sum) + `\n` +
                "f " + (1 + sum) + " " + (5 + sum) + " " + (6 + sum) + `\n` +
                "f " + (1 + sum) + " " + (6 + sum) + " " + (2 + sum) + `\n` +
                "f " + (2 + sum) + " " + (6 + sum) + " " + (7 + sum) + `\n` +
                "f " + (2 + sum) + " " + (7 + sum) + " " + (3 + sum) + `\n` +
                "f " + (3 + sum) + " " + (7 + sum) + " " + (8 + sum) + `\n` +
                "f " + (3 + sum) + " " + (8 + sum) + " " + (4 + sum) + `\n` +
                "f " + (4 + sum) + " " + (8 + sum) + " " + (5 + sum) + `\n` +
                "f " + (4 + sum) + " " + (5 + sum) + " " + (1 + sum) + `\n` +
                "f " + (5 + sum) + " " + (7 + sum) + " " + (6 + sum) + `\n` +
                "f " + (5 + sum) + " " + (8 + sum) + " " + (7 + sum) + `\n`
            faces_complete = faces_complete + faces;
            if (quantity == 1) { break } // If only one obj it's extracted, this breaks the loop
            sum = sum + 8;
        } return faces_complete;
    }

    // Function for getting coordinate values
    function exportEvents_vertices(quantity) {
        let objMaker = "";
        let objComplete = `# Resident evil 4 - AEV Tool 2022` +
            `\n` + `\n` + `# By HardRain` + `\n` + `\n`;
        let vert_topLeftX = topLeftXEl.value / 1000;
        let vert_topLeftZ = topLeftZEl.value / 1000;
        let vert_topRightX = topRightXEl.value / 1000;
        let vert_topRightZ = topRightZEl.value / 1000;
        let vert_bottomRightX = bottomRightXEl.value / 1000;
        let vert_bottomRightZ = bottomRightZEl.value / 1000;
        let vert_bottomLeftX = bottomLeftXEl.value / 1000;
        let vert_bottomLeftZ = bottomLeftZEl.value / 1000;
        let vert_lowerLimit = lowerLimitEl.value / 1000;
        let vert_higherLimit = higherLimitEl.value / 1000;

        for (i = 0; i != buffer.readUint8(6); i++) {
            if (quantity == 1) { i = (eventNumber.value - 1) }
            vert_topLeftX = buffer.readFloatLE(36 + (160 * i)) / 1000;
            vert_topLeftZ = buffer.readFloatLE(40 + (160 * i)) / 1000;
            vert_topRightX = buffer.readFloatLE(44 + (160 * i)) / 1000;
            vert_topRightZ = buffer.readFloatLE(48 + (160 * i)) / 1000;
            vert_bottomRightX = buffer.readFloatLE(52 + (160 * i)) / 1000;
            vert_bottomRightZ = buffer.readFloatLE(56 + (160 * i)) / 1000;
            vert_bottomLeftX = buffer.readFloatLE(60 + (160 * i)) / 1000;
            vert_bottomLeftZ = buffer.readFloatLE(64 + (160 * i)) / 1000;
            vert_lowerLimit = buffer.readFloatLE(24 + (160 * i)) / 1000;
            vert_higherLimit = buffer.readFloatLE(28 + (160 * i)) / 1000;

            objMaker = "# Vertices" + ` Event ${Number(i + 1)}` +
                `\n` +
                "v " + String(vert_topLeftX.toFixed(5)) + " "
                + String(vert_lowerLimit.toFixed(5)) + " "
                + String(vert_topLeftZ.toFixed(5)) +
                `\n` +
                "v " + String(vert_topRightX.toFixed(5)) + " "
                + String(vert_lowerLimit.toFixed(5)) + " "
                + String(vert_topRightZ.toFixed(5)) +
                `\n` +
                "v " + String(vert_bottomRightX.toFixed(5)) + " "
                + String(vert_lowerLimit.toFixed(5)) + " "
                + String(vert_bottomRightZ.toFixed(5)) +
                `\n` +
                "v " + String(vert_bottomLeftX.toFixed(5)) + " "
                + String(vert_lowerLimit.toFixed(5)) + " "
                + String(vert_bottomLeftZ.toFixed(5)) +
                `\n` + // --------- BELOW HERE STARTS WITH HIGHER LIMIT -------------
                "v " + String(vert_topLeftX.toFixed(5)) + " "
                + String(vert_higherLimit.toFixed(5)) + " "
                + String(vert_topLeftZ.toFixed(5)) +
                `\n` +
                "v " + String(vert_topRightX.toFixed(5)) + " "
                + String(vert_higherLimit.toFixed(5)) + " "
                + String(vert_topRightZ.toFixed(5)) +
                `\n` +
                "v " + String(vert_bottomRightX.toFixed(5)) + " "
                + String(vert_higherLimit.toFixed(5)) + " "
                + String(vert_bottomRightZ.toFixed(5)) +
                `\n` +
                "v " + String(vert_bottomLeftX.toFixed(5)) + " "
                + String(vert_higherLimit.toFixed(5)) + " "
                + String(vert_bottomLeftZ.toFixed(5)) + `\n`
            objComplete = objComplete + objMaker;
            if (quantity == 1) break;
        }
        return objComplete
    }

    function importEvents_vertices(quantity) {
        ipcRenderer.on("AEVobj", (e, objpath) => {
            var importedObjContent = fs.readFileSync(objpath, { encoding: 'utf8' });
            let sum = 0;
            let regexFromX = /(v\s[-]?\d{1,}[.]\d{1,})/g;
            let regexFromHeight = /(?!v\s[-]?\d{1,}[.]\d{1,})(\d\s[-]?\d{1,}[.]\d{5,})/g;
            let regexFromZ = /(?!v\s[-]?\d{1,}[.]\d{1,})(?!\d\s\d{1,}[.]\d{1,})([-]?\d{1,}[.]\d{5,}[\r\n])/g;

            let arrayFromX = importedObjContent.match(regexFromX);
            let arrayFromHeight = importedObjContent.match(regexFromHeight);
            let arrayFromZ = importedObjContent.match(regexFromZ);

            for (let i = 0; i != buffer.readUint8(6); i++) {
                if (quantity == 1) { i = (eventNumber.value - 1) }
                // X values
                buffer.writeFloatLE(Number(arrayFromX[0 + sum].substring(2)) * 1000, 36 + (160 * i));
                buffer.writeFloatLE(Number(arrayFromX[1 + sum].substring(2)) * 1000, 44 + (160 * i));
                buffer.writeFloatLE(Number(arrayFromX[2 + sum].substring(2)) * 1000, 52 + (160 * i));
                buffer.writeFloatLE(Number(arrayFromX[3 + sum].substring(2)) * 1000, 60 + (160 * i));
                // Z values
                buffer.writeFloatLE(Number(arrayFromZ[0 + sum].substring(0, arrayFromZ[0].length - 2)) * 1000, 40 + (160 * i));
                buffer.writeFloatLE(Number(arrayFromZ[1 + sum].substring(0, arrayFromZ[1].length - 2)) * 1000, 48 + (160 * i));
                buffer.writeFloatLE(Number(arrayFromZ[2 + sum].substring(0, arrayFromZ[2].length - 2)) * 1000, 56 + (160 * i));
                buffer.writeFloatLE(Number(arrayFromZ[3 + sum].substring(0, arrayFromZ[3].length - 2)) * 1000, 64 + (160 * i));
                // Lower and Higher Limits
                buffer.writeFloatLE(Number(arrayFromHeight[0 + sum].substring(2)) * 1000, 24 + (160 * i));
                buffer.writeFloatLE(Number(arrayFromHeight[4 + sum].substring(2)) * 1000, 28 + (160 * i));
                // Outputing values to screen
                lowerLimitEl.value = buffer.readFloatLE(24 + (160 * (eventNumber.value - 1))).toFixed(2);
                higherLimitEl.value = buffer.readFloatLE(28 + (160 * (eventNumber.value - 1))).toFixed(2);
                topLeftXEl.value = buffer.readFloatLE(36 + (160 * (eventNumber.value - 1))).toFixed(2);
                topRightXEl.value = buffer.readFloatLE(44 + (160 * (eventNumber.value - 1))).toFixed(2);
                bottomLeftXEl.value = buffer.readFloatLE(52 + (160 * (eventNumber.value - 1))).toFixed(2);
                bottomRightXEl.value = buffer.readFloatLE(60 + (160 * (eventNumber.value - 1))).toFixed(2);
                topLeftZEl.value = buffer.readFloatLE(40 + (160 * (eventNumber.value - 1))).toFixed(2);
                topRightZEl.value = buffer.readFloatLE(48 + (160 * (eventNumber.value - 1))).toFixed(2);
                bottomLeftZEl.value = buffer.readFloatLE(56 + (160 * (eventNumber.value - 1))).toFixed(2);
                bottomRightZEl.value = buffer.readFloatLE(64 + (160 * (eventNumber.value - 1))).toFixed(2);
                if (quantity == 1) { break }
                sum = sum + 8;
            } if (quantity == 1) {
                showTextBox("Event imported successfully!", "imported");
            } else
                showTextBox("All events imported successfully!", "imported");

        })
    }

    // Event listener
    var folderName = headerFileName.value.substring(0, headerFileName.value.length - 4);
    btnExportEl.addEventListener("click", function exportOneEvent() {
        showTextBox("Event exported to obj!", "exported");
        fs.mkdirSync(`Events/${folderName}`, { recursive: true });
        fs.writeFileSync(`Events/${folderName}/event_${eventNumber.value}_type_${buffer.readUint8(69 + (160 * (eventNumber.value - 1)))}.obj`, exportEvents_vertices(1) + generateFaces(1));
    });
    btnExportAllEl.addEventListener("click", function exportAllEvents() {
        showTextBox("All events exported to obj!", "exported");
        fs.mkdirSync(`Events/${folderName}`, { recursive: true });
        fs.writeFileSync(`Events/${folderName}/event_all.obj`, exportEvents_vertices(0) + generateFaces(0));
        console.log(__dirname);
    });
    btnImportEl.addEventListener("click", function importOneEvent() {
        ipcRenderer.send("AEVimportBtn");
        importEvents_vertices(1)
    })
    btnImportAllEl.addEventListener("click", function importAllEvents() {
        ipcRenderer.send("AEVimportBtn");
        importEvents_vertices(0);
    })

    // Save all modified buffer back to file
    saveBtn.addEventListener("click", () => {
        fs.writeFileSync(filepath, buffer);
        var saveMessage = document.querySelector(".hide");
        saveMessage.style.display = "block"
        setTimeout(() => {
            saveMessage.style.display = "none"
        }, 2000);
    })

    closeBtn.addEventListener("click", () => {
        ipcRenderer.send("closeAEVfile");
    })

    ipcRenderer.on("saveAsAEVfileContent", (e, arg) => {
        fs.writeFileSync(arg, buffer);
    })
})
