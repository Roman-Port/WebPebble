var phoneconn = {};

phoneconn.url = "ws://cloudpebble-developer-proxy.get-rpws.com:43187/webpebble";
phoneconn.ws = null;
phoneconn.currentId = 1;
phoneconn.callbacks = {};

phoneconn.init = function (callback) {
    //Create a WebSocket connection.
    console.log("Connecting to " + phoneconn.url);
    phoneconn.ws = new WebSocket(phoneconn.url);
    //Establish events.
    phoneconn.ws.addEventListener('message', phoneconn.onMessage);
    //Connect and do auth.
    phoneconn.ws.addEventListener('open', function (event) {
        //We've connected. Do first time authoriation.
        var token = Cookies.get("access-token");
        phoneconn.send(1, { "token": token }, function (data) {
            console.log("Got auth data:");
            console.log(data);
        });
    });
}

phoneconn.onMessage = function (event) {
    var data = JSON.parse(event.data);
    //Debug log
    console.log(data);
    //If the ID of this is -1, this is a event. 
    if (data.messageid == -1) {
        console.log("TODO event");
    } else {
        //Find the target callback for this.
        var target = phoneconn.callbacks[data.messageid];
        target(data);
        //Remove this.
        delete phoneconn.callbacks[data.messageid];
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
    buf.messageid = id;
    //Send on the websocket.
    phoneconn.ws.send(JSON.stringify(buf));
}