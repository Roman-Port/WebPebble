var filemanager = {};

filemanager.loadedFiles = {};
filemanager.lastFile = null;

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
        var dd = filemanager.loadedFiles[id];
        dd.tab.tab_ele.firstChild.innerText = dd.shortName;
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