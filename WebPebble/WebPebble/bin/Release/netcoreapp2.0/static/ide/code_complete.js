﻿var ycmd = {};
ycmd.latestRequest = -1;
ycmd.frame = document.getElementById('completion_frame');
ycmd.open = false;
ycmd.cursorPos = 0;

ycmd.subscribe = function () {
    //Subscribe to events from the editor.
    editor.on("change", ycmd.onEditorChange);
    //Subscribe to keybinds.
    keyboardJS.on('up', function () {
        ycmd.onUpDown('up');
    });
    keyboardJS.on('down', function () {
        ycmd.onUpDown('down');
    });
    keyboardJS.on('left', function () {
        ycmd.hideBox();
    });
    keyboardJS.on('right', function () {
        ycmd.hideBox();
    });
    keyboardJS.on('escape', function () {
        ycmd.hideBox();
    });
    keyboardJS.on('enter', function () {
        if (ycmd.open) {
            //Use the active one.
            ycmd.chooseOption(ycmd.frame.firstChild.x_complete[ycmd.cursorPos].x_complete_data);
        }
    });
    //Subscribe to clicks on the box.
    document.getElementsByClassName('ace_content')[0].addEventListener('click', ycmd.hideBox);
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
        ycmd.setSavedCursorPos();
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
    
    if (data.completions.length > 0) {
        //Insert into DOM.
        e.appendChild(ee);
        //Start listening if there are things to listen for.
        keyboardJS.watch(document.getElementsByClassName('ace_text-input')[0]);
        ee.x_complete[0].className = "c_item c_item_select";
        ycmd.boxPos = 0;
        ycmd.open = true;
        
    } else {
        //Hide.
        ee = document.createElement('div');
        ee.className = "completion_window completion_window_hidden";
        e.appendChild(ee);
        ycmd.open = false;
    }

    //Set position.
    ycmd.setBoxPos(e);

    //Insert final
    ycmd.frame.parentNode.replaceChild(e, ycmd.frame);
    ycmd.frame = e;
};

ycmd.setBoxPos = function (e) {
    var top = (document.getElementsByClassName('ace_gutter-active-line')[0].offsetTop + 60);
    e.style.top = top.toString() + "px";
    e.style.left = (document.getElementsByClassName('ace_gutter-active-line')[0].offsetWidth + 315).toString() + "px";


    
    var f = e.firstChild;
    if (f !== null) {
        if (top < 310) {
            //Not enough space. Use bottom.
            f.className = "completion_window completion_window_top";
        } else {
            f.className = "completion_window";
        }
    }
    
};

ycmd.setSavedCursorPos = function () {
    ycmd.cursorPos = editor.getCursorPosition();
};

ycmd.hideBox = function () {
    var ee = ycmd.frame.firstChild;
    if (ee !== null) {
        ee.className = "completion_window completion_window_hidden";
    }
    keyboardJS.stop();
    ycmd.open = false;
};

ycmd.onUpDown = function (key) {
    //If this is open, set the cursor back and scroll through the dialog.
    var pos = ycmd.cursorPos;
    if (key == 'up') {
        //editor.moveCursorTo(pos.row + 1, pos.column);
        ycmd.setCursorPosInWindow(ycmd.boxPos - 1);
    } else {
        //editor.moveCursorTo(pos.row - 1, pos.column);
        ycmd.setCursorPosInWindow(ycmd.boxPos + 1);
    }
    editor.moveCursorTo(pos.row, pos.column);
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

ycmd.chooseOption = function (data) {
    console.log(data);
    //Hide the box
    ycmd.hideBox();
}