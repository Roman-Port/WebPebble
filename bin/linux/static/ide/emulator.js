var emu = {};

emu.ws = null;
emu.url = "ws://cloudpebble-developer-proxy.get-rpws.com:43189/session";
emu.onLoadCallback = null;
emu.status = 0;

emu.launch = function (callback) {
    //Clear and show the emulator area.
    document.getElementById('watch_emulator_area').style.display = "block";
    emu.setStatus("Connecting...", true);
    emu.ws = new WebSocket(emu.url);
    //Establish events.
    emu.ws.addEventListener('message', emu.onMessage);
    emu.ws.addEventListener('close', emu.onClose);
    emu.ws.addEventListener('error', emu.onClose);
    //Get ready
    emu.ws.addEventListener('open', function (event) {
        //Set callback
        emu.onLoadCallback = callback;
        //Request to boot the emulator.
        emu.sendJson(1, { "platform": "basalt" });
        //Update status.
        emu.setStatus("Emulator booting...", true);
    });
};

emu.sendJson = function (type, data) {
    var j = {};
    j.type = type;
    j.data = data;
    emu.ws.send(JSON.stringify(j));
};

emu.setStatus = function (text, showLoader) {
    var area = document.getElementById('emu_loader');
    if (showLoader) {
        text = '<img style="margin-right:10px;" src="data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjZweCIgIGhlaWdodD0iMjZweCIgIHhtbG5zPSJodHRwOi8vd3d3LnczLm9yZy8yMDAwL3N2ZyIgdmlld0JveD0iMCAwIDEwMCAxMDAiIHByZXNlcnZlQXNwZWN0UmF0aW89InhNaWRZTWlkIiBjbGFzcz0ibGRzLWVjbGlwc2UiIHN0eWxlPSJiYWNrZ3JvdW5kOiBub25lOyI+PHBhdGggbmctYXR0ci1kPSJ7e2NvbmZpZy5wYXRoQ21kfX0iIG5nLWF0dHItZmlsbD0ie3tjb25maWcuY29sb3J9fSIgc3Ryb2tlPSJub25lIiBkPSJNMTAgNTBBNDAgNDAgMCAwIDAgOTAgNTBBNDAgNDYgMCAwIDEgMTAgNTAiIGZpbGw9IiNlMTViNjQiIHRyYW5zZm9ybT0icm90YXRlKDIyMS44NzYgNTAgNTMpIj48YW5pbWF0ZVRyYW5zZm9ybSBhdHRyaWJ1dGVOYW1lPSJ0cmFuc2Zvcm0iIHR5cGU9InJvdGF0ZSIgY2FsY01vZGU9ImxpbmVhciIgdmFsdWVzPSIwIDUwIDUzOzM2MCA1MCA1MyIga2V5VGltZXM9IjA7MSIgZHVyPSIxcyIgYmVnaW49IjBzIiByZXBlYXRDb3VudD0iaW5kZWZpbml0ZSI+PC9hbmltYXRlVHJhbnNmb3JtPjwvcGF0aD48L3N2Zz4=" style="padding-top:3px; padding-bottom:3px; float:left; height:24px;"><div style="float:left;">' + text + '</div>';
    }
    area.innerHTML = "<div style=\"line-height:20px;\">" + text + "</div>";
    //Make sure this is what the scroller is set to.
    document.getElementById('emu_options').style.top = "0px";
};

emu.onMessage = function (e) {
    var d = JSON.parse(e.data);
    switch (d.type) {
        case 0:
            //This is a type switch.
            console.log("QEMU switched status: " + d.data.new_status);
            emu.status = d.data.new_status;
            break;
        case 2:
            //Emulator launched.
            emu.onEmulatorLaunch(d.data);
            break;
    }
};

emu.onClose = function (e) {

};

emu.onEmulatorLaunch = function(data) {
    console.log("Emulator launched. Data:");
    console.log(data);
    //Prepare view

    //Call final callback
    ws.onLoadCallback();
    ws.onLoadCallback = null;
};