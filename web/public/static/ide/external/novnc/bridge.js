//Bridge between the modified NoVNC library and normal JS because I have no idea how to use modules and really don't want to do so.

import RFB from '/static/ide/external/novnc/core/rfb.js';

qemu.createConnection = function(node, url, data) {
    qemu.rfb = new RFB(node, url, data);
    qemu.rfb.scaleViewport = false;
};



//rfb.addEventListener("connect",  connectedToServer);
//rfb.addEventListener("disconnect", disconnectedFromServer);