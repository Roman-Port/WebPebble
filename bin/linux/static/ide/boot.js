﻿//Init editor
var editor = ace.edit("editor_inner");
editor.setTheme("ace/theme/dracula");
editor.session.setMode("ace/mode/c_cpp");
//Add sidebar options
sidebarmanager.addButton("Settings", 0, false, function () {
    console.log("click");
}, function () { }, null, "sidebar_sett", false);
sidebarmanager.addButton("Compilation", 0, false, function () {
    //Fetch data.
    project.serverRequest("build_history/", function (data) {
        //Add each build.
        for (var i = 0; i < data.length; i += 1) {
            var build = data[i];
            var dom = document.createElement('tr');
            html_tools.createQuickDom(build.id, "th", dom);
            html_tools.createQuickDom(build.time, "th", dom);
            if (build.passed) {
                html_tools.createQuickDom("OK", "th", dom);
            } else {
                html_tools.createQuickDom("Failed", "th", dom);
            }
            //add btns
            var b = document.createElement('th');
            html_tools.createQuickDomClassEvent("Build Log", 'div', b, 'med_button', function () { });
            html_tools.createQuickDomClassEvent("Get PBW", 'div', b, 'med_button', function () { });
            dom.appendChild(b);
            dom.x_id = build.id;
            document.getElementsByClassName('build_history')[0].appendChild(dom);
        }
        //Reveal list
        document.getElementsByClassName('build_history_area')[0].className = "container build_history_area";
    }, function () { }, true);
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