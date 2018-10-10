﻿var project = {};
project.id = "2ca12e56dd024781";
project.serverRequest = function (url, run_callback, fail_callback, isJson, type, body, timeout) {
    url = "http://10.0.1.52/project/" + project.id + "/" + url;
    //This is the main server request function. Please change all other ones to use this.
    if (isJson == null) { isJson = true; }

    if (type == null) { type = "GET"; }

    if (timeout == null) { timeout = 10000; }

    if (fail_callback == null) {
        fail_callback = function () {
            project.showDialog("Error", "You aren't online, or the server had an issue. Try again later.", ["Close"], [function () { }]);
        }
    }
    var xmlhttp = new XMLHttpRequest();

    xmlhttp.timeout = timeout;

    xmlhttp.onreadystatechange = function () {
        if (this.readyState == 4 && this.status == 200) {
            if (isJson) {
                //This is JSON.
                //This is most likely to be valid, but check for errors.
                var JSON_Data;
                try {
                    JSON_Data = JSON.parse(this.responseText);
                } catch (e) {
                    fail_callback("JSON Parse Error", true);
                    return;
                }
                if (JSON_Data["maintenance"] != null) {
                    if (JSON_Data["maintenance"] == true) {
                        //Stop and show maintance mode message.
                        DisplayApiMaintenanceMsg(JSON_Data["maintenance_msg"]);
                    }

                }
                if (JSON_Data.error != null) {
                    //Server-side error!
                    fail_callback(JSON_Data.error + " - Check Console", true);
                    console.log("A server error (" + JSON_Data.error + ") occurred and data could not be grabbed. Error: " + JSON_Data.raw_error);
                    return;
                } else {
                    //Aye okay here. Call the callback.
                    run_callback(JSON_Data);
                    return;
                }
            } else {
                //Just return it
                run_callback(this.responseText);
            }
        } else if (this.readyState == 4) {
            //Got an invalid request.
            fail_callback("HTTP Error " + this.status, true);
        }
    }

    xmlhttp.ontimeout = function () {
        fail_callback("No Connection", false);
    }

    xmlhttp.onerror = function () {
        fail_callback("No Connection", false);
    }

    xmlhttp.onabort = function () {
        fail_callback("Abort", false);
    }
    //Todo: Add timeout error.
    xmlhttp.open(type, url, true);
    xmlhttp.setRequestHeader("Authorization", Cookies.get('access-token'));
    xmlhttp.send(body);
};

project.showDialog = function (title, text, buttonTextArray, buttonCallbackArray, data) {
    //Data can be whatever you want to pass into the callbacks.
    //Set text first.
    document.getElementById('popup_title').innerText = title;
    document.getElementById('popup_text').innerText = text;
    //Erase old buttons now.
    document.getElementById('popup_btns').innerHTML = "";
    //Add new buttons.
    for (var i = 0; i < buttonTextArray.length; i += 1) {
        var b_text = buttonTextArray[i];
        var b_callback = buttonCallbackArray[i];
        var e = document.createElement('div');
        e.innerText = b_text;
        e.x_callback = b_callback;
        e.x_callback_data = data;
        e.addEventListener('click', function () {
            //Hide
            project.hideDialog();
            //Call
            this.x_callback(this.x_callback_data, this);
        });
        document.getElementById('popup_btns').appendChild(e);
    }
    //Show.
    document.getElementById('popup_window_bg').className = "popup_background";
    document.getElementById('popup_window').className = "popup_window_container";
}

project.hideDialog = function () {
    document.getElementById('popup_window_bg').className = "popup_background hide";
    document.getElementById('popup_window').className = "popup_window_container hide";
}

project.assets = [];

project.init = function () {
    //Get assets from server.
    project.serverRequest("media_list/", function (data) {
        //Set assets and add tabs.
        project.assets = data;
        for (var i = 0; i < data.length; i += 1) {
            var d = data[i];
            if (d.type == 0 || d.type == 1) {
                var e = document.createElement('div');
                var ee = document.createElement('div');
                ee.className = "tab btn";
                var name = d.filename.split('/');
                name = name[name.length - 1];
                ee.innerText = name;
                e.appendChild(ee);
                //Set data on dom.
                e.x_asset = d;
                e.x_asset.shortName = name;
                //Add the event listener to view this.
                e.addEventListener('click', function () {
                    var asset = this.x_asset;
                    filemanager.LoadFile(asset.id, asset.shortName);
                });
                //Append to the list.
                var list = document.getElementById('assets_' + d.type);
                list.insertBefore(e, list.firstChild);
            }
        }
    }, null, true);
}

project.initKeybinds = function () {
    //thanks to https://stackoverflow.com/questions/93695/best-cross-browser-method-to-capture-ctrls-with-jquery
    $(window).bind('keydown', function (event) {
        if (event.ctrlKey || event.metaKey) {
            switch (String.fromCharCode(event.which).toLowerCase()) {
                case 's':
                    event.preventDefault();
                    //Control + S. Save.
                    filemanager.SaveAll(function () {
                        //Hide the dialog after a moment.
                        window.setTimeout(function () {
                            project.hideDialog();
                        }, 2000);
                    })
                    break;
            }
        }
    });
}

project.buildPbwBtn = function () {
    //Save first.
    filemanager.SaveAll(function () {
        //Show message because this could take a while.
        project.showDialog("Building PBW...", "This could take a while.", [], []);
        project.serverRequest("build/", function (data) {
            //Check if the build crashed.
            if (data.passed) {
                //OK.
                project.showDialog("Build Finished", "The build finished successfully.", ["Dismiss", "Get PBW", "View Log"], [function () { },
                function () { },
                function () {
                    //Set the template and apply it. This is a bit ugly.
                    var t = document.getElementById('template_bigtext');
                    t.value = data.log;
                    tabManager.addDomTab("Build Log " + data.id, t, null);
                }]);
            } else {
                //Failed.
                project.showDialog("Build Failed", "The build failed to compile. Take a look at the log to see what went wrong.", ["Dismiss", "View Log"], [function () { },
                function () {
                    //Set the template and apply it. This is a bit ugly.
                    var t = document.getElementById('template_bigtext');
                    t.value = data.log;
                    tabManager.addDomTab("Build Log " + data.id, t, null);
                }
                ]);
            }
        }, function () {
            //Darn. Something went wrong.
            project.showDialog("Failed", "There was an internal server error, or you are offline.", ["Close"], [function () { }]);
        }, true, "GET", null, 60 * 1000);
    });

}