const fs = require('fs');
const { ipcRenderer } = require('electron');
const Alphabet = require('./mdt-alphabet');
const { triggerAsyncId } = require('async_hooks');

// Const for testing text output (DEBUG)

/* ===============
    CREATE
   =============== */

// Getting elements
var rootEl = document.querySelector("html");
var copyBtnEl = document.getElementById("copyBtn");
var pasteBtnEl = document.getElementById("pasteBtn");
var undoBtnEl = document.getElementById("undoBtn");
var redoBtnEl = document.getElementById("redoBtn");
var countEl = document.getElementById("count");
var headerFileName = document.getElementById("header-filename");
var headerFileSize = document.getElementById("header-filesize");
var showAllBtn = document.getElementById("show-all-btn");
var hideAllBtn = document.getElementById("hide-all-btn");
var messageContainerEl = document.getElementsByClassName("message-container");

// Getting language elements
const englishBtn = document.getElementById("englishBtn");
const deutschBtn = document.getElementById("deutschBtn");
const spanishBtn = document.getElementById("spanishBtn");
const frenchBtn = document.getElementById("frenchBtn");
const italianBtn = document.getElementById("italianBtn");
const englishLabel = document.getElementById("englishLabel");
const deutschLabel = document.getElementById("deutschLabel");
const spanishLabel = document.getElementById("spanishLabel");
const frenchLabel = document.getElementById("frenchLabel");
const italianLabel = document.getElementById("italianLabel");
const radioClassEl = document.getElementsByClassName("radio-languages");


// Const for getting Menu elements
const openFile = document.getElementById("openMDTfile");
const closeBtn = document.getElementById("closeMDTfile");
const saveBtn = document.getElementById("saveMDTfile");
const saveAsBtn = document.getElementById("saveMDTas");
const quitApp = document.getElementById("quitApp");
const minimizeBtn = document.getElementById("minimize");
const maximizeBtn = document.getElementById("maximize");
const closeWindowBtn = document.getElementById("closeWindow");
const oneRowBtn = document.getElementById("oneRow");
const twoRowsBtn = document.getElementById("twoRows");
const threeRowsBtn = document.getElementById("threeRows");

// Content elements
let containerLeftSideEl = document.getElementById("container-text");
let containerTextBoxEl = document.getElementsByClassName("principal-div");
let textBoxEl = document.getElementsByClassName("text-boxes");
let messageStartBtn = document.getElementById("messageStartBtn");
let messageEndBtn = document.getElementById("messageEndBtn");
let lineBreakBtn = document.getElementById("lineBreakBtn");
let newPageBtn = document.getElementById("newPageBtn");
let optionBtn = document.getElementById("optionBtn");
let pauseBtn = document.getElementById("pauseBtn");

// Menu actions (open/save/quit)
openFile.addEventListener("click", () => {
    ipcRenderer.send("openMDTfile")
    ipcRenderer.send("closeMDTfile")
})

saveAsBtn.addEventListener("click", () => {
    ipcRenderer.send("saveAsMDTfile")
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

// Change layout
oneRowBtn.addEventListener("click", function () {
    for (let i = 0; i < messageContainerEl.length; i++) {
        document.getElementsByClassName("principal-div")[i].style.width = "85%";
    }
})
twoRowsBtn.addEventListener("click", function () {
    for (let i = 0; i < messageContainerEl.length; i++) {
        document.getElementsByClassName("principal-div")[i].style.width = "47%";
    }
})
threeRowsBtn.addEventListener("click", function () {
    for (let i = 0; i < messageContainerEl.length; i++) {
        document.getElementsByClassName("principal-div")[i].style.width = "32%";
    }
})

/* ===============
    READ
   =============== */

// Global variables
var chunk = 0; // Will be incremented in each chunk
let wordCollector = '';
let messageIterator = 0;
let tempChars = '';

// Menu bar buttons 
copyBtnEl.addEventListener("click", () => document.execCommand("copy")); // Copy function
pasteBtnEl.addEventListener("click", () => document.execCommand("paste")); // Paste function
undoBtnEl.addEventListener("click", () => document.execCommand("undo")); // Undo function
redoBtnEl.addEventListener("click", () => document.execCommand("redo")); // Redo function

// Getting file path
ipcRenderer.on("mdtFileChannel", (e, filepath) => {

    var fd = fs.openSync(filepath); // Opening the file in memory
    var buffer = fs.readFileSync(fd); // Converting file string to buffer (hex)
    var MDTFileName = String(filepath);
    var conv = MDTFileName.replace(/^[^.]*\\/gm, '');

    let total_languages = buffer.readUInt8(0); // Reading number of languages
    if (total_languages > 6) {
        countEl.setAttribute("value", 1);
        deutschBtn.remove(); deutschLabel.remove();
        spanishBtn.remove(); spanishLabel.remove();
        frenchBtn.remove(); frenchLabel.remove();
        italianBtn.remove(); italianLabel.remove();
        englishLabel.innerText = "Single language"
    } else {
        countEl.setAttribute("value", total_languages - 1);
    }
    headerFileSize.value = buffer.length + " bytes";
    headerFileName.value = conv;

    // Core functions
    function createElement(index) {
        let firstDivTag = document.createElement("div");
        let detailsTag = document.createElement("details");
        let summaryTag = document.createElement("summary");
        let secondDivTag = document.createElement("div");
        let textareaTag = document.createElement("textarea");

        firstDivTag.classList.add("principal-div")
        detailsTag.classList.add("message-container");
        summaryTag.innerText = "Message " + index;
        secondDivTag.classList.add("details-expanded");
        textareaTag.classList.add("text-boxes");
        textareaTag.id = index;

        containerLeftSideEl.append(firstDivTag);
        firstDivTag.appendChild(detailsTag);
        secondDivTag.appendChild(textareaTag)
        detailsTag.append(summaryTag, secondDivTag);

    }
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
    function readMessages(languageOffset) {
        let count = 0; // Used and reused for many loop reasons
        messageIterator = 0;
        let engLanguageKey = buffer.readUint32LE(languageOffset); // Search for English language KEY
        if (total_languages > 6) { engLanguageKey = 0 }; // If the file contains a single language, it will read from start
        let engLanguageOffset = buffer.readUint8(engLanguageKey + 4); // Read messages quantity
        let pointers = []; // Array to every message pointer, for this particular language

        // Loop for each message box
        for (let index = 0; index != engLanguageOffset; index++) {
            createElement(Number(index + 1));
            count = 0;
            wordCollector = ''; // Empties the collector

            // Loop to detect and store every message pointer
            for (let i = 0; i != engLanguageOffset && index == 0; i++) {
                let j = buffer.readUint32LE(engLanguageKey + 8 + count); // Reads each message pointer
                count = count + 4;
                pointers.push(j); // Stores every message pointer, for this particular language
            }
            count = 0;

            // Loop to read every message chunk
            for (let k = 0; k < pointers.length; k++) {

                // Loop to read every char from a string
                for (let m = 0; ; m++) {
                    tempChars = buffer.readUint16LE(engLanguageKey + pointers[messageIterator] + count); // Reads every short

                    // Loop to check if the short read is a String Code
                    for (let n = 0; n < Object.keys(Alphabet.StringCodes).length; n++) {
                        if (tempChars == Object.keys(Alphabet.StringCodes)[n]) {
                            tempChars = Alphabet.StringCodes[n]

                            // Check if the String Code have a modifier (another short followed by it)
                            if (tempChars == "[print-speed]" || tempChars == "[core-17]" || tempChars == "[sleep]" ||
                                tempChars == "[align-left]" || tempChars == "[align-top]" || tempChars == "[core-6]" ||
                                tempChars == "[character]" || tempChars == "[message-change]" || tempChars == "[color]") {
                                wordCollector = wordCollector + tempChars; // Appends String Code character in the wordCollector
                                count = count + 2; // Reads modifier short
                                let modifierByte = buffer.readUint16LE(engLanguageKey + pointers[messageIterator] + count)
                                tempChars = `{${modifierByte}}`
                            }
                        }
                    }

                    count = count + 2; // Moves to next short
                    wordCollector = wordCollector + tempChars; // Appends each character in the wordCollector

                    // Only replace numbers that are bigger than 18, this skips the String Codes
                    if (tempChars > 18) {
                        wordCollector = wordCollector.replace(String(tempChars), Alphabet.Alphabet[tempChars]); // Convert short to char
                    }

                    // Does the same of the above, but ends loop if finds String Code [message-end] and completes one message
                    if (buffer.readUint16LE(engLanguageKey + pointers[messageIterator] + count) == 1) {
                        tempChars = buffer.readUint16LE(engLanguageKey + pointers[messageIterator] + count);
                        if (tempChars == 1) {
                            tempChars = "[message-end]";
                        }
                        wordCollector = wordCollector + tempChars;
                        break;
                    }
                } messageIterator++; // Changes which pointer should be read
                break;
            }
            count = 0;
            textBoxEl[index].innerHTML = wordCollector; // Outputs the converted string into the text boxes
        }
    }


    // Language buttons
    englishBtn.addEventListener("click", () => {
        while (containerTextBoxEl[0]) {
            containerTextBoxEl[0].parentNode.removeChild(containerTextBoxEl[0]);
        }
        readMessages(8);
    });
    frenchBtn.addEventListener("click", () => {
        while (containerTextBoxEl[0]) {
            containerTextBoxEl[0].parentNode.removeChild(containerTextBoxEl[0]);
        }
        readMessages(12)
    });
    deutschBtn.addEventListener("click", () => {
        while (containerTextBoxEl[0]) {
            containerTextBoxEl[0].parentNode.removeChild(containerTextBoxEl[0]);
        }
        readMessages(16)
    });
    italianBtn.addEventListener("click", () => {
        while (containerTextBoxEl[0]) {
            containerTextBoxEl[0].parentNode.removeChild(containerTextBoxEl[0]);
        }
        readMessages(20)
    });
    spanishBtn.addEventListener("click", () => {
        while (containerTextBoxEl[0]) {
            containerTextBoxEl[0].parentNode.removeChild(containerTextBoxEl[0]);
        }
        readMessages(24)
    });

    // Panel buttons
    showAllBtn.addEventListener("click", function () {
        for (i = 0; i < messageContainerEl.length; i++) {
            messageContainerEl[i].setAttribute("open", "true");
        }
    })
    hideAllBtn.addEventListener("click", function () {
        for (i = 0; i < messageContainerEl.length; i++) {
            messageContainerEl[i].removeAttribute("open");
        }
    })


    /* ===============
        UPDATE
       =============== */

    const LanguagesObject = {
        0: 8,
        1: 12,
        2: 16,
        3: 20,
        4: 24
    }

    containerLeftSideEl.addEventListener("change", function (e) {
        let languageKey = 0;
        let messageCount = 0;
        let pointers = [];
        let count = 0;
        let content = "";
        let char = "";
        let frenchOffset = buffer.readUint32LE(12);
        let deutschOffset = buffer.readUint32LE(16);
        let italianOffset = buffer.readUint32LE(20);
        let spanishOffset = buffer.readUint32LE(24);
        let regexNumber = /\[[\d]\]/gm;
        let regexStringCodes = /(\[(?:\[??[^\[]*?\]))/gm;
        let regexCodes = /\[[\w].*?\]/gm; // Same as above, but simpler
        let regexletterOutside = /[^\[\d\]]+/g;

        for (let index = 0; index < 5; index++) {
            if (radioClassEl[index].checked) {
                languageKey = buffer.readUint32LE(LanguagesObject[index]);
                messageCount = buffer.readUint8(languageKey + 4);

                // Stores every message pointer inside
                for (let index = 0; index < messageCount; index++) {
                    pointers.push(buffer.readUint32LE(languageKey + 8 + count))
                    count += 4;
                }
                if (document.activeElement.nodeName == "TEXTAREA") {
                    content = document.activeElement.value;
                    document.activeElement.value = content;
                }

                // Gets all String Codes and envolves them into square brackets
                for (let m = 0; m < content.length; m++) {
                    content = content.replaceAll(Alphabet.StringCodes[m], `[${Object.keys(Alphabet.StringCodes)[m]}]`);
                }

                // Gets all characters outside square brackets and converts them to number
                for (let n = 0; n < content.length; n++) {
                    for (let j = 0; j < Object.keys(Alphabet.Alphabet).length; j++) {

                        if (content[n] == Alphabet.Alphabet[j + 128] && regexletterOutside.test(content[n])) {
                            content = content.replace(Alphabet.Alphabet[j + 128], Object.keys(Alphabet.Alphabet)[j]);
                        }
                    }
                }

                /* ======================================================================
                    FALTA: escrever campo de texto de volta pro arquivo;
                    criar padding de 0x20 bytes zerados antes do language key
                    criar lógica para adicionar padding, ex: digitou 1 caractere, adiciona 15 padding
                    --
                    fazer testes com MDT de 1 idioma
                    cirar lógica para pegar modifier dos string codes
                   ====================================================================== */

                console.log(content);
            }
        }
    })

    messageStartBtn.addEventListener("click", function (e) {
        for (let i = 0; i < textBoxEl.length; i++) {
            if (document.activeElement === textBoxEl[i]) {
                textBoxEl[i].innerHTML += "[message-start]"
            }
        }
    })
    messageEndBtn.addEventListener("click", function (e) {
        for (let i = 0; i < textBoxEl.length; i++) {
            if (document.activeElement === textBoxEl[i]) {
                textBoxEl[i].innerHTML += "[message-end]"
            }
        }
    })
    lineBreakBtn.addEventListener("click", function (e) {
        for (let i = 0; i < textBoxEl.length; i++) {
            if (document.activeElement === textBoxEl[i]) {
                textBoxEl[i].innerHTML += "[line-break]"
            }
        }
    })
    newPageBtn.addEventListener("click", function (e) {
        for (let i = 0; i < textBoxEl.length; i++) {
            if (document.activeElement === textBoxEl[i]) {
                textBoxEl[i].innerHTML += "[new-page]"
            }
        }
    })
    optionBtn.addEventListener("click", function (e) {
        for (let i = 0; i < textBoxEl.length; i++) {
            if (document.activeElement === textBoxEl[i]) {
                textBoxEl[i].innerHTML += "[option]"
            }
        }
    })
    pauseBtn.addEventListener("click", function (e) {
        for (let i = 0; i < textBoxEl.length; i++) {
            if (document.activeElement === textBoxEl[i]) {
                textBoxEl[i].innerHTML += "[pause]"
            }
        }
    })

    /* ==========================================
        NEW FUNCIONALITY: 
       ========================================== */

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
        ipcRenderer.send("closeMDTfile");
    })

    ipcRenderer.on("saveAsMDTfileContent", (e, arg) => {
        fs.writeFileSync(arg, buffer);
    })
})
