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
    edit_resource.refreshPreviewWindow(pebble_data);
};

edit_resource.refreshPreviewWindow = function(pebble_data) {
    //Destroy old window
    var oldNode = document.getElementById('preview_window_node');
    if(oldNode != null) {
        oldNode.parentElement.removeChild(oldNode);
    }
    //Create new
    var preview_window = document.getElementById('resource_preview');
    preview_window.style.display = "block";
    var resrc_url = "https://api.webpebble.get-rpws.com/project/" + project.id + "/media/" + pebble_data.x_webpebble_media_id + "/";
    if (pebble_data.type == "raw") {
        //Show the button to download this resource again.
        preview_window.appendChild(edit_resource.createControlItemNode("Font Preview", '<div class="med_button" onclick="edit_resource.onDownloadRawBtnClicked(this);">Download Raw Resource</div>', 'preview_window_node'));
        //Set URL.
        preview_window.x_download_url = resrc_url + "application_octet-stream/" + pebble_data.name;
    } else if (pebble_data.type == "font") {
        //Preview the font.
        //Get the font size.
        var name_split2 = pebble_data.name.split('_');
        var font_size2 = name_split2[name_split2.length - 1];
        font_size2 = parseInt(font_size2);
        if (font_size2 > 40) {
            font_size2 = 40;
        }
        //Create the CSS. We'll need to get a one time token to do this.
        var e = document.createElement('style');
        e.innerHTML = '';
        preview_window.appendChild(e);
        project.anyServerRequest("https://api.webpebble.get-rpws.com/users/@me/create_asset_token/", function(token) {
            e.innerHTML = '@font-face { font-family: "temporary_resrc_font"; src: url("' + resrc_url + '?mime=font%2Fttf&one_time_token='+token.token+'") format("truetype");}';
        }, function() {
            project.reportError("Failed to load font preview; Couldn't get one-time access token.");
        });
        
        //Append an actual preview of the font. Display the standard ones.
        var inner = '<textarea style="line-height:' + (font_size2 + 6).toString() + 'px; font-size:' + font_size2.toString() + 'px; background-color:white; font-family: &quot;temporary_resrc_font&quot;, serif; width:100%;" type="text" rows="4">ABCDEFGHIJKLMNOPQRSTUVWXYZ abcdefghijklmnopwxyz 123456789</textarea>';
        preview_window.appendChild(edit_resource.createControlItemNode("Font Preview", inner, 'preview_window_node'));
    } else {
        //This is some sort of bitmap image.
        preview_window.appendChild(edit_resource.createControlItemNode("Image Preview", '<table style="min-width: 144px; min-height: 168px; background-color: #c1c1c1; border-radius: 5px;"> <tr> <td style="text-align: center; vertical-align: middle;"> <img src="' + resrc_url + "?mime=image%2Fpng" +'"> </td> </tr> </table>', 'preview_window_node'));
    }
}

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

edit_resource.createControlItemNode = function (left, right, id) {
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

    if(id != null) {
        bm.id = id;
    }

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

edit_resource.getUpdatedPebbleMedia = function () {
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

edit_resource.getTypeId = function() {
    var type = 1; //Images
    if (document.getElementById('addresrc_entry_type').value == "font") { type = 2; /*Fonts*/ }
    if (document.getElementById('addresrc_entry_type').value == "raw") { type = 3; /*Binary data*/ }
    return type;
}

edit_resource.createDataNow = function (callback) {
    //First, upload the new file. A check should be done to see if one was actually sent.
    project.showDialog("Creating Resource...", '<div class="inf_loader"></div>', [], [], null, false);
    //Determine the type of file.
    var type = edit_resource.getTypeId();
    //Create the object creation payload.
    var pbl_data = edit_resource.getUpdatedPebbleMedia();
    var payload = {
        "type":1,
        "sub_type":type,
        "name":document.getElementById('addresrc_entry_filename').value,
        "appInfoJson": pbl_data
    };
    //Create the object.
    project.serverRequest("media/create/", function(object_data) {
        var newId = object_data.id;
        //Do the upload.
        edit_resource.uploadFile(newId, function (uploaded_file) {
            //We've already updated the appinfo data.
            //Update the local data.
            project.refreshAppInfo(function () {
                pbl_data.x_webpebble_media_id = newId;
                //Add this file to the sidebar.
                project.addResourceToSidebar(object_data);
                //Hide the loader.
                project.hideDialog();
                //Switch to this. This pretty much just allows us to save to it.
                edit_resource.onSelectExisting(newId);
                //Call the callback, if there is one.
                if (callback != null) {
                    callback(object_data, pbl_data);
                }
            });
        }, function() {
            project.hideDialog();
            //Quietly delete the asset.
            project.serverRequest("media/"+newId+"/", function(object_data) {

            }, null, true, "DELETE");
        }, function () {
            //File upload failed.
            edit_resource.addWarning('add_resrc_uploader_frame', "Please attach a file.");
            
        });
    }, null, true, "POST", JSON.stringify(payload));  
};

edit_resource.updateDataNow = function (callback) {
    project.showDialog("Updating Resource...", '<div class="inf_loader"></div>', [], [], null, false);
    //Determine the type of file.
    var type = edit_resource.getTypeId();
    var id = edit_resource.openFile.pebble_data.x_webpebble_media_id;

    var afterFileUpdate = function () {
        var uploaded_file = edit_resource.openFile.media_data;
        //Generate the request payload.
        var pbl_data = edit_resource.getUpdatedPebbleMedia();
        pbl_data.x_webpebble_media_id = id;
        var payload = {
            "name":document.getElementById('addresrc_entry_filename').value,
            "appinfoData":pbl_data,
            "sub_type":type
        }
        //Find the old copy version of this resource and replace it with ourself.
        project.serverRequest("media/"+id+"/", function () {
            //Refresh local data.
            project.refreshAppInfo(function () {
                //Rename object on sidebar. Something fishy going on here....
                sidebarmanager.items[edit_resource.openFile.id].tab_ele.firstChild.innerText = document.getElementById('addresrc_entry_filename').value;
                //Hide the loader.
                project.hideDialog();
                //Refresh preview window.
                edit_resource.refreshPreviewWindow(pbl_data);
                //Call the callback, if there is one.
                if (callback != null) {
                    callback(uploaded_file, pbl_data);
                }
            });
        }, null, true, "POST", JSON.stringify(payload));
        
    };
     
    if (edit_resource.checkIfFileIsPending()) {
        //Upload a new file.
        edit_resource.uploadFile(id, function () {
            //Call the main code now.
            afterFileUpdate();
        }, function() {
            //Stop.
            project.hideDialog();
        }, function() {
            //No file was uploaded.
        });
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

edit_resource.uploadFile = function (id, callback, failedCallback, noFileCallback) {
    project.showLoaderBottom("Uploading file...", function(c) {
        //Thanks to https://stackoverflow.com/questions/39053413/how-to-submit-the-file-on-the-same-page-without-reloading for telling me how to do this without a reload.
        var form_ele = document.getElementById('add_resrc_uploader');
        var form = jQuery(form_ele);
        var url = "https://api.webpebble.get-rpws.com/project/" + project.id + "/media/"+id+"/?upload_method=1";
        jQuery.ajax({
            url: url,
            type: "PUT",
            data: new FormData(form_ele),
            processData: false,
            contentType: false,
            async: true,
            success: function (response) {
                c();
                
                if (response) {
                    if(response.ok == false) {
                        if(response.size == -1) {
                            //No file uploaded
                            project.reportError("No file was attached!");
                            noFileCallback();
                            failedCallback(response);
                        } else {
                            project.reportError("Failed to upload file; "+response.uploader_error);
                            failedCallback(response);
                        }
                    } else {
                        callback(response);
                    }
                } else {
                    
                }
            },
            error: function () {
                c();
                project.reportError("Failed to upload file; The server was unable to handle the request. Is the file too big?");
                failedCallback();
            },
            
            xhrFields: {
                withCredentials: true
        }
        });
    });
    
}