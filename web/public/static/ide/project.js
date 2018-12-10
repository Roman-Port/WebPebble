﻿var project = {};
project.settings = {};
project.appInfo = {};
project.started = false;

project.id = window.location.pathname.split('/')[2];


project.serverRequest = function (url, run_callback, fail_callback, isJson, type, body, timeout) {
    url = "https://api.webpebble.get-rpws.com/project/" + project.id + "/" + url;
    project.anyServerRequest(url, run_callback, fail_callback, isJson, type, body, timeout);
};

project.anyServerRequest = function (url, run_callback, fail_callback, isJson, type, body, timeout) {
    //This is the main server request function. Please change all other ones to use this.
    if (isJson == null) { isJson = true; }

    if (type == null) { type = "GET"; }

    if (timeout == null) { timeout = 10000; }

    if (fail_callback == null) {
        fail_callback = function () {
            project.showDialog("Error", "Failed to connect. Please check your internet and try again later.", ["Retry"], [function () { project.anyServerRequest(url, run_callback, fail_callback, isJson, type, body, timeout);  }]);
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
                //Return it
                run_callback(JSON_Data);
            } else {
                //Just return it
                run_callback(this.responseText);
            }
        } else if (this.readyState == 4) {
            //Parse the response and display the error
            var errorData = JSON.parse(this.responseText);
            if(errorData.code == 0) {
                //This user isn't logged in. Redirect to the login screen.
                window.location = "/login";
            }
            else if(errorData.code == 1) {
                //Not found or bad owner.
                project.showDialog("Error", "This project couldn't be found, or you don't own it.", ["Quit"], [function () { window.location = "/"; }]);
            }
            else {
                //Unknown error.
                project.showDialog("Unknown Server Error", errorData.message, ["Reload"], [function () { window.location.reload(); }]);
            }
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
    xmlhttp.withCredentials = true;
    xmlhttp.send(body);
};

project.showLoader = function (title) {
    project.showDialog(title, '<div class="inf_loader"></div>', [], [], null, false);
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
    //Get the user info
    project.anyServerRequest("https://api.webpebble.get-rpws.com/users/@me/", function(userdata) {
        user.settings = userdata.settings;
        document.getElementById('theme_style').href = "/static/ide/themes/" + user.settings.theme;
        //Get assets from server.
        project.serverRequest("media_list/", function (data) {
            //Set assets and add tabs.
            project.assets = {};
            for (var i = 0; i < data.length; i += 1) {
                var d = data[i];
                //Add only source files right now.
                if (d.type == 0) {
                    project.addExistingFileToSidebar(d);
                }
                if (d.type == 1) {
                    project.addResourceToSidebar(d);
                }
            }
            //Fetch project data.
            project.serverRequest("settings/", function (sett) {
                project.settings = sett;
                var n = document.getElementById('project_name');
                n.innerText = sett.name;
                n.className = "btn btn_active";
                //Fetch appinfo.json
                project.refreshAppInfo(function () {
                    //Log into the websocket.
                    phoneconn.init(function () {
                        project.started = false;
                        //Allow the user to use this.
                        ycmd.subscribe();
                        project.hideDialog();
                        document.getElementById('fullscreen_loader').parentNode.removeChild(document.getElementById('fullscreen_loader'));
                        project.forceHideBottomModalNoArgs();
                    });
                });
            }, null, true);
        }, null, true);
    }, null, true, "GET");
    
};

project.refreshAppInfo = function (callback) {
    project.serverRequest("appinfo.json", function (app) {
        project.appInfo = app;
        callback(app);

    }, null, true);
};

project.promptDeleteProject = function() {
    project.showDialog("Delete Project?", "Once you delete a project, there is no going back. Are you sure you would like to delete this project?", ["Delete", "Cancel"], [function() {
        project.showLoader("Removing Project...");
        project.serverRequest("delete_project/?c="+project.id, function() {
            window.location = "/me/";
        }, function() {
            project.showDialog("Failed to delete.", [], []);
        }, true);
    }, function(){}]);
};

project.zipProject = function() {
    project.showLoader("Creating ZIP Archive...");
    project.serverRequest("zip.zip", function(zip) {
        project.hideDialog();
        filemanager.DownloadUrl("data:application/zip;base64,"+zip);
    }, function() {
        project.showDialog("Failed to create ZIP.", [], []);
    }, false);
}

project.addExistingFileToSidebar = function (d) {
    if (d.type == 0) {
        var name = d.nickname;
        var actionsHtml = "";
        if (d.type == 0) {
            //This file is deletable from the sidebar. Add the HTML for that.
            actionsHtml = '<div class="action_window"><div onclick="filemanager.PromptDeleteFile(filemanager.loadedFiles[this.parentElement.parentElement.parentElement.x_id]);"><img src="https://romanport.com/static/icons/white/baseline-delete.svg"></div></div>';
        }
        //Add this to the sidebar.
        var tab = sidebarmanager.addButton(name, d.type + 1, false, function (idd) {
            //Check if this file is ready for reading yet.
            //We can use the id we got passed to get our data.
            var dd = filemanager.loadedFiles[idd];
            return dd.loaded;
        }, function () {
            //Show the save/cancel dialog.
        }, null, d.id, false, actionsHtml, null, function() {
            console.log("renamed");
        });
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
        project.serverRequest("media/" + d.id + "/?mime=text%2Fplain", function (file_data) {
            //Set the contents of the session.
            var dd = filemanager.loadedFiles[d.id];
            dd.save_url = "media/"+dd.id+"/";
            dd.loaded = true;
            //Update the IDE.
            dd.session.setMode("ace/mode/" + "c");
            dd.session.setValue(file_data);
            //Add an event listener to the IDE to update this when it is edited.
            dd.session.on("change", function () {
                //Not exactly sure how the scope works in this lamda function. We'll hope it is correct.
                dd.saved = false;
                //Add the little star to the filename.
                sidebarmanager.updateSuffixOfTab(dd.tab,"*");
            });
            //Hide the loader symbol.
            sidebarmanager.updateSuffixOfTab(dd.tab);
        }, null, false);
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
                        project.hideDialog();
                    })
                    break;
            }
        }
    });
}

project.displayLog = function (content, id) {
    //Display tab line.
    document.getElementById('sidebar_sec_3_line').style.display = "block";
    //Display tab
    var tab = sidebarmanager.addButton("Build Log " + id, 3, false, function () { }, function () { }, null, "build_log_id" + id, true, "");
    tab.edit_session.setValue(content);
    tab.edit_session.setMode("ace/mode/text");
}

project.buildPbwBtn = function () {
    //Save first.
    filemanager.SaveAll(function () {
        //Show message because this could take a while.
        project.showDialog("Building PBW...", "<div class=\"inf_loader\"></div>", [], []);
        project.serverRequest("build/", function (data) {
            //Check if the build crashed.
            if (data.passed) {
                //OK.
                //If the Pebble is connected, install this file onto it. Else, show the dialog
                if (phoneconn.deviceConnected) {
                    //Install on phone.
                    phoneconn.installApp(location.protocol + "//" + location.hostname + "/project/" + project.id + "/pbw_media/" + data.id + "/" + project.id + "_build_" + data.id + ".pbw", data);
                } else {
                    project.showDialog("Build Finished", "The build finished successfully.", ["Dismiss", "Get PBW", "View Log"], [function () { },
                    function () {
                        filemanager.DownloadUrl("/project/" + project.id + "/pbw_media/" + data.id + "/" + project.id + "_build_" + data.id + ".pbw");
                    },
                    function () {
                        project.displayLog(data.log, data.id);
                    }]);
                }
                
            } else {
                //Failed.
                project.showDialog("Build Failed", "The build failed to compile. Take a look at the log to see what went wrong.", ["Dismiss", "View Log"], [function () { },
                function () {
                    project.displayLog(data.log, data.id);
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
            formEle.x_refer = formEle.firstChild;
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
                var ele = document.getElementById('formele_id_' + i);
                if (ele.x_refer != null) {
                    ele = ele.x_refer;
                }
                results.push(ele.value);
            }
            //Call the callback.
            confirmAction(results);
        },
        function () {
            cancelAction();
        },
    ],null,true);
}

project.forceCreateAssetMedia = function(callback, majorType, minorType, name, filename, template) {
    //Generate payload.
    payload = {
        "type":majorType,
        "sub_type":minorType,
        "name":name,
        "filename":filename,
        "template":template
    };
    project.serverRequest("media/create/", function(itemData) {
        //Call callback
        callback(itemData);
        //Add to sidebar
        project.addExistingFileToSidebar(itemData);
    }, null, true, "POST", JSON.stringify(payload));
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

        if(this.value == "js") {
            document.getElementById('popup_text').childNodes[2].style.display = "none";
            document.getElementById('popup_text').childNodes[3].style.display = "none";
        } else {
            document.getElementById('popup_text').childNodes[2].style.display = "inline-block";
            document.getElementById('popup_text').childNodes[3].style.display = "inline-block";
        }
    }

    project.displayForm("Add File", [
        { "title": "Source Type", "type": "select", "onChange": onTypeChange, "options": [{ "title": "C File", "value": "c" }, { "title": "JS File", "value": "js" }, { "title": "C Worker File", "value": "c_worker" }, { "title": "Window Layout File", "value": "layout" }] },
        { "title": "Filename", "type": "text", "onChange": onFilenameEdit }
        
    ], function (data) {
        //Decide what to do.
        var type = data[0];
        var name = data[1];
        if (type == "c") {
            project.forceCreateAssetMedia(function(){}, 0, 0, name, name, "blank");
        }
        if(type == "js") {
            //Check to see if we alrady have a js file
            var keys = Object.keys(filemanager.loadedFiles)
            for(var i = 0; i<keys.length; i+=1) {
                var da = filemanager.loadedFiles[keys[i]];
                if(da.innerType == 4) {
                    //Jump to this file on the sidebar

                    //Deactive old items
                    if (sidebarmanager.activeItem != null) {
                        sidebarmanager.hide_content(sidebarmanager.activeItem);
                    }

                    //Switch to this
                    sidebarmanager.show_content(sidebarmanager.items[da.internalId]);
                }
            }
            //Create a new file
            project.forceCreateAssetMedia(function(){}, 0, 4, "index.js", "index.js", "pkjs");
        }
    }, function () {

    });
}

project.copyToSettingsView = function () {
    //Copy the values to the settings view.
    var appInfo = project.appInfo;
    document.getElementById('settings_entry_sdk_version').value = appInfo.pebble.sdkVersion;
    document.getElementById('settings_entry_kind').value = "watchapp";
    if (appInfo.pebble.watchapp.watchface) { document.getElementById('settings_entry_kind').value = "watchface"; }
    document.getElementById('settings_entry_short_name').value = appInfo.pebble.shortName;
    document.getElementById('settings_entry_long_name').value = appInfo.pebble.longName;
    document.getElementById('settings_entry_dev_name').value = appInfo.pebble.companyName;
    document.getElementById('settings_entry_version').value = appInfo.pebble.versionLabel;
    document.getElementById('settings_entry_uuid').value = appInfo.pebble.uuid;
}

project.mediaResourcesFiles = {};

project.addResourceToSidebar = function (data) {
    //Add the tab for this.
    project.mediaResourcesFiles[data.id] = data;
    var tab = sidebarmanager.addButton(data.nickname, 2, false, function (context) {
        
    }, function () {
        //Show the save/cancel dialog.
        }, document.getElementById('template_add_resource'), data.id, false, "", function (context) {
            //Show the edit menu.
            edit_resource.onSelectExisting(context);
        }
    );
    //Set the data on the DOM.
    tab.tab_ele.x_data = data;
}

project.bottomModalQueue = [];
project.bottomModalActive = false;

project.showBottomModal = function(text, callback, className) {
    var i = {};
    i.type = 0;
    i.callback = callback;
    i.text = text;
    i.className = className;
	//If it's already active, push it the the queue.
	if(project.bottomModalActive) {
		project.bottomModalQueue.unshift(i);
	} else {
		//Display now.
		project.forceShowBottomModal(i);
	}
};

project.showLoaderBottom = function(text, onBeginLoad, className, onDismissCallback) {
    var i = {};
    i.type = 1;
    i.callback = onDismissCallback;
    i.onBeginLoad = onBeginLoad;
    i.text = text;
    i.className = className;
	//If it's already active, push it the the queue.
	if(project.bottomModalActive) {
		project.bottomModalQueue.unshift(i);
	} else {
		//Display now.
		project.forceShowBottomModal(i);
	}
}

project.reportError = function(text) {
	project.showBottomModal(text, null, "bottom_modal_error");
}

project.reportDone = function(text) {
    project.showBottomModal(text, null, "bottom_modal_good");
}

project.forceHideBottomModalNoArgs = function() {
    project.forceHideBottomModal({
        "callback":null,
        "className":""
    });
}

project.forceHideBottomModal = function(request) {
    var node = document.getElementById('bottom_modal');
    //Hide.
    node.className = "bottom_modal open_sans "+request.className;
    window.setTimeout(function() {
        //Call callback
        if(request.callback != null) {
            request.callback();
        }
        //Completely hide the classname
        node.className = "bottom_modal open_sans ";
        //Toggle flag
        project.bottomModalActive = false;
        //If there is an item in the queue, show it.
        if(project.bottomModalQueue.length >= 1) {
            var o = project.bottomModalQueue.pop();
            project.forceShowBottomModal(o);
        }
    }, 300);
}

project.forceShowBottomModal = function(request) {
	var node = document.getElementById('bottom_modal');
	node.innerHTML = request.text;
	node.className = "bottom_modal bottom_modal_active open_sans "+request.className;
    project.bottomModalActive = true;
    
    //Called when it is time to dismiss this.
    var onDoneShowCallback = function() {
        project.forceHideBottomModal(request);
    };

	if(request.type == 0) {
        //Standard wait. 
        window.setTimeout(onDoneShowCallback, 300 + ((request.text.length / 6) * 1000));
    } else if(request.type == 1) {
        //Record current time. This'll be used when we come back.
        var startTime = new Date().getTime();
        //Load callback. Call the callback and expect a ping back shortly.
        request.onBeginLoad(function() {
            //Check if we've elapsed the time required to show the modal.
            var remainingTime = 300 - (new Date().getTime() - startTime);
            if(remainingTime > 0) {
                //Wait.
                window.setTimeout(onDoneShowCallback, remainingTime);
            } else {
                //Do it now. 
                onDoneShowCallback();
            }
        });
        
    }
}