﻿var phoneconn = {};

phoneconn.url = "ws://cloudpebble-developer-proxy.get-rpws.com:43187/webpebble";
phoneconn.ws = null;
phoneconn.currentId = 1;
phoneconn.callbacks = {};

phoneconn.authorized = false;
phoneconn.deviceConnected = false;

phoneconn.pebbleProtocolMsgBuffer = []; //This could get quite large.

phoneconn.init = function (callback) {
    //Create a WebSocket connection.
    console.log("Connecting to " + phoneconn.url);
    phoneconn.ws = new WebSocket(phoneconn.url);
    //Establish events.
    phoneconn.ws.addEventListener('message', phoneconn.onMessage);
    phoneconn.ws.addEventListener('close', phoneconn.onClose);
    phoneconn.ws.addEventListener('error', phoneconn.onClose);
    //Connect and do auth.
    phoneconn.ws.addEventListener('open', function (event) {
        //We've connected. Do first time authoriation.
        var token = Cookies.get("access-token");
        phoneconn.send(1, { "token": token }, function (data) {
            console.log("Got auth data:");
            console.log(data);
            if (data.data["ok"] == "true") {
                //Logged in OK
                phoneconn.authorized = true;
                callback();
            } else {
                //Failed. Tell the user.
                project.showDialog("Failed to Authenticate", 'Failed to authenticate with the WebSocket connection. Try reloading, or log in again.', [], [], null, false);
            }
        });
    });
}

phoneconn.onClose = function (event) {
    phoneconn.authorized = false;
    project.showDialog("WebPebble Connection Lost", 'Connection to CloudPebble was lost. You might\'ve signed in at another location, or lost connection to the internet.', ["Reconnect"], [function () {
        project.showDialog("Reconnecting to WebPebble...", '<div class="inf_loader"></div>', [], [], null, false);
        phoneconn.init(function () {
            phoneconn.authorized = true;
            project.hideDialog();
        });
    }], null, false);
    
}

phoneconn.onMessage = function (event) {
    var data = JSON.parse(event.data);
    //Debug log
    console.log(data);
    //If the ID of this is -1, this is a event. 
    if (data.requestid == -1) {
        var target = phoneconn.eventList[data.type];
        target(data);
    } else {
        //Find the target callback for this.
        var target = phoneconn.callbacks[data.requestid];
        target(data);
        //Remove this.
        delete phoneconn.callbacks[data.requestid];
    }
}

phoneconn.send = function (type, data, callback) {
    //Generate ID
    id = phoneconn.currentId;
    phoneconn.currentId++;
    //Register the callback.
    phoneconn.callbacks[id] = callback;
    //Create the data to send.
    var buf = {};
    buf.type = type;
    buf.data = data;
    buf.requestid = id;
    //Send on the websocket.
    phoneconn.ws.send(JSON.stringify(buf));
}

//API

phoneconn.getScreenshot = function () {
    project.showDialog("Taking Screenshot...", '<div class="inf_loader"></div>', [], [], null, false);
    phoneconn.send(2, {}, function (data) {
        project.showDialog("Pebble Screenshot", '<img src="' + data.data.img_header + data.data.data + '">', ["Save", "Dismiss"], [function () {
            //Open iframe on this.
            filemanager.DownloadUrl(data.data.download_header + data.data.data);
        }, function () { }], null, false);
    });
}

phoneconn.installApp = function (pbwUrl, buildData) {
    project.showDialog("Installing on Device...", '<div class="inf_loader"></div>', [], [], null, false);
    phoneconn.send(5, { "url": pbwUrl }, function () { });
    window.setTimeout(function () {
        project.showDialog("Build Finished", "The build finished and was installed on the Pebble successfully.", ["Dismiss", "Get PBW", "View Log"], [function () { },
        function () {
            filemanager.DownloadUrl(pbwUrl);
        },
        function () {
            project.displayLog(buildData.log, buildData.id);
        }]);
    }, 1500);
}


//Events
phoneconn.onPhoneStatusMsg = function () {
    document.getElementById('watch_control_main').style.display = "block";
    document.getElementById('watch_control_warning').style.display = "none";
    phoneconn.deviceConnected = true;
}

phoneconn.onPebbleProtocolMsg = function (data) {
    //Add this to the message buffer.
    phoneconn.pebbleProtocolMsgBuffer.push(data);
}

phoneconn.eventList = {
    3: phoneconn.onPhoneStatusMsg,
    4: phoneconn.onPebbleProtocolMsg
};