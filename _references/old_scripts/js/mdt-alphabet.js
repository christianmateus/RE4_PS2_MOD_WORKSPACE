exports.Alphabet = {
    128: ' ',
    129: '►',
    130: '▼',
    131: '0',
    132: '1',
    133: '2',
    134: '3',
    135: '4',
    136: '5',
    137: '6',
    138: '7',
    139: '8',
    140: '9',
    141: ':',
    142: '%',
    143: '&',
    144: '+',
    145: '-',
    146: '/',
    147: '=',
    148: ',',
    149: '.',
    150: '˙',
    151: '…',
    152: '(',
    153: ')',
    154: '!',
    155: '?',
    156: '“',
    157: '”',
    158: '~',
    159: '☼',
    160: '▬',
    161: '<',
    162: '>',
    163: '[',
    164: ']',
    165: '①',
    166: '②',
    167: 'A',
    168: 'B',
    169: 'C',
    170: 'D',
    171: 'E',
    172: 'F',
    173: 'G',
    174: 'H',
    175: 'I',
    176: 'J',
    177: 'K',
    178: 'L',
    179: 'M',
    180: 'N',
    181: 'O',
    182: 'P',
    183: 'Q',
    184: 'R',
    185: 'S',
    186: 'T',
    187: 'U',
    188: 'V',
    189: 'W',
    190: 'X',
    191: 'Y',
    192: 'Z',
    193: 'a',
    194: 'b',
    195: 'c',
    196: 'd',
    197: 'e',
    198: 'f',
    199: 'g',
    200: 'h',
    201: 'i',
    202: 'j',
    203: 'k',
    204: 'l',
    205: 'm',
    206: 'n',
    207: 'o',
    208: 'p',
    209: 'q',
    210: 'r',
    211: 's',
    212: 't',
    213: 'u',
    214: 'v',
    215: 'w',
    216: 'x',
    217: 'y',
    218: 'z',

    219: 'â',
    220: 'ê',
    221: 'î',
    222: 'ô',
    223: 'û',

    224: 'Â',
    225: 'Ê',
    226: 'Î',
    227: 'Ô',
    228: 'Û',

    229: 'à',
    230: 'è',
    231: 'ì',
    232: 'ò',
    233: 'ù',

    234: 'À',
    235: 'È',
    236: 'Ì',
    237: 'Ò',
    238: 'Ù',

    239: 'á',
    240: 'é',
    241: 'í',
    242: 'ó',
    243: 'ú',
    244: 'ý',

    245: 'Á',
    246: 'É',
    247: 'Í',
    248: 'Ó',
    249: 'Ú',
    250: 'Ý',

    251: 'ä',
    252: 'ë',
    253: 'ï',
    254: 'ö',
    255: 'ü',
    256: 'ÿ',

    257: 'Ä',
    258: 'Ë',
    259: 'Ï',
    260: 'Ö',
    261: 'Ü',
    262: 'Ÿ',

    263: 'ã',
    264: 'õ',
    265: 'Ã',
    266: 'Õ',

    267: 'ñ',
    268: 'Ñ',

    269: 'å',
    270: 'Å',
    271: 'ç',
    272: 'Ç',
    273: 'ø',
    274: 'Ø',
    275: 'Þ',
    276: 'þ',

    277: 'š',
    278: 'Š',

    279: 'ß',
    280: 'Ð',
    281: 'ƒ',
    282: 'µ',

    283: 'ð',
    284: 'æ',
    285: 'œ',
    286: 'Æ',
    287: 'Œ',

    // Symbols
    288: '°',
    289: '¡',
    290: '¿',
    291: "'",
    292: '™',
    293: ';',
    294: '#',
    295: '@',
    296: '→',
    297: '←',
    298: '↑',
    299: '↓',
    300: '{ptas}',
    301: '"',
    302: '„',
    303: '®',
    304: '{X}',
    305: '{SQR}',
    306: '{O}',
    307: '{TRI}',
    308: '*',
    309: 'x',
    310: '{R1}',
    311: '{R2}',
    312: '{L1}',

}

exports.StringCodes = {
    0: "[message-start]",
    1: "[message-end]",
    2: "[message-change]", // Insert another string from the room's MDT
    3: "[line-break]",
    4: "[new-page]",
    5: "[print-speed]", // Prints each character one character at a time
    6: "[color]", // Changes the color of all text that comes after this point
    7: "[option]",
    8: "[pause]",
    9: "[sleep]", // Sleep for a specific duration before continuing
    10: "[item-QT]",
    11: "[align-left]", // Distance from the left side of the screen where the message will appear
    12: "[align-top]", // Distance from the top of the screen where the message will appear
    13: "[unknown]",
    14: "[return]",
    15: "[core-6]", // Inserts a string from core_6.mdt
    16: "[item-ID]",
    17: "[core-17]", // Inserts a string from core_17.mdt
    18: "[character]" // Inserts a character name, based on index

    // List found by Zatarita
    // * Every commented value has a modifier (another short followed by it)
}

/* ========== Removing focus from button elements ========== */
let messageStartBtn = document.getElementById("messageStartBtn");
let messageEndBtn = document.getElementById("messageEndBtn");
let lineBreakBtn = document.getElementById("lineBreakBtn");
let newPageBtn = document.getElementById("newPageBtn");
let optionBtn = document.getElementById("optionBtn");
let pauseBtn = document.getElementById("pauseBtn");
let itemQtBtn = document.getElementById("itemQtBtn");
let itemIdBtn = document.getElementById("itemIdBtn");

messageStartBtn.addEventListener("focus", function (e) {
    if (e.relatedTarget) {
        e.relatedTarget.focus();
    } else {
        e.currentTarget.focus();
    }
})
messageEndBtn.addEventListener("focus", function (e) {
    if (e.relatedTarget) {
        e.relatedTarget.focus();
    } else {
        e.currentTarget.focus();
    }
})
lineBreakBtn.addEventListener("focus", function (e) {
    if (e.relatedTarget) {
        e.relatedTarget.focus();
    } else {
        e.currentTarget.focus();
    }
})
newPageBtn.addEventListener("focus", function (e) {
    if (e.relatedTarget) {
        e.relatedTarget.focus();
    } else {
        e.currentTarget.focus();
    }
})
optionBtn.addEventListener("focus", function (e) {
    if (e.relatedTarget) {
        e.relatedTarget.focus();
    } else {
        e.currentTarget.focus();
    }
})
pauseBtn.addEventListener("focus", function (e) {
    if (e.relatedTarget) {
        e.relatedTarget.focus();
    } else {
        e.currentTarget.focus();
    }
})