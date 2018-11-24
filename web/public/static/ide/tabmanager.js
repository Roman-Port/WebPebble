var sidebarmanager = {};

sidebarmanager.activeItem = null;
sidebarmanager.items = [];

sidebarmanager.unsaved = {}; //Unsaved data.

sidebarmanager.addButton = function (name, sectionIndex, buttonType, clickAction, closeAction, htmlDom, internalId, showNow, actionsHtml, clickActionAfter) {
    //Name: Name dislpayed
    //sectionIndex: The index of the section to insert this into.
    //buttonType: If this is true, this is displayed as a button. A button cannot be closed, and cannot use a text entry area.
    //clickAction: Called when this tab is switched to.
    //closeAction: Called when this tab is closed. Can be null.
    //htmlDom: The DOM object to clone. If this is null, the text entry area will be used instead.
    //internalId: The ID to use internally.
    //showNow: If true, this tab will be switched to the moment it is added.

    //First, create the dom element to use.
    var tab = document.createElement('div');
    tab.x_id = internalId;
    var tab_inner = document.createElement('div');
    tab_inner.className = "btn";
    if (buttonType == true) { tab_inner.className = "btn btn_active"; }
    tab.appendChild(tab_inner);
    //Attach to section.
    document.getElementById('sidebar_sec_' + sectionIndex).appendChild(tab);
    //Clone dom element.
    var copydom = null;
    var editsession = null;
    if (htmlDom != null) {
        //There was once code here.
    } else {
        //This is a editor. Set the edit session.
        var EditSession = require("ace/edit_session").EditSession;
        editsession = new EditSession("");
        /*editsession.on("change", function () {
            
        });*/
    }

    if (actionsHtml == null) {
        actionsHtml = "";
    }

    //Add to our internal array.
    var t = { "name": name, "sectionIndex": sectionIndex, "buttonType": buttonType, "clickAction": clickAction, "closeAction": closeAction, "dom_template": htmlDom, "internalId": internalId, "tab_ele": tab, "is_dom_ele": htmlDom != null, "edit_session": editsession, "actions_html": actionsHtml, "clickActionAfter": clickActionAfter };
    sidebarmanager.items[internalId] = t;

    //Add an event listener to this object.
    tab.addEventListener('click', sidebarmanager.private_click);

    //Switch if requested.
    if (showNow) {
        //Deactive old items
        if (sidebarmanager.activeItem != null) {
            sidebarmanager.hide_content(sidebarmanager.activeItem);
        }

        //Switch to this
        sidebarmanager.show_content(sidebarmanager.items[internalId]);
    }

    //Correctly update name of tab.
    sidebarmanager.updateSuffixOfTab(sidebarmanager.items[internalId].tab_ele);

    return sidebarmanager.items[internalId];
};

sidebarmanager.markUnsaved = function (name, warnOnTabSwitch, saveCallback) {
    //Called by other files to mark a file as unsaved. If there are unsaved files, we won't reload the page. You can also mark a file and a warning will appear if you switch away tabs.
    //Save callback should have a callback as the first arg.

    //Stop if this is already pending.
    if (sidebarmanager.unsaved[sidebarmanager.activeItem.internalId] != null) {
        return false;
    }

    //Create entity.
    var e = {};
    e.displayName = name;
    e.warnOnTabSwitch = warnOnTabSwitch;
    e.saveCallback = saveCallback;
    //Yucky code to derefrence.
    var d = [sidebarmanager.activeItem.internalId];
    e.tab = JSON.parse(JSON.stringify(d))[0];
    //Add to list.
    console.log("Adding unsaved file:");
    console.log(e);
    sidebarmanager.unsaved[e.tab] = e;
    return true;
};

sidebarmanager.unmarkUnsaved = function () {
    //Unmark the current tab as unsaved.
    if (sidebarmanager.unsaved[sidebarmanager.activeItem.internalId] != null) {
        delete sidebarmanager.unsaved[sidebarmanager.activeItem.internalId];
    }
};

sidebarmanager.checkIfCurrentFileIsUnsaved = function (onlyTabSwitch, id) {
    //onlyTabSwitch is true if this is just a tab, false if exiting the entire tab.
    //Returns null if nothing, else returns data.
    //If ID is null, set it to the current tab.
    if (id == null) {
        id = sidebarmanager.activeItem.internalId;
    }
    //Check if this exists.
    if (sidebarmanager.unsaved[id] != null) {
        var d = sidebarmanager.unsaved[id];
        if (onlyTabSwitch) {
            if (!d.warnOnTabSwitch) {
                //Return null, this is just a tab switch.
                return null;
            }
        }
        return d;
    } else {
        return null;
    }
}

sidebarmanager.updateSuffixOfTab = function (tab, suffix) {
    if (suffix == null) { suffix = ""; }
    if (tab.tab_ele != null) {
        var shortName = sidebarmanager.items[tab.tab_ele.x_id].name;
        tab.tab_ele.firstChild.innerHTML = shortName + suffix + sidebarmanager.items[tab.tab_ele.x_id].actions_html;
    } else {
        var shortName = sidebarmanager.items[tab.x_id].name;
        tab.firstChild.innerHTML = shortName + suffix + sidebarmanager.items[tab.x_id].actions_html;
    }
    
}

sidebarmanager.private_click = function () {
    //Get the data for this.
    var d = sidebarmanager.items[this.x_id];
    //Store this in a function.
    var final_callback = function (id) {
        //Run the callback for this first in case it needs to prepare.
        var reply = d.clickAction(id);
        //Stop if reply is not null and is false.
        if (reply != null) {
            if (reply == false) {
                return;
            }
        }
        //Now, switch views. Hide the last item if needed.
        if (sidebarmanager.activeItem != null) {
            sidebarmanager.hide_content(sidebarmanager.activeItem);
        }
        //Show the current view.
        sidebarmanager.show_content(d);
    }
    //Check if this is unsaved.
    var id = this.x_id;
    if (sidebarmanager.activeItem == null) {
        //This is the first item opened. Don't check and go ahead.
        final_callback(id);
    } else {
        var unsaved = sidebarmanager.checkIfCurrentFileIsUnsaved(true, sidebarmanager.activeItem.internalId);
        if (unsaved != null) {
            //Warn the user about this.
            project.showDialog("Unsaved Work", "If you continue, your unsaved work will be lost. Would you like to save?", ["Cancel", "Save", "Don't Save"], [
                function () {
                    //Cancel. Do nothing.
                    return;
                },
                function () {
                    //Save first, then close.
                    unsaved.saveCallback(function () {
                        sidebarmanager.unmarkUnsaved();

                        final_callback(id);
                    });
                },
                function () {
                    //Just close.
                    sidebarmanager.unmarkUnsaved();

                    final_callback(id);
                }
            ]);
        } else {
            //Go now.
            final_callback(id);
        }
    }


};

sidebarmanager.close_active_tab = function () {
    //Unmark as unsaved
    sidebarmanager.unmarkUnsaved();
    //Switch away from tab.
    sidebarmanager.hide_content(sidebarmanager.activeItem);
    //Update the tab info.
    sidebarmanager.activeItem.tab_ele.parentNode.removeChild(sidebarmanager.activeItem.tab_ele);
    //Clear active tab.
    sidebarmanager.activeItem = null;
};

sidebarmanager.hide_content = function (tab) {
    //If this has a dom element associated, hide it.
    if (tab.is_dom_ele) {
        //Destroy the content.
        tab.copy_dom.parentNode.removeChild(tab.copy_dom);
    } else {
        //Hide the text editor
        document.getElementById('editor_inner').style.display = "none";
    }

    //Make this button appear inactive.
    if (tab.buttonType == false) {
        tab.tab_ele.firstChild.className = "btn";
    }

    //Make sure editor is hidden.
    document.getElementById('editor').className = "ide_window hide";
}

sidebarmanager.show_content = function (tab) {
    //If this has a dom element associated, show it.
    if (tab.is_dom_ele) {
        //Clone the content.
        var copy = tab.dom_template.cloneNode(true);
        copy.className = "fillEditor open_sans open-sans";
        copy.id = "";
        document.getElementById('editor').appendChild(copy);
        tab.copy_dom = copy;
        tab.copy_dom.style.display = "block";
    } else {
        //Switch to this document.
        editor.setSession(tab.edit_session);
        //show the text editor
        document.getElementById('editor_inner').style.display = "block";
    }

    //Run after callback.
    if (tab.clickActionAfter != null) {
        tab.clickActionAfter(tab.internalId);
    }

    //Set this to the current view.
    sidebarmanager.activeItem = tab;

    //Make sure editor is shown.
    document.getElementById('editor').className = "ide_window";

    //Make this button appear active.
    if (tab.buttonType == false) {
        tab.tab_ele.firstChild.className = "btn btn_fill_active";
    }
}

var html_tools = {};
html_tools.createQuickDom = function (text, type, parent) {
    var d = document.createElement(type);
    d.innerText = text;
    parent.appendChild(d);
}

html_tools.createQuickDomClass = function (text, type, parent, className) {
    var d = document.createElement(type);
    d.innerText = text;
    d.className = className;
    parent.appendChild(d);
}

html_tools.createQuickDomClassEvent = function (text, type, parent, className, event) {
    var d = document.createElement(type);
    d.innerText = text;
    d.className = className;
    d.addEventListener('click', event);
    parent.appendChild(d);
}