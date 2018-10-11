var sidebarmanager = {};

sidebarmanager.activeItem = null;
sidebarmanager.items = [];

sidebarmanager.addButton = function (name, sectionIndex, buttonType, clickAction, closeAction, htmlDom, internalId) {
    //Name: Name dislpayed
    //sectionIndex: The index of the section to insert this into.
    //buttonType: If this is true, this is displayed as a button. A button cannot be closed, and cannot use a text entry area.
    //clickAction: Called when this tab is switched to.
    //closeAction: Called when this tab is closed. Can be null.
    //htmlDom: The DOM object to clone. If this is null, the text entry area will be used instead.
    //internalId: The ID to use internally.

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

    //Add to our internal array.
    var t = { "name": name, "sectionIndex": sectionIndex, "buttonType": buttonType, "clickAction": clickAction, "closeAction": closeAction, "dom_template": htmlDom, "internalId": internalId, "tab_ele": tab, "copy_dom": copydom, "is_dom_ele": copydom != null, "edit_session": editsession };
    sidebarmanager.items[internalId] = t;

    //Add an event listener to this object.
    tab.addEventListener('click', sidebarmanager.private_click);

    //Switch to this
    sidebarmanager.show_content(sidebarmanager.items[internalId]);

    return sidebarmanager.items[internalId];
}

sidebarmanager.private_click = function () {
    //Get the data for this.
    var d = sidebarmanager.items[this.x_id];
    //Run the callback for this first in case it needs to prepare.
    d.clickAction();
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
}