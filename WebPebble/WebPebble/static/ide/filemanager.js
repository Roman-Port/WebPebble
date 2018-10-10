var filemanager = {};

filemanager.loadedFiles = {};
filemanager.lastFile = null;

filemanager.LoadFile = function (id, name) {
    //Check if this file is already loaded.
    if (filemanager.loadedFiles[id] != null) {
        //Instead, switch to this file.
        filemanager.SwitchFile(id);
        return;
    }
    //First, add a tab.
    //Create the element on the DOM and add the load callback.
    var tab = tabManager.addTab(name, function () {
        if (!this.x_tab.ignoreClick) {
            filemanager.SwitchFile(this.x_file_id);
        }
    });
    tab.x_file_id = id;



    //Insert into loaded files.
    var tabData = {};
    tabData.name = name;
    tabData.id = id;
    tabData.state = 0;
    tabData.session = null; //Will be set to the session.
    tabData.saved = true;
    tabData.editCount = 0;
    tabData.dom = tab;
    tabData.shortName = name.split('/');
    tabData.shortName = tabData.shortName[tabData.shortName.length - 1];
    filemanager.loadedFiles[id] = tabData;

    //Set up IDE
    var EditSession = require("ace/edit_session").EditSession;
    tabData.session = new EditSession("");
    tabData.session.on("change", function () {
        //Mark this as "edited".
        console.log("edited");
        //Add a little star to the filename if we haven't already.
        //If this is the first edit, ignore it. It's us setting the value.
        if (filemanager.loadedFiles[tabData.id].saved && filemanager.loadedFiles[tabData.id].editCount != 0) {
            filemanager.loadedFiles[tabData.id].saved = false;
            filemanager.loadedFiles[tabData.id].dom.firstChild.innerText = filemanager.loadedFiles[tabData.id].shortName + "*";
        }
        filemanager.loadedFiles[tabData.id].editCount += 1;
    });

    //Switch to this tab.
    filemanager.SwitchFile(id);

    //Start the server request for the content. 
    project.serverRequest("media/" + id + "/get/application_json/", function (data) {
        var content = data.content;
        var type = data.type;
        //Update data.
        filemanager.loadedFiles[id].state = 1;
        filemanager.loadedFiles[id].saveUrl = data.saveUrl;
        //Update IDE.
        filemanager.loadedFiles[id].session.setMode("ace/mode/" + type);
        filemanager.loadedFiles[id].session.setValue(content);
        //Add an event listener to close a file to the close icon.
        tabManager.finishTabLoad(tab, function () {
            var id = this.parentElement.x_file_id;
            this.parentElement.x_tab.ignoreClick = true;
            filemanager.CloseFile(id);
        });
    }, null, true);
}

filemanager.SwitchFile = function (id) {
    //Set this file as the active tab.
    var file = filemanager.loadedFiles[id];
    //Unmark the last file and check to see if we are the last file.
    if (tabManager.compareCurrentTab(file.dom)) {
        //This is the same one.
        return;
    }
    //Switch to this tab.
    tabManager.switchToTab(filemanager.loadedFiles[id].dom);
    //Set up IDE.
    editor.setSession(file.session);
    //Set the active file.
    filemanager.lastFile = file;
}

filemanager.CloseFile = function (id) {
    var data = filemanager.loadedFiles[id];
    var endFunction = function () {
        //Actually do the closing.
        //Hide dialog
        project.hideDialog();
        //Close it's tab.
        tabManager.closeTab(filemanager.loadedFiles[id].dom);
        //Get rid of it from inside the list of open files.
        delete filemanager.loadedFiles[id];
    }
    //Ask for confirmation if this file is unsaved.
    if (filemanager.loadedFiles[id].saved) {
        //Just do it.
        endFunction();
    } else {
        project.showDialog("Save " + data.shortName + "?", "If you don't save changes to this file, they will be lost.", ["Save", "Don't Save", "Cancel"], [
            function () {
                project.showDialog("Saving...", "One moment, please.", [], []);
                filemanager.SaveFile(id, endFunction);
            },
            function () { endFunction(); },
            function () { /* Nothing */ }
        ]);
    }

}

filemanager.SaveFile = function (id, callback) {
    var data = filemanager.loadedFiles[id];
    //Send to the server.
    project.serverRequest("media/" + data.id + "/put/", function () {
        //Update the tab info.
        filemanager.loadedFiles[id].dom.firstChild.innerText = filemanager.loadedFiles[id].shortName;
        //Run callback
        callback();
    }, null, false, "POST", data.session.getValue());
}

filemanager.SaveAll = function (totalCallback) {
    //This could take a bit, so show a dialog.
    project.showDialog("Saving...", "One moment, please.", [], []);
    var saveCount = 0;
    //Count how many need saving.
    var keys = Object.keys(filemanager.loadedFiles);
    var toSave = [];
    for (var i = 0; i < keys.length; i++) {
        var d = filemanager.loadedFiles[keys[i]];
        if (d.saved == false) {
            //Set it to saved and update the dom, then add it to the save pile.
            d.saved = true;
            toSave.push(d);
        }
    }
    var closeCallback = function () {
        //Let auto cleanup do it.
    };
    //Check if any need saving.
    if (toSave.length == 0) {
        project.showDialog("Saved", "No files needed saving.", ["Close"], [closeCallback]);
        if (totalCallback != null) { totalCallback(); }
    } else {
        //Save all.
        for (var i = 0; i < toSave.length; i++) {
            filemanager.SaveFile(toSave[i].id, function () {
                //Increment the value and check if all are saved.
                saveCount += 1;
                if (toSave.length == saveCount) {
                    //Done.
                    project.showDialog("Saved", "Saved " + toSave.length + " files.", ["Close"], [closeCallback]);
                    if (totalCallback != null) { totalCallback(); }
                }
            });
        }
    }
}