var edit_resource = {};
edit_resource.openFile = null;

edit_resource.onSelect = function () {
    //Selected to start.
    edit_resource.openFile = null;
    //Make sure we can see the selected type.
    edit_resource.onTypeChange('bitmap');
}

edit_resource.onSelectExisting = function (context) {
    //Context contains our ID. Get the data for it.
    var pebble_data = project.appInfo.pebble.resources.media;
    //Find this one.
    for (var i = 0; i < pebble_data.length; i += 1) {
        if (pebble_data[i].x_webpebble_media_id == context) {
            pebble_data = pebble_data[i];
            break;
        }
    }
    //Get the media data.
    var media_data = project.mediaResourcesFiles[context];
    console.log(pebble_data);
    console.log(media_data);
    //Store temporarily.
    edit_resource.openFile = {
        "media_data": media_data,
        "pebble_data": pebble_data,
        "id": context
    };
    //Fill in DOM data.
    document.getElementById('addresrc_entry_filename').value = media_data.nickname;
    document.getElementById('addresrc_entry_id').value = pebble_data.name; //C ID
    document.getElementById('addresrc_entry_type').value = pebble_data.type; //The type
    //Do chosen platforms later.
    edit_resource.onTypeChange(pebble_data.type);
    //Type specific data.
    if (pebble_data.type == "font") {
        //Use the regex entered as the character regex.
        document.getElementById('addresrc_entry_font_characters').value = pebble_data.characterRegex;
        //Trim the font size off of the identifier and fill in the font size and ID.
        var name_split = pebble_data.name.split('_');
        var font_size = name_split[name_split.length - 1];
        var idd = pebble_data.name.substring(0, pebble_data.name.length - font_size.length - 1);
        //Set elements
        document.getElementById('addresrc_entry_font_size').value = font_size;
        document.getElementById('addresrc_entry_id').value = idd;
        //If the tracking adjust isn't zero, set it.
        var trackAdjust = pebble_data.trackingAdjust;
        if (trackAdjust != null) {
            document.getElementById('addresrc_entry_font_tracking').value = pebble_data.trackingAdjust;
        }
        //If compatability isn't latest, set it to 2.7.
        if (pebble_data.compatibility != null) {
            document.getElementById('addresrc_entry_font_compat').value = "27";
        }
    }
    if (pebble_data.type == "bitmap") {
        //Add each compression type.
        if (pebble_data.memoryFormat != null) {
            document.getElementById('addresrc_entry_bitmap_memformat').value = pebble_data.memoryFormat;
        }
        if (pebble_data.spaceOptimization != null) {
            document.getElementById('addresrc_entry_bitmap_opti').value = pebble_data.spaceOptimization;
        }
        if (pebble_data.storageFormat != null) {
            document.getElementById('addresrc_entry_bitmap_storeformat').value = pebble_data.storageFormat;
        }
    }
    edit_resource.onFontSizeChange();
    document.getElementById('add_resrc_delete').style.display = "inline-block";
}

edit_resource.onTypeChange = function (type) {
    //Hide all of the special types.
    var typeDivs = document.getElementsByClassName('typeselect_hidden');
    for (var i = 0; i < typeDivs.length; i += 1) {
        typeDivs[i].style.display = "none";
    }
    //If it exists, show the special div for this.
    var specialDiv = document.getElementById('typeselect_template_' + type);
    if (specialDiv != null) {
        specialDiv.style.display = "block";
    }
}

edit_resource.onFontSizeChange = function () {
    //Fill in the "identifier" box.
    document.getElementById('typeselect_font_id').innerText = document.getElementById('addresrc_entry_id').value + "_" + document.getElementById('addresrc_entry_font_size').value;
}

edit_resource.getUpdatedPebbleMedia = function (fileData) {
    //Get the package.json/pebble/resources/media data for this.
    //fileData is the WebPebble media data.
    var o = {};
    o.file = fileData.filename.substring(10); //Trim off the /resources.
    o.name = document.getElementById('addresrc_entry_id').value; //C ID
    o.type = document.getElementById('addresrc_entry_type').value; //The type
    o.targetPlatforms = null; //This'll be set to a list of platforms if it is checked later.

    //Type specific data.
    if (o.type == "font") {
        //Use the regex entered as the character regex.
        o.characterRegex = document.getElementById('addresrc_entry_font_characters').value;
        //Append the font size to the C id.
        o.name += "_" + document.getElementById('addresrc_entry_font_size').value;
        //If the tracking adjust isn't zero, set it.
        var trackAdjust = parseInt(document.getElementById('addresrc_entry_font_tracking').value);
        if (trackAdjust != 0) {
            o.trackingAdjust = trackAdjust;
        }
        //If compatability isn't latest, set it to 2.7.
        if (document.getElementById('addresrc_entry_font_compat').value != "latest") {
            o.compatibility = "2.7";
        }
    }
    if (o.type == "bitmap") {
        //Add each compression type.
        var mem = document.getElementById('addresrc_entry_bitmap_memformat').value;
        var space = document.getElementById('addresrc_entry_bitmap_opti').value;
        var store = document.getElementById('addresrc_entry_bitmap_storeformat').value;

        if (mem != "default") {
            o.memoryFormat = mem;
        }
        if (space != "default") {
            o.spaceOptimization = space;
        }
        if (store != "default") {
            o.storageFormat = store;
        }
    }

    //Add the uploaded id.
    o.x_webpebble_media_id = fileData.id;

    return o;
}

edit_resource.saveNow = function (callback) {
    //Check if we need to create a new file, or if we just save this one.
    if (edit_resource.openFile == null) {
        //Create
        edit_resource.createDataNow(callback);
    } else {
        //Edit
        edit_resource.updateDataNow(callback);
    }
}

edit_resource.createDataNow = function (callback) {
    //First, upload the new file. A check should be done to see if one was actually sent.
    project.showDialog("Creating Resource...", '<div class="inf_loader"></div>', [], [], null, false);
    //Determine the type of file.
    var type = "images";
    if (document.getElementById('addresrc_entry_type').value == "font") { type = "fonts"; }
    if (document.getElementById('addresrc_entry_type').value == "raw") { type = "data"; }
    //Do the upload.
    edit_resource.uploadFile("resources", type, document.getElementById('addresrc_entry_filename').value, function (uploaded_file) {
        //Generate the Pebble resource file.
        var pbl_data = edit_resource.getUpdatedPebbleMedia(uploaded_file);
        //Push it to the resources for the Pebble.
        project.appInfo.pebble.resources.media.push(pbl_data);
        //Save that file.
        project.saveAppInfo(function () {
            //Add this file to the sidebar.
            project.addResourceToSidebar(uploaded_file);
            //Hide the loader.
            project.hideDialog();
            //Call the callback, if there is one.
            if (callback != null) {
                callback(uploaded_file, pbl_data);
            }
        });
    });
}

edit_resource.updateDataNow = function (callback) {
    project.showDialog("Updating Resource...", '<div class="inf_loader"></div>', [], [], null, false);
    //Determine the type of file.
    var type = "images";
    if (document.getElementById('addresrc_entry_type').value == "font") { type = "fonts"; }
    if (document.getElementById('addresrc_entry_type').value == "raw") { type = "data"; }
    var afterFileUpdate = function () {
        var uploaded_file = edit_resource.openFile.media_data;
        //Generate the Pebble resource file.
        var pbl_data = edit_resource.getUpdatedPebbleMedia(uploaded_file);
        //Find the old copy version of this resource and replace it with ourself.
        var old_pebble_data = project.appInfo.pebble.resources.media;
        //Find this one.
        for (var i = 0; i < old_pebble_data.length; i += 1) {
            if (old_pebble_data[i].x_webpebble_media_id == edit_resource.openFile.id) {
                //Remove this.
                project.appInfo.pebble.resources.media.splice(i, 1);
                break;
            }
        }
        //Push it to the resources for the Pebble.
        project.appInfo.pebble.resources.media.push(pbl_data);
        //Save that file.
        project.saveAppInfo(function () {
            //Add this file to the sidebar.
            project.addResourceToSidebar(uploaded_file);
            //Hide the loader.
            project.hideDialog();
            //Call the callback, if there is one.
            if (callback != null) {
                callback(uploaded_file, pbl_data);
            }
        });
    }
    if (edit_resource.checkIfFileIsPending) {
        //Upload a new file.
        alert('todo');
    } else {
        //Go forward and keep old file.
        afterFileUpdate();
    }
}

edit_resource.checkIfFileIsPending = function () {
    return document.getElementById("add_resrc_uploader_file").files.length != 0;
}

edit_resource.uploadFile = function (type, sub_type, name, callback) {
    //Thanks to https://stackoverflow.com/questions/39053413/how-to-submit-the-file-on-the-same-page-without-reloading for telling me how to do this without a reload.
    var form_ele = document.getElementById('add_resrc_uploader');
    var form = jQuery(form_ele);
    var url = "/project/" + project.id + "/upload_media/?type=" + encodeURIComponent(type) + "&sub_type=" + encodeURIComponent(sub_type) + "&nickname=" + encodeURIComponent(name);
    jQuery.ajax({
        url: url,
        type: "POST",
        data: new FormData(form_ele),
        processData: false,
        contentType: false,
        async: true,
        success: function (response) {
            if (response) {
                callback(response);
            }
        },
        error: function () {
            alert("server error while uploading file");
        }
    });
}