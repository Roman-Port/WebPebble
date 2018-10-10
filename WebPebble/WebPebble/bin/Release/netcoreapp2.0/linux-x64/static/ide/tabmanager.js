var tabManager = {};
tabManager.activeTabs = [];
tabManager.currentTab = null;

tabManager.addDomTab = function (title, ele, exitCallback) {
    //Copy an element into this tab's dedicated HTML. Automates things.
    var tab = tabManager.addTab(title, function () {
        if (!this.x_tab.ignoreClick) {
            tabManager.switchToTab(this);
        }
    });
    tab.x_tab.exitCallback = exitCallback;
    //Copy this element.
    var copy = ele.cloneNode(true);
    copy.className = "fillEditor";
    copy.id = "";
    document.getElementById('editor').appendChild(copy);
    tab.x_tab.content = copy;
    //Instantly "finish loading".
    tabManager.finishTabLoad(tab, function () {
        var tab = this.parentElement;
        tab.x_tab.ignoreClick = true; //Used to prevent clicking after a tab is closed.
        //Clean up by deleting the html. First, call the callback in case it needs it.
        if (tab.x_tab.exitCallback != null) {
            tab.x_tab.exitCallback();
        }
        //Do cleanup.
        var target = tab.x_tab.content;
        target.parentElement.removeChild(target);
        //Close tab.
        tabManager.closeTab(tab);
    });
    //Now, switch to this tab.
    tabManager.switchToTab(tab);
}

tabManager.compareCurrentTab = function (tab) {
    return tab == tabManager.currentTab;
}

tabManager.addTab = function (title, clickCallback) {
    //Get the DOM.
    var tabs = document.getElementById('tabs');
    //Create object.
    var tab = document.createElement('div');
    tab.dom = tab; //janky
    tab.className = "tab";
    //Set inner img
    var tabNameInner = document.createElement('span');
    tabNameInner.innerText = title;
    tab.appendChild(tabNameInner);
    //Add image
    var tabImg = document.createElement('img');
    //Use a loader until this image is downloaded from the server. You'll call our function for that.
    tabImg.src = "https://romanport.com/static/icons/loader.svg";
    tab.appendChild(tabImg);

    //Add the functions to the tab. The first one is going to be for finishing the loading process.
    tab.x_tab = {};
    tab.x_tab.dom = tab;
    tab.x_tab.ignoreClick = false;

    //Add event listner to the tab.
    tab.addEventListener('click', clickCallback);

    //Append this to the dom.
    tabs.appendChild(tab);

    //Add to active tabs.
    tabManager.activeTabs.push(tab);

    //Ensure that the ide can be seen
    document.getElementById('editor').className = "ide_window";

    //Return tab
    return tab;
}

tabManager.finishTabLoad = function (tab, exitCallback) {
    //Find the image.
    var ti = tab.lastChild;
    ti.src = "https://romanport.com/static/icons/baseline-close.svg";
    //Set callback.
    tab.lastChild.addEventListener('click', exitCallback);
}

tabManager.switchToTab = function (tab) {
    //First, deactive the current tab.
    if (tabManager.currentTab != null) {
        tabManager.currentTab.className = "tab";
        //If this tab has dedicated html, hide it.
        if (tabManager.currentTab.x_tab.content != null) {
            tabManager.currentTab.x_tab.content.style.display = "none";
        }
    }
    //Set this as the current tab.
    tabManager.currentTab = tab;
    tab.className = "tab tab_active";
    //If we have dedicated html, show it.
    if (tab.x_tab.content != null) {
        tab.x_tab.content.style.display = "block";
    }
}

tabManager.closeTab = function (tab) {
    //Assume cleanup has already been done.
    //If this is the active tab, switch to the first one.
    var nextTab = null;
    if (tabManager.compareCurrentTab(tab)) {
        //If there are no other tabs, collapse the view.
        if (tabManager.activeTabs.length == 1) {
            //Last one.
            //Hide the entire editor.
            document.getElementById('editor').className = "ide_window hide";
        } else {
            //Switch to the first one that isn't us.
            var i = 0;
            while (true) {
                var target = tabManager.activeTabs[i];
                if (tabManager.compareCurrentTab(target)) {
                    //This is us. Skip.
                    i += 1;
                } else {
                    //Switch to this one.
                    nextTab = target;
                    break;
                }
            }
        }
    } else {
        console.log("keep this tab active");
    }
    //Now, it is safe to do what we want with this tab.
    //Remove it from the list at the top and the internal list.
    tabManager.activeTabs.splice(tabManager.activeTabs.indexOf(tab), 1);
    tab.parentElement.removeChild(tab);
    //Switch to
    if (nextTab != null) {
        tabManager.switchToTab(nextTab);
    }
}