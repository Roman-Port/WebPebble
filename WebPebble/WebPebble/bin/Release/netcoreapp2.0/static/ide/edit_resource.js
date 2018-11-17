var edit_resource = {};
edit_resource.openFile = null;

edit_resource.onSelect = function () {
    //Remove any old warnings.
    edit_resource.removeOldWarnings();
    //Selected to start.
    edit_resource.openFile = null;
    //Make sure we can see the selected type.
    edit_resource.onTypeChange('bitmap');
};

edit_resource.removeOldWarnings = function () {
    edit_resource.removeWarning('add_resrc_id_frame');
    edit_resource.removeWarning('add_resrc_uploader_frame');
}

edit_resource.onSelectExisting = function (context) {
    //Remove any old warnings.
    edit_resource.removeOldWarnings();
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
    //Show the preview. 
    var preview_window = document.getElementById('resource_preview');
    preview_window.style.display = "block";
    var resrc_url = "/project/" + project.id + "/media/" + media_data.id + "/get/";
    if (pebble_data.type == "raw") {
        //Show the button to download this resource again.
        preview_window.appendChild(edit_resource.createControlItemNode("Font Preview", '<div class="med_button" onclick="edit_resource.onDownloadRawBtnClicked(this);">Download Raw Resource</div>'));
        //Set URL.
        preview_window.x_download_url = resrc_url + "application_octet-stream/" + pebble_data.name;
    } else if (pebble_data.type == "font") {
        //Preview the font.
        //Get the font size.
        var name_split2 = pebble_data.name.split('_');
        var font_size2 = name_split[name_split2.length - 1];
        font_size2 = parseInt(font_size2);
        if (font_size2 > 40) {
            font_size2 = 40;
        }
        //Create the CSS.
        var e = document.createElement('style');
        e.innerHTML = '@font-face { font-family: "temporary_resrc_font"; src: url("' + resrc_url + 'font_ttf") format("truetype");}';
        preview_window.appendChild(e);
        //Append an actual preview of the font. Display the standard ones.
        var inner = '<textarea style="line-height:' + (font_size2 + 6).toString() + 'px; font-size:' + font_size2.toString() + 'px; background-color:white; font-family: &quot;temporary_resrc_font&quot;, serif; width:100%;" type="text" rows="4">ABCDEFGHIJKLMNOPQRSTUVWXYZ abcdefghijklmnopwxyz 123456789</textarea>';
        preview_window.appendChild(edit_resource.createControlItemNode("Font Preview", inner));
    } else {
        //This is some sort of bitmap image.
        preview_window.appendChild(edit_resource.createControlItemNode("Image Preview", '<table style="min-width: 144px; min-height: 168px; background-color: #c1c1c1; border-radius: 5px;"> <tr> <td style="text-align: center; vertical-align: middle;"> <img src="' + resrc_url + "image_png/" +'"> </td> </tr> </table>'));
    }
};

edit_resource.onChange = function () {
    //Check if this is a file that already exists.
    if (edit_resource.openFile != null) {
        //We need to set a flag so we don't leave the page without saving.
        sidebarmanager.markUnsaved(edit_resource.openFile.media_data.nickname, true, function (callback) {
            //Save quietly.
            edit_resource.saveNow(function () {
                callback();
            });
        });
    }
    
}

edit_resource.onDownloadRawBtnClicked = function (context) {
    var e = context.parentNode.parentNode.parentNode;
    filemanager.DownloadUrl(e.x_download_url);
}

edit_resource.createControlItemNode = function (left, right) {
    var bm = document.createElement('div');
    bm.className = "control_item";
    var label_node = document.createElement('div');
    label_node.className = "label";
    label_node.innerText = left;
    bm.appendChild(label_node);

    var right_node = document.createElement('div');
    right_node.className = "control_normal";
    right_node.innerHTML = right;
    bm.appendChild(right_node);

    return bm;
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

edit_resource.saveNow = function (final_callback) {
    var callback = function () {
        //Unmark this file as unsaved.
        sidebarmanager.unmarkUnsaved();
        //Run the final callback.
        if (final_callback != null) {
            final_callback();
        }
    }
    //Check if we need to create a new file, or if we just save this one.
    if (edit_resource.openFile == null) {
        //Create
        edit_resource.createDataNow(callback);
    } else {
        //Edit
        edit_resource.updateDataNow(callback);
    }
};

edit_resource.addWarning = function (elementId, text) {
    //Check if a warning already exists.
    if (document.getElementById(elementId + '_warning') != null) {
        return;
    }
    //Create element.
    var e = document.createElement('div');
    e.id = elementId + "_warning";
    e.className = "warning";
    e.innerText = text;
    document.getElementById(elementId).appendChild(e);
};

edit_resource.removeWarning = function (elementId) {
    //Remove a warning if it exists.
    var node = document.getElementById(elementId + '_warning');
    if (node != null) {
        node.parentNode.removeChild(node);
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
        edit_resource.removeWarning('add_resrc_uploader_frame');
        //Save that file.
        project.serverRequest("appinfo.json/add_resource", function (updated_data) {
            //Update the local data.
            project.refreshAppInfo(function () {
                pbl_data = updated_data;
                //Add this file to the sidebar.
                project.addResourceToSidebar(uploaded_file);
                //Hide the loader.
                project.hideDialog();
                //Switch to this. This pretty much just allows us to save to it.
                edit_resource.onSelectExisting(pbl_data.x_webpebble_media_id);
                //Call the callback, if there is one.
                if (callback != null) {
                    callback(uploaded_file, pbl_data);
                }
            });
        }, null, true, "POST", JSON.stringify(pbl_data));

    }, function () {
        //File upload failed.
        edit_resource.addWarning('add_resrc_uploader_frame', "Please attach a file.");
        project.hideDialog();
    });
};

edit_resource.updateDataNow = function (callback) {
    project.showDialog("Updating Resource...", '<div class="inf_loader"></div>', [], [], null, false);
    //Determine the type of file.
    var type = "images";
    if (document.getElementById('addresrc_entry_type').value == "font") { type = "fonts"; }
    if (document.getElementById('addresrc_entry_type').value == "raw") { type = "data"; }
    var afterFileUpdate = function () {
        var uploaded_file = edit_resource.openFile.media_data;
        //Rename
        uploaded_file.nickname = document.getElementById('addresrc_entry_filename').value;
        //Generate the Pebble resource file.
        var pbl_data = edit_resource.getUpdatedPebbleMedia(uploaded_file);
        //Find the old copy version of this resource and replace it with ourself.
        var old_pebble_data = project.appInfo.pebble.resources.media;
        //Set the x_webpebble_pebble_id
        pbl_data.x_webpebble_pebble_media_id = old_pebble_data.x_webpebble_pebble_media_id;
        //Find this one.
        for (var i = 0; i < old_pebble_data.length; i += 1) {
            if (old_pebble_data[i].x_webpebble_pebble_media_id == edit_resource.openFile.pebble_data.x_webpebble_pebble_media_id) {
                //Remove this.
                project.appInfo.pebble.resources.media.splice(i, 1);
                break;
            }
        }
        //Save that file.
        project.serverRequest("appinfo.json/add_resource", function (updated_data) {
            //Refresh local data.
            project.refreshAppInfo(function () {
                //We have updated data. Write it.
                edit_resource.openFile.pbl_data = updated_data;
                pbl_data = updated_data;
                project.serverRequest("media/" + edit_resource.openFile.media_data.id + "/rename/?name=" + encodeURIComponent(uploaded_file.nickname), function () {
                    //Rename object on sidebar. Something fishy going on here....
                    sidebarmanager.items[edit_resource.openFile.id].tab_ele.firstChild.innerText = document.getElementById('addresrc_entry_filename').value;
                    //Hide the loader.
                    project.hideDialog();
                    //Call the callback, if there is one.
                    if (callback != null) {
                        callback(uploaded_file, pbl_data);
                    }
                }, null, false);
            });
            
        }, null, true, "POST", JSON.stringify(pbl_data));
    };
     
    if (edit_resource.checkIfFileIsPending()) {
        //Upload a new file.
        //Delete the old media.
        project.serverRequest("media/" + edit_resource.openFile.media_data.id + "/delete/?challenge=delete", function () {
            //Now, reupload the new media.
            edit_resource.uploadFile("resources", type, document.getElementById('addresrc_entry_filename').value, function (uploaded) {
                //Delete old media.
                edit_resource.openFile.media_data = uploaded;
                //Call the main code now.
                afterFileUpdate();
            });
        }, function () { }, false, "POST", "delete");
    } else {
        //Go forward and keep old file.
        afterFileUpdate();
    }
}

edit_resource.checkIfFileIsPending = function () {
    return document.getElementById("add_resrc_uploader_file").files.length != 0;
}

edit_resource.check_identifier_latestid = 0;

edit_resource.check_identifier = function (id) {
    //Check to see if we need to append the font size.
    if (document.getElementById('addresrc_entry_type').value == "font") {
        id += '_' + document.getElementById('addresrc_entry_font_size').value.toString();
    }
    //Check if this is the current name.
    if (edit_resource.openFile != null) {
        if (edit_resource.openFile.pebble_data.name == id) {
            //This is the current name. Remove the warning just in case it is displayed and then don't bother to check.
            edit_resource.removeWarning('add_resrc_id_frame');
            return;
        }
    }
    
    //Create a request ID because some of these might finish before others.
    var req_id = (edit_resource.check_identifier_latestid + 1).toString();
    edit_resource.check_identifier_latestid++;
    project.serverRequest("check_identifier?resrc_id=" + encodeURIComponent(id) + "&request_id=" + req_id, function (d) {
        if (d.request_id == edit_resource.check_identifier_latestid.toString()) {
            if (d.exists) {
                edit_resource.addWarning('add_resrc_id_frame', 'That resource ID already exists.');
            } else {
                edit_resource.removeWarning('add_resrc_id_frame');
            }
        }
    }, null, true);
}

edit_resource.uploadFile = function (type, sub_type, name, callback, failedCallback) {
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
            failedCallback();
        }
    });
}