﻿//Init editor
var editor = ace.edit("editor_inner");
editor.setTheme("ace/theme/dracula");
editor.session.setMode("ace/mode/c_cpp");
//Add sidebar options
sidebarmanager.addButton("Settings", 0, false, function () {
    console.log("click");
}, function () { }, null, "sidebar_sett", false);
sidebarmanager.addButton("Compilation", 0, false, function () {
    console.log("click");
}, function () { }, document.getElementById('template_compile'), "sidebar_compile", false);
//Add options to add sources
sidebarmanager.addButton("Add Source", 1, true, function () {
    project.showAddAssetDialog();
    return false;
}, function () { }, null, "sidebar_add_src", false);
sidebarmanager.addButton("Add Resource", 2, true, function () {
    console.log("click");
}, function () { }, null, "sidebar_add_resrc", false);
//Boot
project.init();
project.initKeybinds();