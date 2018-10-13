var edit_resource = {};

edit_resource.onSelect = function () {
    //Selected to start.

    //Make sure we can see the selected type.
    edit_resource.onTypeChange('bitmap');
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

edit_resource.onFontSizeChange = function (value) {
    //Fill in the "identifier" box.
    document.getElementById('typeselect_font_id').innerText = document.getElementById('addresrc_entry_id').value + "_" + value;
}

edit_resource.getUpdatedPebbleMedia = function (fileData) {
    //Get the package.json/pebble/resources/media data for this.
    //fileData is the WebPebble media data.
    var o = {};
    o.file = fileData.filename;
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

    return o;
}

edit_resource.saveNow = function (callback) {
    //First, upload the new file. A check should be done to see if one was actually sent.
    project.showDialog("Creating Resource...", '<div class="inf_loader"></div>', [], [], null, false);
    //Determine the type of file.
    var type = "images";
    if (document.getElementById('addresrc_entry_type').value == "font") { type = "fonts"; }
    if (document.getElementById('addresrc_entry_type').value == "raw") { type = "data"; }
    //Do the upload.
    edit_resource.uploadFile("resource", type, function (uploaded_file) {
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

edit_resource.uploadFile = function (type,sub_type, callback) {
    //Thanks to https://stackoverflow.com/questions/39053413/how-to-submit-the-file-on-the-same-page-without-reloading for telling me how to do this without a reload.
    var form_ele = document.getElementById('add_resrc_uploader');
    var form = jQuery(form_ele);
    var url = "/project/" + project.id + "/upload_media/?type=" + encodeURIComponent(type) + "&sub_type=" + encodeURIComponent(sub_type);
    jQuery.ajax({
        url: url,
        type: "POST",
        data: new FormData(form_ele),
        processData: false,
        contentType: false,
        async: false,
        success: function (response) {
            if (response) {
                callback(JSON.parse(response));
            }
        },
        error: function () {
            alert("server error while uploading file");
        }
    });
}