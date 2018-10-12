//Init editor
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
                html_tools.createQuickDom(build.id, "OK", dom);
            } else {
                html_tools.createQuickDom(build.id, "Failed", dom);
            }
            document.getElementsByClassName('build_history').appendChild(dom);
        }
        //Reveal list
        document.getElementsByClassName('build_history_area').style.filer = "none";
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