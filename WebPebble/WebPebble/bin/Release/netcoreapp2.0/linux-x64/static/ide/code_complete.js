var ycmd = {};
ycmd.latestRequest = -1;
ycmd.frame = document.getElementById('completion_frame');
ycmd.external = {};
ycmd.external.getCaretCoordinates = null;

ycmd.subscribe = function () {
    //Subscribe to events from the editor.
    editor.on("change", ycmd.onEditorChange);
    // Init the external thingy here. The way we use it is gross, but it works.
    ycmd.external.getCaretCoordinates = require('textarea-caret');
};

ycmd.onEditorChange = function (conte) {
    //Get the position of the cursor.
    var pos = editor.getCursorPosition();
    pos.column += 1;
    pos.row += 1;
    var content = filemanager.loadedFiles[sidebarmanager.activeItem.internalId].session.getValue();
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
    var textArea = document.getElementsByClassName('ace_text-input')[0];
    var caret = getCaretCoordinates(this, this.selectionEnd);
    console.log('(top, left, height) = (%s, %s, %s)', caret.top, caret.left, caret.height);

    e.style.top = (caret.top + 60).toString() + "px";
    e.style.left = (caret.left + 80 + document.getElementsByClassName('ace_gutter-active-lin')[0].offsetWidth).toString() + "px";
    //Insert into DOM.
    ycmd.frame.parentNode.replaceChild(e, ycmd.frame);
    ycmd.frame = e;
}

