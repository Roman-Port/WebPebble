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
                project.showDialog("Saving...", "<div class=\"inf_loader\"></div>", [], []);
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
    project.serverRequest("media/" + data.id + "/?upload_method=Binary", function () {
        //Update the tab info.
        var dd = filemanager.loadedFiles[id];
        sidebarmanager.updateSuffixOfTab(dd.tab);
        //Run callback
        callback();
    }, null, false, "PUT", data.session.getValue());
}

filemanager.PromptDeleteFile = function (file) {
    project.showDialog("Delete file \"" + file.shortName + "\"?", "This cannot be undone, and will happen immediately.", ["Confirm", "Cancel"], [
        function () {
            project.serverRequest("media/" + file.id + "/", function () {
                //Switch away from tab.
                sidebarmanager.hide_content(sidebarmanager.activeItem);
                //Update the tab info.
                sidebarmanager.activeItem.tab_ele.parentNode.removeChild(sidebarmanager.activeItem.tab_ele);
            }, null, false, "DELETE", null);
        }, function () {

        }
    ]);
};

filemanager.PromptDeleteResourceFile = function () {
    var file = edit_resource.openFile;
    project.showDialog("Delete file \"" + file.media_data.nickname + "\"?", "This cannot be undone, and will happen immediately.", ["Confirm", "Cancel"], [
        function () {
            project.showLoader("Removing File...");
            //Delete the actual media. This will also delete any appinfo.json data associated with this.
            project.serverRequest("media/" + file.id + "/", function () {
                sidebarmanager.close_active_tab();
                //Hide the loader.
                project.hideDialog();
            }, null, false, "DELETE");
            
        }, function () {

        }
    ]);
};

filemanager.DownloadUrl = function (url) {
    //Check if this is legacy from the old days
    if(url.startsWith('/')) {
        url = "https://api.webpebble.get-rpws.com"+url;
    }
    //Open an iframe to download this file.
    var ifg = document.createElement('iframe');
    ifg.src = url;
    ifg.style.display = "none";
    document.body.appendChild(ifg);
    //Close after a few seconds.
    window.setTimeout(function () {
        document.body.removeChild(ifg);
    }, 2000);
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