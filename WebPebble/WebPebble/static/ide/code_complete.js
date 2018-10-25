﻿var ycmd = {};
ycmd.latestRequest = -1;
ycmd.frame = document.getElementById('completion_frame');
ycmd.open = false;

ycmd.subscribe = function () {
    //Subscribe to events from the editor.
    editor.on("change", ycmd.onEditorChange);
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
    //Create a request to YCMD.
    ycmd.latestRequest = phoneconn.send(6, data, function (ycmd_reply) {
        
        //Check if this is the latest request.
        if (ycmd_reply.requestid === ycmd.latestRequest) {
            var d = ycmd_reply.data.ycmd.sdks.sdk;
            console.log(d);
            //Okay. Move on.
            ycmd.onGotYcmdComp(ycmd_reply.data.ycmd.sdks.sdk);
            
        }
    });
    //Finally, move the existing box.
    ycmd.setBoxPos(ycmd.frame);
};

ycmd.onGotYcmdComp = function (data) {

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

ycmd.setCursorPosInWindow = function (newPos) {
    var ee = ycmd.frame.firstChild;
    //Check.
    if (newPos < 0) {
        newPos = 0;
    }
    if (newPos > ee.x_complete.length - 1) {
        newPos = ee.x_complete.length - 1;
    }
    //Unset the existing one.
    ee.x_complete[ycmd.boxPos].className = "c_item";
    ee.x_complete[newPos].className = "c_item c_item_select";
    ycmd.boxPos = newPos;
    //Scroll to view.
    ee.x_complete[ycmd.boxPos].scrollIntoView();
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
    console.log(data);
    //Create html for the predictions.
    var e = document.createElement('div');
    e.className = "completion_frame open_sans";
    var ee = document.createElement('div');
    ee.x_complete = [];
    for (var i = 0; i < data.completions.length; i += 1) {
        var o = document.createElement('div');
        var d = data.completions[i];
        o.className = "c_item";
        o.innerText = d.menu_text;

        o.x_complete_data = d;
        o.x_complete_id = i;

        ee.appendChild(o);
        ee.x_complete.push(o);
    }
    e.appendChild(ee);

    //Set box position and show it.
    ycmd.setBoxPos(e);
    ycmd.boxPos = 0;

    //Return box.
    return e;
};

ycmd.offsetCursor = function (x, y) {
    var pos = editor.getCursorPosition();
    pos.column += x;
    pos.row += y;
    editor.moveCursorTo(pos.row, pos.column);
}

/* On key presses */
ycmd.checkIfKeypressIsTarget = function () {
    //Check if a keypress was actually directed at us.
    return editor.isFocused() && ycmd.open;
}

ycmd.onHijackKeypress = function (e) {
    //Called from Ace. This is a key event.
    if (e.keyCode == 38) {
        //Up.
        ycmd.onKeyDirPress(-1);
    }
    if (e.keyCode == 40) {
        //Down.
        ycmd.onKeyDirPress(1);
    }

    return true;
}

ycmd.onKeyDirPress = function (boxDir) {
    //Box dir is the direction to scroll in the box.
    //Scroll in the box.
    ycmd.setCursorPosInWindow(ycmd.boxPos + boxDir);
}