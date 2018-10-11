//Init editor
var editor = ace.edit("editor_inner");
editor.setTheme("ace/theme/dracula");
editor.session.setMode("ace/mode/c_cpp");
//Add sidebar options
sidebarmanager.addButton("Settings", 0, false, function () {
    console.log("click");
}, function () { }, null, "sidebar_sett");
sidebarmanager.addButton("Compilation", 0, false, function () {
    console.log("click");
}, function () { }, null, "sidebar_compile");
//Add options to add sources
sidebarmanager.addButton("Add Source", 1, true, function () {
    console.log("click");
}, function () { }, null, "sidebar_add_src");
sidebarmanager.addButton("Add Resource", 2, true, function () {
    console.log("click");
}, function () { }, null, "sidebar_add_resrc");
//Boot
project.init();
project.initKeybinds();