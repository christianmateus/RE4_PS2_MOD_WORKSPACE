const fs = require('fs');
const { ipcRenderer } = require('electron');
const textBox = require("../shared/textBox").module;

/* ===============
    CREATE
   =============== */

// Getting elements
var rootEl = document.querySelector("html");
var addNewBtnEl = document.getElementsByClassName("addNewObjectBtn");
var copyBtnEl = document.getElementById("copyBtn");
var pasteBtnEl = document.getElementById("pasteBtn");
var undoBtnEl = document.getElementById("undoBtn");
var redoBtnEl = document.getElementById("redoBtn");
var countEl = document.getElementById("count");
var numberSequential = document.querySelector(".number-sequential");
var imageEl = document.querySelector(".imageEl");
var selectList = document.querySelector(".objectList");
var indexEl = document.querySelector(".index");
var posX = document.querySelector(".posX");
var posY = document.querySelector(".posY");
var posZ = document.querySelector(".posZ");
var rotX = document.querySelector(".rotX");
var rotY = document.querySelector(".rotY");
var rotZ = document.querySelector(".rotZ");

// Getting elements for cloneRow function
var table = document.querySelector("table");
var tBody = document.querySelector("tbody");
var row = document.querySelector(".tableRow");

// Const for getting Menu elements
const openFile = document.getElementById("openETSfile");
const closeBtn = document.getElementById("closeETSfile");
const saveBtn = document.getElementById("saveETSfile");
const saveAsBtn = document.getElementById("saveETSas");
const quitApp = document.getElementById("quitApp");
const minimizeBtn = document.getElementById("minimize");
const maximizeBtn = document.getElementById("maximize");
const closeWindowBtn = document.getElementById("closeWindow");

var toggleWhiteTheme = document.querySelector(".white-theme-btn");
var toggleDarkTheme = document.querySelector(".dark-theme-btn");

// Menu actions (open/save/quit)
openFile.addEventListener("click", () => {
    ipcRenderer.send("openETSfile")
    ipcRenderer.send("closeETSfile")
})

saveAsBtn.addEventListener("click", () => {
    ipcRenderer.send("saveAsETSfile")
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

/* ===============
    READ
   =============== */

// Global variables
var chunk = 0; // Will be incremented in each chunk
var seq = 1; // Slot for item number
var select_id = 2; // Add a new id for each select (used in cloneRow())
var index_id = 2; // Add a new id for each index (used in cloneRow())
var posX_id = 2; // Add a new id for each posX (used in cloneRow())
var posY_id = 2; // Add a new id for each posX (used in cloneRow())
var posZ_id = 2; // Add a new id for each posX (used in cloneRow())
var rotX_id = 2; // Add a new id for each posX (used in cloneRow())
var rotY_id = 2; // Add a new id for each posX (used in cloneRow())
var rotZ_id = 2; // Add a new id for each posX (used in cloneRow())

// Menu bar buttons 
copyBtnEl.addEventListener("click", () => document.execCommand("copy")); // Copy function
pasteBtnEl.addEventListener("click", () => document.execCommand("paste")); // Paste function
undoBtnEl.addEventListener("click", () => document.execCommand("undo")); // Undo function
redoBtnEl.addEventListener("click", () => document.execCommand("redo")); // Redo function

// Getting file path
ipcRenderer.on("etsFileChannel", (e, filepath) => {

    var fd = fs.openSync(filepath); // Opening the file in memory
    var buffer = fs.readFileSync(fd); // Converting file string to buffer (hex)

    let total_obj = buffer.readUInt8(0); // Reading number of objects
    countEl.setAttribute("value", total_obj);
    document.getElementById('guide').style.display = "block";

    // Reading table data
    for (i = 1; i != total_obj; i++) {
        numberSequential.innerText = 1;

        // Reading object ID
        var getObjId = buffer.readUint8(64);
        selectList.id = 1;
        selectList.selectedIndex = getObjId;
        imageEl.setAttribute("src", `./images/obj/${getObjId}.png`)

        // Reading index byte
        var getIndex = buffer.readUint8(66)
        indexEl.id = 1;
        indexEl.setAttribute("value", getIndex);

        // Reading position X
        var getPosX = buffer.readFloatLE(48).toFixed(2);
        posX.id = 1;
        posX.value = getPosX;

        // Reading position Y
        var getPosY = buffer.readFloatLE(52).toFixed(2);
        posY.id = 1;
        posY.value = getPosY;

        // Reading position Z
        var getPosZ = buffer.readFloatLE(56).toFixed(2);
        posZ.id = 1;
        posZ.value = getPosZ;

        // ROTATION
        // Reading rotation X
        var getRotX = buffer.readFloatLE(32).toFixed(2);
        rotX.id = 1;
        rotX.value = getRotX;

        // Reading rotation Y
        var getRotY = buffer.readFloatLE(36).toFixed(2);
        rotY.id = 1;
        rotY.value = getRotY;

        // Reading rotation Z
        var getRotZ = buffer.readFloatLE(40).toFixed(2);
        rotZ.id = 1;
        rotZ.value = getRotZ;

        cloneRow();
    }

    function cloneRow() {
        let clone = row.cloneNode(true); // Cloning row
        chunk = chunk + 64; // Reading next chunks
        seq++; // Slot for item number
        // select_id++;

        let cloneSeq = clone.querySelector(".number-sequential");
        cloneSeq.innerText = seq;

        // Getting object ID and image
        let cloneId = clone.querySelector(".objectList");
        let cloneImage = clone.querySelector(".imageEl")
        var getCloneObjId = buffer.readUint8(64 + chunk);
        cloneId.id = select_id++;
        cloneId.selectedIndex = getCloneObjId;
        cloneImage.setAttribute("src", `./images/obj/${getCloneObjId}.png`)

        // Getting index 
        let cloneIndex = clone.querySelector(".index");
        let getCloneIndex = buffer.readUint8(66 + chunk);
        cloneIndex.id = index_id++;
        cloneIndex.setAttribute("value", getCloneIndex);

        // Getting POSITION coordinates
        let clonePosX = clone.querySelector(".posX");
        let getClonePosX = buffer.readFloatLE(48 + chunk).toFixed(2);
        clonePosX.id = posX_id++;
        clonePosX.value = getClonePosX;

        let clonePosY = clone.querySelector(".posY");
        let getClonePosY = buffer.readFloatLE(52 + chunk).toFixed(2);
        clonePosY.id = posY_id++;
        clonePosY.value = getClonePosY;

        let clonePosZ = clone.querySelector(".posZ");
        let getClonePosZ = buffer.readFloatLE(56 + chunk).toFixed(2);
        clonePosZ.id = posZ_id++;
        clonePosZ.value = getClonePosZ;

        // Getting ROTATION coordinates
        let cloneRotX = clone.querySelector(".rotX");
        let getCloneRotX = buffer.readFloatLE(32 + chunk).toFixed(2);
        cloneRotX.id = rotX_id++;
        cloneRotX.value = getCloneRotX;

        let cloneRotY = clone.querySelector(".rotY");
        let getCloneRotY = buffer.readFloatLE(36 + chunk).toFixed(2);
        cloneRotY.id = rotY_id++;
        cloneRotY.value = getCloneRotY;

        let cloneRotZ = clone.querySelector(".rotZ");
        let getCloneRotZ = buffer.readFloatLE(40 + chunk).toFixed(2);
        cloneRotZ.id = rotZ_id++;
        cloneRotZ.value = getCloneRotZ;

        tBody.appendChild(clone); // Appending row to the end of table
        console.log(buffer.byteLength + " bytes")
    }


    /* ===============
        UPDATE
       =============== */

    // Getting object ID and image
    tBody.addEventListener("change", function (e) {
        if (e.target.className == "objectList") {
            // Gets value from select/option and sets to buffer
            var select_number = e.target.id; // Get clicked select ID attribute
            var select_opt = e.target.selectedIndex; // Get *option* selected from the clicked select
            var chunk_save = 64 * (parseInt(select_number) - 1); // Multiply chunk size by select ID number

            // Using the same class name for every image and using the *option* selected value as the index
            var imageChanger = document.getElementsByClassName("imageEl")[parseInt(select_number) - 1];
            buffer.writeUint8(select_opt, 64 + chunk_save); // Writes data on buffer on every hange
            imageChanger.setAttribute("src", `images/obj/${select_opt}.png`);
        }
        if (e.target.className == "index") {
            // Gets value from index input and sets to buffer
            var setIndex = e.target.value;
            var setIndexId = e.target.id;
            var chunk_save = 64 * (parseInt(setIndexId) - 1);
            if (setIndex <= 255) {
                buffer.writeUint8(setIndex, 66 + chunk_save);
                document.getElementsByClassName("index")[parseInt(setIndexId) - 1].style.color = "#000";
                document.getElementsByClassName("index")[parseInt(setIndexId) - 1].style.outline = "none";
            } else {
                document.getElementsByClassName("index")[parseInt(setIndexId) - 1].style.color = "red";
                document.getElementsByClassName("index")[parseInt(setIndexId) - 1].style.outline = "1px solid red";
            }
        }
        if (e.target.className == "posX") {
            // Gets value from posX and sets to buffer
            var setPosX = e.target.value;
            var setPosXid = e.target.id;
            var chunk_save = 64 * (parseInt(setPosXid) - 1);
            buffer.writeFloatLE(setPosX, 48 + chunk_save).toFixed(2);

        }
        if (e.target.className == "posY") {
            // Gets value from posY and sets to buffer
            var setPosY = e.target.value;
            var setPosYid = e.target.id;
            var chunk_save = 64 * (parseInt(setPosYid) - 1);
            buffer.writeFloatLE(setPosY, 52 + chunk_save).toFixed(2);

        }
        if (e.target.className == "posZ") {
            // Gets value from posY and sets to buffer
            var setPosZ = e.target.value;
            var setPosZid = e.target.id;
            var chunk_save = 64 * (parseInt(setPosZid) - 1);
            buffer.writeFloatLE(setPosZ, 56 + chunk_save).toFixed(2);

        }
        if (e.target.className == "rotX") {
            // Gets value from rotX and sets to buffer
            var setRotX = e.target.value;
            var setRotXid = e.target.id;
            var chunk_save = 64 * (parseInt(setRotXid) - 1);
            buffer.writeFloatLE(setRotX, 32 + chunk_save).toFixed(2);

        }
        if (e.target.className == "rotY") {
            // Gets value from rotY and sets to buffer
            var setRotY = e.target.value;
            var setRotYid = e.target.id;
            var chunk_save = 64 * (parseInt(setRotYid) - 1);
            buffer.writeFloatLE(setRotY, 36 + chunk_save).toFixed(2);

        }
        if (e.target.className == "rotZ") {
            // Gets value from rotZ and sets to buffer
            var setRotZ = e.target.value;
            var setRotZid = e.target.id;
            var chunk_save = 64 * (parseInt(setRotZid) - 1);
            buffer.writeFloatLE(setRotZ, 40 + chunk_save).toFixed(2);

        }
    })

    /* =======================
        ADD NEW OBJECT
       ======================= */

    function addNewObject() {
        let newEntry = Buffer.alloc(0x40); // Creates an empty entry
        let endOfFile;
        let temp_allEntries;
        let temp_buffer;
        if (buffer.at(buffer.length - 0x10) == 205 && buffer.at(buffer.length - 0x0F) == 205 && buffer.at(buffer.length - 0x0E) == 205) {
            endOfFile = Buffer.alloc(0x10, 205) // Creates CDCDCDCD bytes for end of file
            temp_allEntries = buffer.subarray(0x00, buffer.length - 0x10); // Getting header and all entries (this avoids conflicts with CDCDCD bytes)  
            newEntry.writeUInt8(buffer.at(0x42 + (0x40 * (total_obj - 1))) + 1, 0x32); // Updates index by +1
            temp_buffer = Buffer.concat([temp_allEntries, newEntry, endOfFile]); // Adds the entry to the end of the main buffer
        } else {
            newEntry.writeUInt8(buffer.at(0x42 + (0x40 * (total_obj - 1))) + 1, 0x32); // Updates index by +1
            temp_buffer = Buffer.concat([buffer, newEntry]); // Adds the entry to the end of the main buffer
        }

        buffer = temp_buffer;
        buffer.writeUint8(buffer.at(0x00) + 1, 0x00);
        countEl.setAttribute("value", parseInt(total_obj) + 1);
        total_obj++;
        cloneRow();
        textBox("New object entry added!", "green");
    }

    for (const button of addNewBtnEl) {
        button.addEventListener("click", function () {
            addNewObject();
        });
    }
    // CTRL + D adds new object
    window.addEventListener('keydown', function (e) {
        if (e.ctrlKey == true && e.key == "d")
            addNewObject();
    });

    // Save all modified buffer back to file
    saveBtn.addEventListener("click", () => {
        fs.writeFileSync(filepath, buffer);
        var saveMessage = document.querySelector(".hide");
        saveMessage.style.display = "block"
        setTimeout(() => {
            saveMessage.style.display = "none"
        }, 2000);
    })

    // NEEDS FIX!!
    closeBtn.addEventListener("click", () => {
        fs.closeSync(fd);
        ipcRenderer.send("closeETSfile");
    })

    ipcRenderer.on("saveAsETSfileContent", (e, arg) => {
        fs.writeFileSync(arg, buffer);
    })
});

