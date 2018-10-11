var sidebarmanager = {};

sidebarmanager.activeItem = null;
sidebarmanager.items = [];

sidebarmanager.addButton = function (name, sectionIndex, buttonType, clickAction, closeAction, htmlDom, internalId, showNow, actionsHtml) {
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
        var copy = htmlDom.cloneNode(true);
        copy.className = "fillEditor";
        copy.id = "";
        document.getElementById('editor').appendChild(copy);
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
    var t = { "name": name, "sectionIndex": sectionIndex, "buttonType": buttonType, "clickAction": clickAction, "closeAction": closeAction, "dom_template": htmlDom, "internalId": internalId, "tab_ele": tab, "copy_dom": copydom, "is_dom_ele": copydom != null, "edit_session": editsession, "actions_html": actionsHtml };
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
    //Run the callback for this first in case it needs to prepare.
    var reply = d.clickAction(this.x_id);
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

sidebarmanager.hide_content = function (tab) {
    //If this has a dom element associated, hide it.
    if (tab.is_dom_ele) {
        //Hide the content.
        tab.copy_dom.style.display = "none";
    } else {
        //Hide the text editor
        document.getElementById('editor_inner').style.display = "none";
    }

    //Make this button appear inactive.
    if (tab.buttonType == false) {
        tab.tab_ele.firstChild.className = "btn";
    }
}

sidebarmanager.show_content = function (tab) {
    //If this has a dom element associated, show it.
    if (tab.is_dom_ele) {
        //show the content.
        tab.copy_dom.style.display = "block";
    } else {
        //Switch to this document.
        editor.setSession(tab.edit_session);
        //show the text editor
        document.getElementById('editor_inner').style.display = "block";
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