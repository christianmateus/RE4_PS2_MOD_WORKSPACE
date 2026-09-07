const fs = require('fs');
const { exec } = require('node:child_process');
const { ipcRenderer } = require('electron');
const path = require('path');
const textBox = require('../shared/textBox');

/* ===============
    CREATE
   =============== */

// Getting elements
var rootEl = document.querySelector("html");
var scanVagBtnEl = document.getElementById("scanVagBtn");
var scanBtnEl = document.getElementById("scanBtn");
var copyBtnEl = document.getElementById("copyBtn");
var pasteBtnEl = document.getElementById("pasteBtn");
var undoBtnEl = document.getElementById("undoBtn");
var redoBtnEl = document.getElementById("redoBtn");
var includeBackupBtnEl = document.getElementById("include-backup");
var containerAudiosEl = document.getElementById("container-geral");
var containerWavAudiosEl = document.getElementById("container-wav-converted");
var headerFileName = document.getElementById("header-filename");
var headerFileSize = document.getElementById("header-filesize");
var countEl = document.getElementById("count");

// Getting audio elements
const exportAllBtn = document.getElementById("exportAllBtn");
const convertAllWavBtn = document.getElementById("exportAllWavBtn");
const playBtn = document.getElementById("playBtn");

// Const for getting Menu elements
const openFile = document.getElementById("openSNDfile");
const closeBtn = document.getElementById("closeSNDfile");
const saveBtn = document.getElementById("saveSNDfile");
const saveAsBtn = document.getElementById("saveSNDas");
const quitApp = document.getElementById("quitApp");
const helpBtn = document.getElementById("help");
const minimizeBtn = document.getElementById("minimize");
const maximizeBtn = document.getElementById("maximize");
const closeWindowBtn = document.getElementById("closeWindow");

// Menu actions (open/save/quit)
openFile.addEventListener("click", () => {
   ipcRenderer.send("openSNDfile")
   ipcRenderer.send("closeSNDfile")
})

saveAsBtn.addEventListener("click", () => {
   ipcRenderer.send("saveAsSNDfile")
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

// Basic funcionalities

helpBtn.addEventListener("click", function () {
   ipcRenderer.send("showHelp");
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
ipcRenderer.on("sndFileChannel", (e, filepath) => {

   var fd = fs.openSync(filepath); // Opening the file in memory
   var buffer = fs.readFileSync(fd); // Converting file string to buffer (hex)

   // Defining headers
   var SNDFileName = String(filepath);
   var SNDFileName_converted = SNDFileName.replace(/^[^.]*\\/gm, '');
   headerFileSize.value = buffer.length + " bytes"; // Outputing file size to the header
   headerFileName.value = SNDFileName_converted; // Outputing file name to the header

   // Reading SND file header 
   const firstAudioOffset = buffer.readUint32LE(0x6C);

   // Splitting the file in parts
   var buffer_initial = buffer.subarray(0, buffer.indexOf("56616769", "hex"));
   var buffer_VAG = buffer.subarray(buffer.indexOf("56616769", 0, "hex"), firstAudioOffset);
   var buffer_audios = Buffer.alloc(0);
   var buffer_second_block = Buffer.alloc(0);
   var buffer_VAG_second_block = Buffer.alloc(0);
   var buffer_audios_second_block = Buffer.alloc(0);

   const secondBlockStart = buffer.readUint32LE(0x8C);
   const secondBlockFirstAudioOffset = buffer.readUint32LE(0xCC);

   // Verifies if values are not null to find another sound block
   if (buffer_initial.at(0x80) < 254 && buffer_initial.at(0xA0) < 254 && buffer_initial.at(0xC0) < 254) {
      // Verifies if pointers are valid
      if (secondBlockStart != 0 && secondBlockFirstAudioOffset > secondBlockStart) {

         // Defines first block audios length
         buffer_audios = buffer.subarray(firstAudioOffset, secondBlockStart);

         // Second block for scenarios (rXXX) .snd files
         buffer_second_block = buffer.subarray(secondBlockStart, buffer.indexOf("56616769", secondBlockStart, "hex"));
         buffer_VAG_second_block = buffer.subarray(buffer.indexOf("56616769", secondBlockStart, "hex"), secondBlockFirstAudioOffset);
         buffer_audios_second_block = buffer.subarray(secondBlockFirstAudioOffset);

         //Ativar quando for criar suporte para audios de cenarios
         // countEl.value = buffer_VAG.at(0x08) + buffer_VAG_second_block.at(0x08);
         countEl.value = buffer_VAG.at(0x08);
      }
   } else {
      buffer_audios = buffer.subarray(firstAudioOffset); // Sets that .snd file has only 1 audio block
      countEl.value = buffer_VAG.at(0x08);
   }

   // Getting VAG values
   let audioStartOffset = buffer_VAG.readUint32LE(16);
   let audioLength = buffer_VAG.readUint32LE(20);
   let frequencyBigEndian = Buffer.alloc(4);
   let lengthBigEndian = Buffer.alloc(4);
   let audioFrequency = buffer_VAG.readUint32LE(28);

   // Removing extension from folder name
   var folderName = headerFileName.value.substring(0, headerFileName.value.length - 4);

   // Core functions
   function createAudioElement(audioNumber, isWAV) {
      let div = document.createElement("div");
      let span = document.createElement("span");
      let importBtn = document.createElement("button");
      let exportBtn = document.createElement("button");
      let wavImg = document.createElement("img");
      let vagImg = document.createElement("img");
      let sndImg = document.createElement("img");
      let playImg = document.createElement("img");
      let pauseImg = document.createElement("img");
      let exportVagImg = document.createElement("img");
      let convertBtn = document.createElement("button");
      let convertToVagBtn = document.createElement("button");
      let playBtn = document.createElement("button");
      let pauseBtn = document.createElement("button");

      if (isWAV == false) {
         div.classList.add("container-audio-items");
         div.id = `${audioNumber + 1}`

         if (fs.existsSync(`SND/${folderName}`)) {
            // const inputPath = `${path.join(__dirname, '..', 'SND', folderName)}`;
            const inputPath = `${path.join('SND', folderName)}`;
            let audioFiles = fs.readdirSync(inputPath, { withFileTypes: true });

            // Filters the audio files to ignore all subfolders, this leaves the array with only .wav files
            let filesNames = audioFiles
               .filter(dirent => dirent.isFile())
               .map(dirent => dirent.name);

            span.innerText = `VAG: ${filesNames[audioNumber]}`;

            if (span.innerText == "undefined") {
               span.innerText = `VAG: Not exported`;
            }
         }

         span.id = `span-${audioNumber + 1}`
         // wavImg.src = `${path.join(__dirname, "..", "images", "wav-64.png")}`;
         sndImg.src = `${path.join("images", "import-snd.png")}`;
         sndImg.style.float = "left";
         sndImg.width = 16;
         sndImg.style.marginRight = "4px";

         exportVagImg.src = `${path.join("images", "export-vag.png")}`;
         exportVagImg.style.float = "left";
         exportVagImg.width = 16;
         exportVagImg.style.marginRight = "4px";

         wavImg.src = `${path.join("images", "wav-64.png")}`;
         wavImg.style.float = "left";
         wavImg.width = 16;
         wavImg.style.marginRight = "4px";

         importBtn.classList.add("win7-btn");
         importBtn.id = `import-${audioNumber + 1}`;
         importBtn.innerText = "Import to .snd";

         exportBtn.classList.add("win7-btn");
         exportBtn.id = `export-${audioNumber + 1}`;
         exportBtn.innerText = "Export .vag";
         convertBtn.classList.add("win7-btn");
         convertBtn.style.display = "inline-block";
         convertBtn.id = `convert-${audioNumber + 1}`;
         convertBtn.innerHTML = "Convert to .wav";

      } else {
         div.classList.add("container-audio-items");
         div.id = `${audioNumber + 1}`

         // const inputPath = `${path.join(__dirname, '..', 'SND', folderName, 'wav')}`;
         const inputPath = `${path.join('SND', folderName, 'wav')}`;
         let audioFiles = fs.readdirSync(inputPath, { withFileTypes: true });

         // Filters the audio files to ignore all subfolders, this leaves the array with only .wav files
         let filesNames = audioFiles
            .filter(dirent => dirent.isFile())
            .map(dirent => dirent.name);
         filesNames.sort((a, b) => a - b);

         span.innerText = `${filesNames[audioNumber]}`;

         playImg.src = `${path.join("images", "play.png")}`;
         playImg.style.float = "left";
         playImg.width = 16;
         playImg.style.marginRight = "4px";

         pauseImg.src = `${path.join("images", "pause.png")}`;
         pauseImg.style.float = "left";
         pauseImg.width = 16;
         pauseImg.style.marginRight = "4px";

         vagImg.src = `${path.join("images", "vag.png")}`;
         vagImg.style.float = "left";
         vagImg.width = 16;
         vagImg.style.marginRight = "4px";

         playBtn.classList.add("win7-btn");
         playBtn.innerHTML = 'Play';
         playBtn.id = `play-${audioNumber + 1}`;

         pauseBtn.classList.add("win7-btn");
         pauseBtn.innerHTML = 'Pause';
         pauseBtn.id = `pause-${audioNumber + 1}`;

         convertToVagBtn.classList.add("win7-btn");
         convertToVagBtn.innerHTML = 'Convert to .vag';
         convertToVagBtn.id = `convertToVag-${audioNumber + 1}`;
      }

      if (isWAV) {
         div.appendChild(span);
         div.appendChild(playBtn);
         playBtn.appendChild(playImg);
         // div.appendChild(pauseBtn);
         // pauseBtn.appendChild(pauseImg);
         div.appendChild(convertToVagBtn);
         convertToVagBtn.appendChild(vagImg);
         containerWavAudiosEl.appendChild(div);
      } else {
         div.appendChild(span);
         div.appendChild(importBtn);
         importBtn.appendChild(sndImg);
         div.appendChild(exportBtn);
         exportBtn.appendChild(exportVagImg);
         div.appendChild(convertBtn);
         convertBtn.appendChild(wavImg);
         containerAudiosEl.appendChild(div);
      }

   }

   function checkWavFolder() {
      if (fs.existsSync(`SND/${folderName}/wav`)) {

         //Empties the container
         containerWavAudiosEl.innerHTML = `<h5>WAV Converted Audios</h5>`

         // Create WAV elements based on file count inside the wav folder
         let wavAudioFiles = fs.readdirSync(`SND/${folderName}/wav`);
         for (let index = 0; index != wavAudioFiles.length; index++) {
            createAudioElement(index, true);
         }
      }
   }

   function exportAllAudios() {
      let audioOffsetIterator = 0; // Iterates through each sound offset

      for (let j = 0; j != countEl.value; j++) {
         let audioLength = buffer_VAG.readUint32LE(20 + audioOffsetIterator); // Gets audio length from VAG area (in .snd)
         let audioFrequency = buffer_VAG.readUint32LE(28 + audioOffsetIterator); // Gets audio frequency from VAG area (in .snd)
         lengthBigEndian.writeUint32BE(audioLength); // Converts length to Big Endian
         frequencyBigEndian.writeUint32BE(audioFrequency); // Converts frequency to Big Endian

         // Creates VAG Header
         let vagHeader = Buffer.alloc(0x40, `564147700000000600000000${lengthBigEndian.toString("hex")}${frequencyBigEndian.toString("hex")}000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000`, "hex");

         // Gets a single complete audio
         let audioFile = buffer_audios.subarray(buffer_VAG.readUint32LE(16 + audioOffsetIterator),
            buffer_VAG.readUint32LE(16 + audioOffsetIterator) + buffer_VAG.readUint32LE(20 + audioOffsetIterator));

         // Insert the header at the start of the audio
         audioFile = Buffer.concat([vagHeader, audioFile]);

         fs.mkdirSync(`SND/${folderName}`, { recursive: true });

         // Check if the user wants backup
         if (includeBackupBtnEl.checked) {
            fs.mkdirSync(`SND/${folderName}/backup`, { recursive: true });
         }

         // Writes the .vag files
         if (j < 9) {
            fs.writeFileSync(`SND/${folderName}/${folderName}_0${j + 1}.vag`, audioFile);
            if (includeBackupBtnEl.checked) {
               fs.writeFileSync(`SND/${folderName}/backup/${folderName}_0${j + 1}.vag`, audioFile);
            }
         } else {
            fs.writeFileSync(`SND/${folderName}/${folderName}_${j + 1}.vag`, audioFile);
            if (includeBackupBtnEl.checked) {
               fs.writeFileSync(`SND/${folderName}/backup/${folderName}_${j + 1}.vag`, audioFile);
            }
         }
         audioOffsetIterator += 16;
      }
      checkVagFolder();
      textBox.module("All audios exported as .vag", "green");
   }

   function convertToWav(inputFile, outputFile) {
      if (!fs.existsSync(`SND/${folderName}/wav`)) {
         fs.mkdirSync(`SND/${folderName}/wav`, { recursive: true });
      }

      // Change file extension
      let outputFileWAV = outputFile.replace(/\.[^.]+$/, '.wav');

      // USED IN DEVELOPING STAGE ======================================
      // const mfaudio = `${path.join(__dirname, '..', 'resources', 'mfaudio.exe')}`;
      // const inputPath = `${path.join(__dirname, '..', 'SND', folderName, inputFile)}`;
      // const outputPath = `${path.join(__dirname, '..', 'SND', folderName, 'wav', outputFileWAV)}`;

      // USED IN FINAL PRODUCT ======================================
      const mfaudio = `${path.join('resources', 'mfaudio.exe')}`;
      const inputPath = `"${path.join(__dirname, '..', '..', '..', 'SND', folderName, inputFile)}"`;
      const outputPath = `"${path.join(__dirname, '..', '..', '..', 'SND', folderName, 'wav', outputFileWAV)}"`;

      // USED IN DEVELOPING STAGE ======================================
      // console.log(inputPath);
      // console.log(mfaudio);
      // exec(`${mfaudio} /OTWAVU "${inputPath}" "${outputPath}"`, function (e) {
      //    console.log(e);
      //    console.log("Entrou no callback");
      // });

      // USED IN FINAL PRODUCT ======================================
      var child = require('child_process').execFile;
      var executablePath = `${path.join('resources', 'mfaudio.exe')}`;
      var parameters = ['/OTWAVU', inputPath, outputPath];
      child(executablePath, parameters, { shell: true }, function (err, data) {
         console.log(err)
         console.log(data.toString());
      });

   }

   function convertToVag(inputFile, outputFile) {
      // Only converts if directory exists
      // if (fs.existsSync(`${path.join(__dirname, '..', 'SND', folderName, 'wav')}`)) {
      if (fs.existsSync(`${path.join('SND', folderName, 'wav')}`)) {

         // Change file extension
         let outputFileVAG = outputFile.replace(/\.[^.]+$/, '.vag');

         // USED IN DEVELOPING STAGE ======================================
         // const mfaudio = `${path.join(__dirname, '..', 'resources', 'mfaudio.exe')}`;
         // const inputPath = `${path.join(__dirname, '..', 'SND', folderName, 'wav', inputFile)}`;
         // const outputPath = `${path.join(__dirname, '..', 'SND', folderName, outputFileVAG)}`;


         // USED IN FINAL PRODUCT ======================================
         // const mfaudio = `${path.join('resources', 'mfaudio.exe')}`;
         const inputPath = `"${path.join(__dirname, '..', '..', '..', 'SND', folderName, 'wav', inputFile)}"`;
         const outputPath = `"${path.join(__dirname, '..', '..', '..', 'SND', folderName, outputFileVAG)}"`;

         // exec(`${mfaudio} /OTVAGC "${inputPath}" "${outputPath}"`)

         // USED IN FINAL PRODUCT ======================================
         var child = require('child_process').execFile;
         var executablePath = `${path.join('resources', 'mfaudio.exe')}`;
         var parameters = ['/OTVAGC', inputPath, outputPath];
         child(executablePath, parameters, { shell: true }, function (err, data) {
            console.log(err)
            console.log(data.toString());
         });

         setTimeout(() => {
            textBox.module("Audio converted to .vag!", "green");
         }, 600);
      } else {
         textBox.module("Default directory does not exists!", "red");
         console.log("Directory does not exists");
      }
   }

   convertAllWavBtn.addEventListener("click", function () {
      // USED IN DEVELOPING STAGE ======================================
      // const inputPath = `${path.join(__dirname, '..', 'SND', folderName)}`;

      // USED IN FINAL PRODUCT ======================================
      const inputPath = `${path.join('SND', folderName)}`;
      let audioFiles = fs.readdirSync(inputPath, { withFileTypes: true });

      // Filters the audio files to ignore all subfolders, this leaves the array with only .wav files
      let filesNames = audioFiles
         .filter(dirent => dirent.isFile())
         .map(dirent => dirent.name);

      // Iterates through each .vag audio and converts to .wav
      for (let i = 0; i != filesNames.length; i++) {
         convertToWav(filesNames[i], filesNames[i]);
      }

      // Spawns WAV elements on right side of the screen
      if (filesNames.length > 10) {
         setTimeout(() => {
            checkWavFolder();
         }, 10000);
      } else {
         setTimeout(() => {
            checkWavFolder();
         }, 5000);
      }
      textBox.module("All audios converted to .wav!", "blue");
   });

   function checkVagFolder() {
      // Verifies if directory exists
      if (!fs.existsSync(`SND/${folderName}`)) {
         fs.mkdirSync(`SND/${folderName}`, { recursive: true });
      }

      containerAudiosEl.innerHTML = `<h5>Audio List</h5>`
      // Create .vag audio elements for each audio inside .snd
      for (let i = 0; i != countEl.value; i++) {
         createAudioElement(i, false);
      }
   }

   // Check if there are any VAG files on page load, and then create elements
   checkVagFolder();

   // Check if there are any WAV files on page load, and then create elements
   checkWavFolder();

   // Export or convert a single .vag audio, and also reimport back to .snd
   containerAudiosEl.addEventListener("click", function ({ target }) {

      // Exports a single .vag audio
      for (let i = 1; i != countEl.value + 1; i++) {
         if (target.id == `export-${i}`) {
            let audioLength = buffer_VAG.readUint32LE(20 + (16 * (i - 1))); // Gets audio length from VAG area (in .snd)
            let audioFrequency = buffer_VAG.readUint32LE(28 + (16 * (i - 1))); // Gets audio frequency from VAG area (in .snd)
            lengthBigEndian.writeUint32BE(audioLength); // Converts length to Big Endian
            frequencyBigEndian.writeUint32BE(audioFrequency); // Converts frequency to Big Endian

            // Creates VAG Header
            let vagHeader = Buffer.alloc(0x40, `564147700000000600000000${lengthBigEndian.toString("hex")}${frequencyBigEndian.toString("hex")}000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000`, "hex");

            // Gets a single complete audio
            let audioFile = buffer_audios.subarray(buffer_VAG.readUint32LE(16 + (16 * (i - 1))),
               buffer_VAG.readUint32LE(16 + (16 * (i - 1))) + buffer_VAG.readUint32LE(20 + (16 * (i - 1))));

            // Insert the header at the start of the audio
            audioFile = Buffer.concat([vagHeader, audioFile]);

            fs.mkdirSync(`SND/${folderName}`, { recursive: true });

            // Check if the user wants backup
            if (includeBackupBtnEl.checked) {
               fs.mkdirSync(`SND/${folderName}/backup`, { recursive: true });
            }

            if (i < 10) {
               fs.writeFileSync(`SND/${folderName}/${folderName}_0${i}.vag`, audioFile);
               document.getElementById(`span-${i}`).innerText = `VAG: ${folderName}_0${i}.vag`;
               if (includeBackupBtnEl.checked) {
                  fs.writeFile(`SND/${folderName}/backup/${folderName}_0${i}.vag`, audioFile, function (e) {
                     // console.log(e);
                  });
               }
            } else {
               fs.writeFileSync(`SND/${folderName}/${folderName}_${i}.vag`, audioFile);
               document.getElementById(`span-${i}`).innerText = `VAG: ${folderName}_${i}.vag`;
               if (includeBackupBtnEl.checked) {
                  fs.writeFile(`SND/${folderName}/backup/${folderName}_${i}.vag`, audioFile, function (e) {
                     console.log(e);
                  });
               }
            }
            textBox.module("Audio exported as .vag", "green");
         }
      }

      // Convert a single .vag to .wav
      for (let i = 1; i != countEl.value + 1; i++) {
         if (target.id == `convert-${i}`) {
            const inputPath = `${path.join(__dirname, '..', '..', '..', 'SND', folderName)}`;
            console.log(inputPath);
            // const inputPath = `${path.join('SND', folderName)}`;
            let audioFiles = fs.readdirSync(inputPath, { withFileTypes: true });
            // console.log(inputPath);
            const spanParent = target.parentNode; // Gets play button parent
            const spanText = spanParent.children[0].innerText; // Access parent first child, and gets text inside span
            const filename = spanText.substring(5); // Access parent's first child, and gets text inside 

            // Filters the audio files to ignore all subfolders, this leaves the array with only .wav files
            let filesNames = audioFiles
               .filter(dirent => dirent.isFile())
               .map(dirent => dirent.name);

            // Checks if .vag file exists, then tell in which position it is on the filesNames array
            if (fs.existsSync(`${path.join(__dirname, '..', '..', '..', 'SND', folderName, filename)}`)) {
               // if (fs.existsSync(`${path.join('SND', folderName, filename)}`)) {
               for (let x = 0; x != filesNames.length; x++) {
                  if (filesNames[x] == filename) {

                     // Converts to .wav
                     convertToWav(filesNames[x], filesNames[x]);
                     break;
                  }
               }
            } else {
               ipcRenderer.send("errorMessage");
            }

            // Spawns WAV elements on right side of the screen
            setTimeout(() => {
               checkWavFolder();
            }, 1400);
            setTimeout(() => {
               textBox.module("Audio converted to .wav!", "blue");
            }, 600);
         }
      }

      // Reimport .vag to .snd
      for (let i = 1; i != countEl.value + 1; i++) {
         if (target.id == `import-${i}`) {
            const spanParent = target.parentNode; // Gets import button parent
            const spanText = spanParent.children[0].innerText; // Access parent's first child, and gets text inside span
            const filename = spanText.substring(5); // Access parent's first child, and gets text inside 
            // USED IN BUILD
            const filepath = path.join(__dirname, '..', '..', '..', 'SND', folderName, filename);
            console.log(filepath);
            // USED IN DEVELOPMENT STAGE
            // const filepath = path.join("SND", folderName, filename);
            let audioOffsetInSND;
            let audioLengthInSND;
            let audioIndex = spanText.slice(-7, -4); // Gets number from filename
            let audioBuffer;

            // Checks filename index
            if (Number(audioIndex) > 99) {
               audioOffsetInSND = buffer_VAG.readUint32LE(16 + (16 * (audioIndex - 1)));
               audioLengthInSND = buffer_VAG.readUint32LE(20 + (16 * (audioIndex - 1)));

            } else if (Number(audioIndex.substring(1)) < 10) {
               console.log("Menor que dez:" + audioIndex);
               audioOffsetInSND = buffer_VAG.readUint32LE(16 + (16 * (audioIndex.substring(1) - 1)));
               audioLengthInSND = buffer_VAG.readUint32LE(20 + (16 * (audioIndex.substring(1) - 1)));

            } else if (Number(audioIndex.substring(1)) < 100 && Number(audioIndex) != NaN) {
               console.log("Menor que cem:" + audioIndex);
               audioOffsetInSND = buffer_VAG.readUint32LE(16 + (16 * (audioIndex.substring(1) - 1)));
               audioLengthInSND = buffer_VAG.readUint32LE(20 + (16 * (audioIndex.substring(1) - 1)));
            }

            if (fs.existsSync(filepath)) {
               audioBuffer = fs.readFileSync(filepath);
               let audioBufferNoHeader = audioBuffer.subarray(0x40, audioBuffer.length - 0x30);
               let superior_snd;
               let inferior_snd;
               let complete_snd;

               if (Number(audioIndex.substring(1)) == 1) {
                  superior_snd = buffer_audios.subarray(audioOffsetInSND, audioOffsetInSND);
               } else {
                  superior_snd = buffer_audios.subarray(0, (audioOffsetInSND));
               }

               // Checks filename index for snd bottom part
               if (Number(audioIndex > 99)) {
                  audioOffsetInSND = buffer_VAG.readUint32LE(16 + (16 * (audioIndex)));
                  audioLengthInSND = buffer_VAG.readUint32LE(20 + (16 * (audioIndex)));
                  audioLengthInSND = buffer_VAG.writeUint32LE(audioBufferNoHeader.length + 0x30, 20 + (16 * (audioIndex - 1)));
               } else if (Number(audioIndex.substring(1)) < 10) {
                  audioOffsetInSND = buffer_VAG.readUint32LE(16 + (16 * (audioIndex.substring(1))));
                  audioLengthInSND = buffer_VAG.readUint32LE(20 + (16 * (audioIndex.substring(1))));
                  audioLengthInSND = buffer_VAG.writeUint32LE(audioBufferNoHeader.length + 0x30, 20 + (16 * (audioIndex.substring(1) - 1)));
               } else if (Number(audioIndex.substring(1)) < 100 && Number(audioIndex) != NaN) {
                  audioOffsetInSND = buffer_VAG.readUint32LE(16 + (16 * (audioIndex.substring(1))));
                  audioLengthInSND = buffer_VAG.readUint32LE(20 + (16 * (audioIndex.substring(1))));
                  audioLengthInSND = buffer_VAG.writeUint32LE(audioBufferNoHeader.length + 0x30, 20 + (16 * (audioIndex.substring(1) - 1)));
               }

               inferior_snd = buffer_audios.subarray(audioOffsetInSND - 0x30);
               complete_snd = Buffer.concat([superior_snd, audioBufferNoHeader, inferior_snd]);
               buffer_audios = complete_snd;

               let acumulator = 0;
               let offsetIterator = 0;
               for (let k = 0; k < countEl.value; k++) {
                  acumulator += buffer_VAG.readUint32LE(20 + offsetIterator);
                  // console.log(acumulator);
                  buffer_VAG.writeUInt32LE(acumulator, 32 + offsetIterator);
                  offsetIterator += 16;
               }

               // Writes the .vag inside .snd container and compiles it
               if (!fs.existsSync(`SND/${folderName}/compiled`)) {
                  fs.mkdirSync(`SND/${folderName}/compiled`, { recursive: true });
               }
               let COMPLETE_BUFFER = Buffer.concat([buffer_initial, buffer_VAG, buffer_audios]);
               fs.writeFileSync(`SND/${folderName}/compiled/${folderName}.snd`, COMPLETE_BUFFER);

               textBox.module("Audio imported and compiled to SND!", "green");

            } else {
               if (!filename.includes(audioIndex)) {
                  ipcRenderer.send("filenameError");
               } else
                  ipcRenderer.send("vagAbsent");
            }
         }
      }

   })

   // Play or convert .wav sounds
   containerWavAudiosEl.addEventListener("click", function (e) {
      // USED IN BUILD
      const inputPath = `${path.join(__dirname, '..', '..', '..', 'SND', folderName, 'wav')}`;
      // USED IN DEVELOPMENT STAGE
      // const inputPath = `${path.join('SND', folderName, 'wav')}`;
      let audioFiles = fs.readdirSync(inputPath, { withFileTypes: true });

      // Filters the audio files to ignore all subfolders, this leaves the array with only .wav files
      let filesNames = audioFiles
         .filter(dirent => dirent.isFile())
         .map(dirent => dirent.name);

      // Plays a .wav sound
      for (let i = 1; i != filesNames.length + 1; i++) {
         if (e.target.id == `play-${i}`) {
            const spanParent = e.target.parentNode; // Gets play button parent
            const spanText = spanParent.children[0].innerText; // Access parent first child, and gets text inside span

            // USED IN DEVELOPMENT STAGE
            // new Audio(`${path.join('SND', folderName, 'wav', spanText)}`).play();

            // USED IN BUILD
            new Audio(`${path.join('..', '..', 'SND', folderName, 'wav', spanText)}`).play();
         }
      }

      // Pauses a .wav sound
      for (let i = 1; i != filesNames.length + 1; i++) {
         if (e.target.id == `pause-${i}`) {
            const spanParent = e.target.parentNode; // Gets pause button parent
            const spanText = spanParent.children[0].innerText; // Access parent first child, and gets text inside span

            // USED IN DEVELOPMENT STAGE
            // new Audio(`${path.join('SND', folderName, 'wav', spanText)}`).pause();

            // USED IN BUILD
            new Audio(`${path.join('..', '..', 'SND', folderName, 'wav', spanText)}`).pause();
         }
      }

      // Convert a single .wav to .vag
      for (let i = 1; i != countEl.value + 1; i++) {
         if (e.target.id == `convertToVag-${i}`) {
            // const inputPath = `${ path.join('SND', folderName, 'wav') }`;
            let audioFiles = fs.readdirSync(inputPath, { withFileTypes: true });
            const spanParent = e.target.parentNode; // Gets play button parent
            const spanText = spanParent.children[0].innerText; // Access parent first child, and gets text inside span

            // Filters the audio files to ignore all subfolders, this leaves the array with only .wav files
            let filesNames = audioFiles
               .filter(dirent => dirent.isFile())
               .map(dirent => dirent.name);

            // Checks if .wav file exists, then tell in which position it is on the filesNames array
            // USED IN BUILD
            if (fs.existsSync(`${path.join(__dirname, '..', '..', '..', 'SND', folderName, 'wav', spanText)}`)) {
               // USED IN DEVELOPMENT STAGE
               // if (fs.existsSync(`${path.join('SND', folderName, 'wav', spanText)}`)) {
               for (let x = 0; x != filesNames.length; x++) {
                  if (filesNames[x] == spanText) {

                     // Converts to .vag
                     convertToVag(filesNames[x], filesNames[x]);
                     break;
                  }
               }
            } else {
               ipcRenderer.send("wavConvertError");
            }

            // Spawns WAV elements on right side of the screen
            setTimeout(() => {
               checkWavFolder();
               checkVagFolder();
            }, 1400);
         }
      }

   })

   // Export all audios as .vag
   exportAllBtn.addEventListener("click", function () {
      exportAllAudios();
   })

   // Scan for new WAV files
   scanBtnEl.addEventListener("click", function () {
      checkWavFolder();
   })

   // Scan for new VAG files
   scanVagBtnEl.addEventListener("click", function () {
      checkVagFolder();
   })

   /* ==========================================
       NEW FUNCIONALITY: 
      ========================================== */

   // Save all modified buffer back to file
   saveBtn.addEventListener("click", () => {
      let COMPLETE_BUFFER = Buffer.concat([buffer_initial, buffer_VAG, buffer_audios])
      headerFileSize.value = COMPLETE_BUFFER.length + " bytes";
      fs.writeFileSync(filepath, COMPLETE_BUFFER);
      var saveMessage = document.querySelector(".hide");
      saveMessage.style.display = "block"
      setTimeout(() => {
         saveMessage.style.display = "none"
      }, 2000);
   })

   closeBtn.addEventListener("click", () => {
      ipcRenderer.send("closeSNDfile");
      fs.closeSync(fd);
   })

   ipcRenderer.on("saveAsSNDfileContent", (e, arg) => {
      let COMPLETE_BUFFER = Buffer.concat([buffer_initial, buffer_VAG, buffer_audios]);
      fs.writeFileSync(arg, COMPLETE_BUFFER);
   })
})
