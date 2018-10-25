var ycmd = {};
ycmd.latestRequest = -1;
ycmd.frame = document.getElementById('completion_frame');
ycmd.open = false;
ycmd.filler_input = document.getElementById('completion_input');

ycmd.subscribe = function () {
    //Subscribe to events from the editor.
    editor.on("change", ycmd.onEditorChange);
    //Subscribe to changes in the filler. These changes will just be mirrored in the input.
    ycmd.filler_input.oninput = function () {
        var d = this.value;
        //Append this at the current position.
        editor.insert(d);
        //Debug
        console.log("Redirected character typed: " + d);
        //Clear.
        this.value = "";
        //Advance cursor.
        var pos = editor.getCursorPosition();
        pos.column += 1;
        editor.moveCursorTo(pos.column, pos.row);
    };
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
            console.log(data);
            
        }
    });
    //Finally, move the existing box.
    ycmd.setBoxPos(ycmd.frame);
};

ycmd.onGotYcmdComp = function (data) {
    console.log(data);

    var e = ycmd.showBox(data);

    //Insert final
    ycmd.frame.parentNode.replaceChild(e, ycmd.frame);
    ycmd.frame = e;
};

ycmd.getBoxClassname = function (e) {
    var top = (document.getElementsByClassName('ace_gutter-active-line')[0].offsetTop + 60);
    e.style.top = top.toString() + "px";
    e.style.left = (document.getElementsByClassName('ace_gutter-active-line')[0].offsetWidth + 315).toString() + "px";

    if (top < 310) {
        //Not enough space. Use bottom.
        return "completion_window completion_window_top";
    } else {
        return "completion_window";
    }
};

ycmd.setBoxPos = function (e) {
    var f = e.firstChild;
    if (f !== null) {
        f.className = ycmd.getBoxClassname(e);
    }
    
};

ycmd.setSavedCursorPos = function () {
    ycmd.cursorPos = editor.getCursorPosition();
};

ycmd.hideBox = function () {
    var ee = ycmd.frame.firstChild;
    if (ee !== null) {
        ee.className = ycmd.getBoxClassname(ycmd.frame) + " completion_window_hidden";
    }
    keyboardJS.stop();
    ycmd.open = false;
};

ycmd.showBox = function (data) {
    //Create html for the predictions.
    var e = document.createElement('div');
    e.className = "completion_frame open_sans";
    var ee = document.createElement('div');
    ee.x_complete = [];
    for (var i = 0; i < data.completions.length; i += 1) {
        var o = document.createElement('div');
        var d = data.completions[i];
        console.log(d.type);
        o.className = "c_item";
        o.innerText = d.menu_text;

        o.x_complete_data = d;
        o.x_complete_id = i;

        ee.appendChild(o);
        ee.x_complete.push(o);
    }

    //Set box position and show it.
    ycmd.setBoxPos(e);

    //Take control.
    ycmd.filler_input.value = "";
    ycmd.filler_input.focus();

    //Return box.
    return e;
};