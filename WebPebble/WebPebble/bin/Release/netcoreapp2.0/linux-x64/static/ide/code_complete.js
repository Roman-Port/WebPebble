var ycmd = {};
ycmd.latestRequest = -1;
ycmd.frame = document.getElementById('completion_frame');

ycmd.subscribe = function () {
    //Subscribe to events from the editor.
    editor.on("change", ycmd.onEditorChange);
    //Subscribe to keybinds.
    keyboardJS.on('up', function () {
        console.log('up');
        ycmd.hideBox();
    });
    keyboardJS.on('down', function () {
        console.log('down');
        ycmd.hideBox();
    });
    keyboardJS.on('left', function () {
        console.log('right');
        ycmd.hideBox();
    });
    keyboardJS.on('right', function () {
        console.log('left');
        ycmd.hideBox();
    });
};

ycmd.onEditorChange = function (conte) {
    //Get the position of the cursor.
    var pos = editor.getCursorPosition();
    pos.column += 1;
    pos.row += 1;
    var content = filemanager.loadedFiles[sidebarmanager.activeItem.internalId].session.getValue();
    keyboardJS.watch(document.getElementsByClassName('ace_text-input')[0]);
    //Make a request to YCMD.
    var data = {
        "project_id": project.id,
        "asset_id": sidebarmanager.activeItem.internalId,
        "line_no": pos.row,
        "col_no": pos.column,
        "buffer": content
    };
    console.log(data);
    //Create a request to YCMD.
    ycmd.latestRequest = phoneconn.send(6, data, function (ycmd_reply) {
        //Check if this is the latest request.
        if (ycmd_reply.requestid === ycmd.latestRequest) {
            //Okay. Move on.
            ycmd.onGotYcmdComp(ycmd_reply.data.ycmd.sdks.sdk);
        }
    });
    //Finally, move the existing box.
    ycmd.setBoxPos(ycmd.frame);
};

ycmd.onGotYcmdComp = function (data) {
    console.log(data);
    //Create html for the predictions.
    var e = document.createElement('div');
    e.className = "completion_frame";
    var ee = document.createElement('div');
    ee.className = "completion_window";

    e.appendChild(ee);

    //Set position.
    ycmd.setBoxPos(e);
    //Insert into DOM.
    ycmd.frame.parentNode.replaceChild(e, ycmd.frame);
    ycmd.frame = e;
};

ycmd.setBoxPos = function (e) {
    e.style.top = (document.getElementsByClassName('ace_gutter-active-line')[0].offsetTop + 60).toString() + "px";
    e.style.left = (document.getElementsByClassName('ace_gutter-active-line')[0].offsetWidth + 315).toString() + "px";
};

ycmd.hideBox = function () {
    var ee = ycmd.frame.firstChild;
    if (ee !== null) {
        ee.className = "completion_window completion_window_hidden";
    }
}