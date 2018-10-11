var project = {};
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

project.showDialog = function (title, text, buttonTextArray, buttonCallbackArray, data, treatAsDom) {
    //Data can be whatever you want to pass into the callbacks.
    //Set text first.
    document.getElementById('popup_title').innerText = title;
    if (treatAsDom == null) { treatAsDom = false; }
    if (treatAsDom) {
        //Copy dom elements to this.
        document.getElementById('popup_text').innerHTML = "";
        document.getElementById('popup_text').appendChild(text);
    } else {
        //Treat as normal HTML.
        document.getElementById('popup_text').innerHTML = text;
    }
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

project.init = function () {
    //Get assets from server.
    project.serverRequest("media_list/", function (data) {
        //Set assets and add tabs.
        project.assets = {};
        for (var i = 0; i < data.length; i += 1) {
            var d = data[i];
            project.addExistingFileToSidebar(d);
        }
    }, null, true);
}

project.addExistingFileToSidebar = function (d) {
    if (d.type == 0 || d.type == 1) {
        var name = d.filename.split('/');
        name = name[name.length - 1];
        //Add this to the sidebar.
        var tab = sidebarmanager.addButton(name, d.type + 1, false, function (idd) {
            //Check if this file is ready for reading yet.
            //We can use the id we got passed to get our data.
            var dd = filemanager.loadedFiles[idd];
            return dd.loaded;
        }, function () {
            //Show the save/cancel dialog.
        }, null, d.id, false);
        //Add a loading symbol to the tab.
        tab.tab_ele.firstChild.innerHTML = name + "<img src=\"https://romanport.com/static/icons/loader.svg\" height=\"18\" style=\"vertical-align: top; margin-left: 8px;\">";
        //Add to list of filemanager.loadedFiles
        d.shortName = name;
        d.tab = tab;
        d.session = d.tab.edit_session;
        d.saved = true;
        d.loaded = false;
        filemanager.loadedFiles[d.id] = d;
        //Start the loading process for this file.
        project.serverRequest("media/" + d.id + "/get/application_json", function (file_data) {
            //Set the contents of the session.
            var dd = filemanager.loadedFiles[file_data.id];
            dd.save_url = file_data.save_url;
            dd.loaded = true;
            //Update the IDE.
            dd.session.setMode("ace/mode/" + file_data.type);
            dd.session.setValue(file_data.content);
            //Add an event listener to the IDE to update this when it is edited.
            dd.session.on("change", function () {
                //Not exactly sure how the scope works in this lamda function. We'll hope it is correct.
                dd.saved = false;
                //Add the little star to the filename.
                dd.tab.tab_ele.firstChild.innerText = dd.shortName + "*";
            });
            //Hide the loader symbol.
            dd.tab.tab_ele.firstChild.innerText = dd.shortName;
        }, null, true);
    }
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

project.displayForm = function (name, options, confirmAction, cancelAction) {
    //Create the dialog HTML.
    var dialogHtml = document.createElement('div');
    //Loop through each option and include it.
    for (var i = 0; i < options.length; i += 1) {
        var d = options[i];
        var nameHtml = document.createElement('div');
        var formHtml = document.createElement('div');
        //Add the title.
        var titleEle = document.createElement('div');
        titleEle.className = "formTitle";
        titleEle.innerText = d.title;
        nameHtml.appendChild(titleEle);
        //Create the form action based on the action type.
        var formEle = null;
        if (d.type == "text") {
            formEle = document.createElement('input');
            formEle.type = "text";
            //Add change event listener if asked.
            if (d.onChange != null) {
                formEle.addEventListener('input', d.onChange);
            }
        }
        if (d.type == "select") {
            var inner = document.createElement('select');
            //Add options.
            for (var ii = 0; ii < d.options.length; ii += 1) {
                var dd = d.options[ii];
                var ele = document.createElement('option');
                ele.value = dd.value;
                ele.innerText = dd.title;
                inner.appendChild(ele);
            }
            //Add change event listerer if needed
            if (d.onChange != null) {
                inner.addEventListener('change', d.onChange);
            }
            //Move this into an inner div.
            formEle = document.createElement('div');
            formEle.className = "selectFix ";
            formEle.appendChild(inner);
        }
        //If it's null, complain.
        if (formEle == null) {
            console.log("Unknown type - " + d);
            continue;
        } 
        //Fill out the rest of the data and append.
        formEle.className += "formItem";
        formEle.id = "formele_id_" + i;
        formHtml.appendChild(formEle);
        //Append all
        nameHtml.className = "formCol";
        formHtml.className = "formCol";
        dialogHtml.appendChild(nameHtml);
        dialogHtml.appendChild(formHtml);
    }
    //Now, create the dialog.
    project.showDialog(name, dialogHtml, ["Create", "Cancel"], [
        function () {
            //Gather the results.
            var results = [];
            for (var i = 0; i < options.length; i += 1) {
                results.push(document.getElementById('formele_id_' + i).value);
            }
            //Call the callback.
            confirmAction(results);
        },
        function () {
            cancelAction();
        },
    ],true);
}

project.showAddAssetDialog = function () {
    /*project.showDialog("Add File", "Source Type<select style=\"padding:8px;\"><option value=\"c\">C File</option><option value=\"c_worker\">C Worker File</option></select><br>File Name<input style=\"margin-left:10px; padding:8px;\" id=\"form_filename\" type=\"text\">", ["Create", "Cancel"], [
        function () {
            var url = "create_empty_media/?filename=" + encodeURIComponent(document.getElementById('form_filename').value) + "&major_type=src&minor_type=c";
            project.serverRequest(url, function (data) {
                project.addExistingFileToSidebar(data);
            }, null, true);
        },
        function () { },
    ]);*/
    var onFilenameEdit = function () {
        //Verify if this ends correctly.....later.
    }

    var onTypeChange = function () {
        //Hide elements depending on the type.
        console.log(this.value);
    }

    project.displayForm("Add File", [
        { "title": "Source Type", "type": "text", "onChange": onFilenameEdit },
        { "title": "Filename", "type": "select", "onChange": onTypeChange, "options": [{ "title": "C File", "value": "c" }, { "title": "JS File", "value": "js" }, { "title": "C Worker File", "value": "c_worker" }, { "title": "Window Layout File", "value": "layout" }] }
    ], function (data) {
        console.log(data);
    }, function () {

    });
}