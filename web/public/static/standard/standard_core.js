var standard = {};

standard.serverRequest = function (url, run_callback, fail_callback, isJson, type, body, timeout, knownServerErrorCallback) {
    //This is the main server request function. Please change all other ones to use this.
    if (isJson == null) { isJson = true; }

    if (type == null) { type = "GET"; }

    if (timeout == null) { timeout = 10000; }

    if (fail_callback == null) {
        fail_callback = function () {
            project.showDialog("Error", "Failed to connect. Please check your internet and try again later.", ["Retry"], [function () { project.anyServerRequest(url, run_callback, fail_callback, isJson, type, body, timeout);  }]);
        }
    }
    var xmlhttp = new XMLHttpRequest();

    xmlhttp.timeout = timeout;

    xmlhttp.onreadystatechange = function () {
        if (this.readyState == 4 && this.status == 200) {
            if (isJson) {
                //This is JSON.
                //This is most likely to be valid, but check for errors.
                var JSON_Data;
                try {
                    JSON_Data = JSON.parse(this.responseText);
                } catch (e) {
                    fail_callback("JSON Parse Error", true);
                    return;
                }
                //Return it
                run_callback(JSON_Data);
            } else {
                //Just return it
                run_callback(this.responseText);
            }
        } else if (this.readyState == 4) {
            //Parse the response and display the error
            var errorData = JSON.parse(this.responseText);
            knownServerErrorCallback(errorData);
        }
    }

    xmlhttp.ontimeout = function () {
        fail_callback("No Connection", false);
    }

    xmlhttp.onerror = function () {
        fail_callback("No Connection", false);
    }

    xmlhttp.onabort = function () {
        fail_callback("Abort", false);
    }
    //Todo: Add timeout error.
    xmlhttp.open(type, url, true);
    xmlhttp.withCredentials = true;
    xmlhttp.send(body);
};

standard.showLoader = function (title) {
    project.showDialog(title, '<div class="inf_loader"></div>', [], [], null, false);
};

standard.showDialog = function (title, text, buttonTextArray, buttonCallbackArray, data, treatAsDom) {
    //Data can be whatever you want to pass into the callbacks.
    //Set text first.
    document.getElementById('popup_title').innerText = title;
    if (treatAsDom == null) { treatAsDom = false; }
    if (treatAsDom) {
        //Copy dom elements to this.
        document.getElementById('popup_text').innerHTML = "";
        document.getElementById('popup_text').appendChild(text);
    } else {
        //Treat as normal HTML.
        document.getElementById('popup_text').innerHTML = text;
    }
    //Erase old buttons now.
    document.getElementById('popup_btns').innerHTML = "";
    //Add new buttons.
    for (var i = 0; i < buttonTextArray.length; i += 1) {
        var b_text = buttonTextArray[i];
        var b_callback = buttonCallbackArray[i];
        var e = document.createElement('div');
        e.innerText = b_text;
        e.x_callback = b_callback;
        e.x_callback_data = data;
        e.addEventListener('click', function () {
            //Hide
            project.hideDialog();
            //Call
            this.x_callback(this.x_callback_data, this);
        });
        document.getElementById('popup_btns').appendChild(e);
    }
    //Show.
    document.getElementById('popup_window_bg').className = "popup_background";
    document.getElementById('popup_window').className = "popup_window_container";
}

standard.hideDialog = function () {
    document.getElementById('popup_window_bg').className = "popup_background hide";
    document.getElementById('popup_window').className = "popup_window_container hide";
};

standard.displayForm = function (name, options, confirmAction, cancelAction) {
    //Create the dialog HTML.
    var dialogHtml = document.createElement('div');
    //Loop through each option and include it.
    for (var i = 0; i < options.length; i += 1) {
        var d = options[i];
        var nameHtml = document.createElement('div');
        var formHtml = document.createElement('div');
        //Add the title.
        var titleEle = document.createElement('div');
        titleEle.className = "formTitle";
        titleEle.innerText = d.title;
        nameHtml.appendChild(titleEle);
        //Create the form action based on the action type.
        var formEle = null;
        if (d.type == "text") {
            formEle = document.createElement('input');
            formEle.type = "text";
            //Add change event listener if asked.
            if (d.onChange != null) {
                formEle.addEventListener('input', d.onChange);
            }
        }
        if (d.type == "select") {
            var inner = document.createElement('select');
            //Add options.
            for (var ii = 0; ii < d.options.length; ii += 1) {
                var dd = d.options[ii];
                var ele = document.createElement('option');
                ele.value = dd.value;
                ele.innerText = dd.title;
                inner.appendChild(ele);
            }
            //Add change event listerer if needed
            if (d.onChange != null) {
                inner.addEventListener('change', d.onChange);
            }
            //Move this into an inner div.
            formEle = document.createElement('div');
            formEle.className = "selectFix ";
            formEle.appendChild(inner);
            formEle.x_refer = formEle.firstChild;
        }
        //If it's null, complain.
        if (formEle == null) {
            console.log("Unknown type - " + d);
            continue;
        } 
        //Fill out the rest of the data and append.
        formEle.className += "formItem";
        formEle.id = "formele_id_" + i;
        formHtml.appendChild(formEle);
        //Append all
        nameHtml.className = "formCol";
        formHtml.className = "formCol";
        dialogHtml.appendChild(nameHtml);
        dialogHtml.appendChild(formHtml);
    }
    //Now, create the dialog.
    standard.showDialog(name, dialogHtml, ["Create", "Cancel"], [
        function () {
            //Gather the results.
            var results = [];
            for (var i = 0; i < options.length; i += 1) {
                var ele = document.getElementById('formele_id_' + i);
                if (ele.x_refer != null) {
                    ele = ele.x_refer;
                }
                results.push(ele.value);
            }
            //Call the callback.
            confirmAction(results);
        },
        function () {
            cancelAction();
        },
    ],null,true);
};